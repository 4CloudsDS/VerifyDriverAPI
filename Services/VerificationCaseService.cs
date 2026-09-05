using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public sealed class VerificationCaseService : IVerificationCaseService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Draft",
            "Submitted",
            "CounterpartyReview",
            "Verified",
            "Rejected",
            "Disputed"
        };

        private readonly AppDbContext _context;

        public VerificationCaseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VerificationCaseDto> CreateAsync(
            CreateVerificationCaseRequest request,
            CancellationToken cancellationToken)
        {
            await ValidateAsync(request, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var verificationCase = new VerificationCaseDto(
                Guid.NewGuid(),
                request.CaseType.Trim(),
                request.RelationshipContext.Trim(),
                request.PrimaryProfileId,
                string.IsNullOrWhiteSpace(request.Counterparty) ? null : request.Counterparty.Trim(),
                "Draft",
                (request.Evidence ?? [])
                    .Select(item => new DocumentEvidenceDto(
                        Guid.NewGuid(),
                        item.DocumentType.Trim(),
                        item.FileName.Trim(),
                        item.ContentType,
                        item.SizeBytes,
                        PubliclyVisible: false))
                    .ToList(),
                (request.Confirmations ?? [])
                    .Select(item => new CounterpartyConfirmationDto(
                        Guid.NewGuid(),
                        item.Counterparty.Trim(),
                        item.Claim.Trim(),
                        string.IsNullOrWhiteSpace(item.State) ? "Requested" : item.State.Trim()))
                    .ToList(),
                now,
                now);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO verification_cases
                    (case_id, case_type, relationship_context, primary_profile_id, counterparty, status, privacy_status, created_at_utc, updated_at_utc)
                VALUES
                    ({verificationCase.CaseId.ToString()}, {verificationCase.CaseType}, {verificationCase.RelationshipContext}, {verificationCase.PrimaryProfileId}, {verificationCase.Counterparty}, {verificationCase.Status}, {verificationCase.PrivacyStatus}, {verificationCase.CreatedAtUtc.ToString("O")}, {verificationCase.UpdatedAtUtc.ToString("O")});
                """,
                cancellationToken);

            foreach (var evidence in verificationCase.Evidence)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO document_evidence
                        (document_id, case_id, document_type, file_name, content_type, size_bytes, publicly_visible)
                    VALUES
                        ({evidence.DocumentId.ToString()}, {verificationCase.CaseId.ToString()}, {evidence.DocumentType}, {evidence.FileName}, {evidence.ContentType}, {evidence.SizeBytes}, {evidence.PubliclyVisible});
                    """,
                    cancellationToken);
            }

            foreach (var confirmation in verificationCase.Confirmations)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    INSERT INTO counterparty_confirmations
                        (confirmation_id, case_id, counterparty, claim, state)
                    VALUES
                        ({confirmation.ConfirmationId.ToString()}, {verificationCase.CaseId.ToString()}, {confirmation.Counterparty}, {confirmation.Claim}, {confirmation.State});
                    """,
                    cancellationToken);
            }

            return verificationCase;
        }

        public async Task<VerificationCaseDto?> GetAsync(Guid caseId, CancellationToken cancellationToken)
        {
            var cases = await QueryCasesAsync("WHERE case_id = @caseId", command =>
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@caseId";
                parameter.Value = caseId.ToString();
                command.Parameters.Add(parameter);
            }, cancellationToken);

            return cases.FirstOrDefault();
        }

        public async Task<VerificationCaseDto?> UpdateStatusAsync(
            Guid caseId,
            string status,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status))
            {
                throw new ArgumentException(
                    $"Status must be one of: {string.Join(", ", AllowedStatuses)}.",
                    nameof(status));
            }

            var canonicalStatus = AllowedStatuses.First(item => item.Equals(status, StringComparison.OrdinalIgnoreCase));
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE verification_cases
                SET status = {canonicalStatus}, updated_at_utc = {DateTimeOffset.UtcNow.ToString("O")}
                WHERE case_id = {caseId.ToString()};
                """,
                cancellationToken);

            return affected == 0 ? null : await GetAsync(caseId, cancellationToken);
        }

        public Task<IReadOnlyList<VerificationCaseDto>> GetQueueAsync(CancellationToken cancellationToken)
        {
            return QueryCasesAsync(string.Empty, null, cancellationToken);
        }

        public async Task<DisputeDto?> DisputeAsync(
            Guid caseId,
            DisputeRequest request,
            CancellationToken cancellationToken)
        {
            Validate(request);

            var updated = await UpdateStatusAsync(caseId, "Disputed", cancellationToken);
            if (updated is null)
            {
                return null;
            }

            var dispute = new DisputeDto(
                Guid.NewGuid(),
                caseId,
                request.ProfileId,
                request.Reason.Trim(),
                request.RequestedBy.Trim(),
                "Open",
                DateTimeOffset.UtcNow);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO verification_case_disputes
                    (dispute_id, case_id, profile_id, reason, requested_by, status, created_at_utc)
                VALUES
                    ({dispute.DisputeId.ToString()}, {caseId.ToString()}, {dispute.ProfileId}, {dispute.Reason}, {dispute.RequestedBy}, {dispute.Status}, {dispute.CreatedAtUtc.ToString("O")});
                """,
                cancellationToken);

            return dispute;
        }

        private async Task<IReadOnlyList<VerificationCaseDto>> QueryCasesAsync(
            string whereClause,
            Action<System.Data.Common.DbCommand>? configure,
            CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                $"""
                SELECT case_id, case_type, relationship_context, primary_profile_id, counterparty,
                       status, privacy_status, created_at_utc, updated_at_utc
                FROM verification_cases
                {whereClause}
                ORDER BY created_at_utc DESC;
                """;
            configure?.Invoke(command);

            var rows = new List<VerificationCaseRow>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new VerificationCaseRow(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    DateTimeOffset.Parse(reader.GetString(7)),
                    DateTimeOffset.Parse(reader.GetString(8))));
            }

            var cases = new List<VerificationCaseDto>();
            foreach (var row in rows)
            {
                cases.Add(new VerificationCaseDto(
                    row.CaseId,
                    row.CaseType,
                    row.RelationshipContext,
                    row.PrimaryProfileId,
                    row.Counterparty,
                    row.Status,
                    await GetEvidenceAsync(row.CaseId, cancellationToken),
                    await GetConfirmationsAsync(row.CaseId, cancellationToken),
                    row.CreatedAtUtc,
                    row.UpdatedAtUtc,
                    row.PrivacyStatus));
            }

            return cases;
        }

        private async Task<IReadOnlyList<DocumentEvidenceDto>> GetEvidenceAsync(
            Guid caseId,
            CancellationToken cancellationToken)
        {
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT document_id, document_type, file_name, content_type, size_bytes, publicly_visible
                FROM document_evidence
                WHERE case_id = @caseId
                ORDER BY document_type;
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@caseId";
            parameter.Value = caseId.ToString();
            command.Parameters.Add(parameter);

            var evidence = new List<DocumentEvidenceDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                evidence.Add(new DocumentEvidenceDto(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    reader.GetInt32(5) == 1));
            }

            return evidence;
        }

        private async Task<IReadOnlyList<CounterpartyConfirmationDto>> GetConfirmationsAsync(
            Guid caseId,
            CancellationToken cancellationToken)
        {
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT confirmation_id, counterparty, claim, state
                FROM counterparty_confirmations
                WHERE case_id = @caseId
                ORDER BY counterparty;
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@caseId";
            parameter.Value = caseId.ToString();
            command.Parameters.Add(parameter);

            var confirmations = new List<CounterpartyConfirmationDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                confirmations.Add(new CounterpartyConfirmationDto(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)));
            }

            return confirmations;
        }

        private async Task ValidateAsync(CreateVerificationCaseRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CaseType))
            {
                throw new ArgumentException("Case type is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.RelationshipContext))
            {
                throw new ArgumentException("Relationship context is required.", nameof(request));
            }

            if (request.PrimaryProfileId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Primary profile id must be positive.");
            }

            var profileType = await _context.Users
                .AsNoTracking()
                .Where(user => user.uID == request.PrimaryProfileId)
                .Select(user => user.UserType == null ? "Driver" : user.UserType.U_T_description)
                .FirstOrDefaultAsync(cancellationToken);

            if (profileType is null)
            {
                throw new ArgumentException($"Primary profile {request.PrimaryProfileId} does not exist.", nameof(request));
            }

            var rules = VerificationRuleCatalog.ForProfileType(profileType);
            if (!rules.AllowedCaseTypes.Contains(request.CaseType.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"{rules.ProfileType} profiles cannot create '{request.CaseType}' verification cases.",
                    nameof(request));
            }

            var evidenceTypes = (request.Evidence ?? [])
                .Select(item => item.DocumentType)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var requiredEvidence in rules.RequiredEvidenceTypes)
            {
                if (!evidenceTypes.Contains(requiredEvidence))
                {
                    throw new ArgumentException(
                        $"{rules.ProfileType} verification requires {requiredEvidence} evidence.",
                        nameof(request));
                }
            }
        }

        private static void Validate(DisputeRequest request)
        {
            if (request.ProfileId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Profile id must be positive.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new ArgumentException("Dispute reason is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.RequestedBy))
            {
                throw new ArgumentException("Dispute requester is required.", nameof(request));
            }
        }

        private sealed record VerificationCaseRow(
            Guid CaseId,
            string CaseType,
            string RelationshipContext,
            int PrimaryProfileId,
            string? Counterparty,
            string Status,
            string PrivacyStatus,
            DateTimeOffset CreatedAtUtc,
            DateTimeOffset UpdatedAtUtc);
    }
}
