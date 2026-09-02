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

        private readonly List<VerificationCaseDto> _cases = [];
        private readonly object _syncRoot = new();

        public VerificationCaseDto Create(CreateVerificationCaseRequest request)
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

            lock (_syncRoot)
            {
                _cases.Add(verificationCase);
            }

            return verificationCase;
        }

        public VerificationCaseDto? Get(Guid caseId)
        {
            lock (_syncRoot)
            {
                return _cases.FirstOrDefault(item => item.CaseId == caseId);
            }
        }

        public VerificationCaseDto? UpdateStatus(Guid caseId, string status)
        {
            if (string.IsNullOrWhiteSpace(status) || !AllowedStatuses.Contains(status))
            {
                throw new ArgumentException(
                    $"Status must be one of: {string.Join(", ", AllowedStatuses)}.",
                    nameof(status));
            }

            lock (_syncRoot)
            {
                var index = _cases.FindIndex(item => item.CaseId == caseId);
                if (index < 0)
                {
                    return null;
                }

                var updated = _cases[index] with
                {
                    Status = AllowedStatuses.First(item => item.Equals(status, StringComparison.OrdinalIgnoreCase)),
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };
                _cases[index] = updated;
                return updated;
            }
        }

        public IReadOnlyList<VerificationCaseDto> GetQueue()
        {
            lock (_syncRoot)
            {
                return _cases.ToList();
            }
        }
    }
}
