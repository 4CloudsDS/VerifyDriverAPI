using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public sealed class FeedbackModerationService : IFeedbackModerationService
    {
        private readonly AppDbContext _context;

        public FeedbackModerationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StructuredFeedbackDto> SubmitAsync(
            StructuredFeedbackRequest request,
            CancellationToken cancellationToken)
        {
            Validate(request);

            var now = DateTimeOffset.UtcNow;
            var feedback = new StructuredFeedbackDto(
                Guid.NewGuid(),
                request.Category.Trim(),
                request.Severity,
                request.Context.Trim(),
                request.RelatedProfileId,
                string.IsNullOrWhiteSpace(request.RelatedEntity) ? null : request.RelatedEntity.Trim(),
                string.IsNullOrWhiteSpace(request.SubmitterType) ? "Public" : request.SubmitterType.Trim(),
                "PendingModeration",
                now);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO structured_feedback
                    (feedback_id, category, severity, context, related_profile_id, related_entity, submitter_type, moderation_status, dispute_status, submitted_at_utc)
                VALUES
                    ({feedback.FeedbackId.ToString()}, {feedback.Category}, {feedback.Severity}, {feedback.Context}, {feedback.RelatedProfileId}, {feedback.RelatedEntity}, {feedback.SubmitterType}, {feedback.ModerationStatus}, {feedback.DisputeStatus}, {feedback.SubmittedAtUtc.ToString("O")});
                """,
                cancellationToken);

            return feedback;
        }

        public async Task<IReadOnlyList<StructuredFeedbackDto>> GetQueueAsync(CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT feedback_id, category, severity, context, related_profile_id, related_entity,
                       submitter_type, moderation_status, dispute_status, submitted_at_utc
                FROM structured_feedback
                ORDER BY submitted_at_utc DESC;
                """;

            var results = new List<StructuredFeedbackDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new StructuredFeedbackDto(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    DateTimeOffset.Parse(reader.GetString(9)),
                    reader.GetString(8)));
            }

            return results;
        }

        public async Task<DisputeDto?> DisputeAsync(
            Guid feedbackId,
            DisputeRequest request,
            CancellationToken cancellationToken)
        {
            Validate(request);

            var existing = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE structured_feedback SET dispute_status = {"Disputed"} WHERE feedback_id = {feedbackId.ToString()};",
                cancellationToken);

            if (existing == 0)
            {
                return null;
            }

            var dispute = new DisputeDto(
                Guid.NewGuid(),
                feedbackId,
                request.ProfileId,
                request.Reason.Trim(),
                request.RequestedBy.Trim(),
                "Open",
                DateTimeOffset.UtcNow);

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO feedback_disputes
                    (dispute_id, feedback_id, profile_id, reason, requested_by, status, created_at_utc)
                VALUES
                    ({dispute.DisputeId.ToString()}, {feedbackId.ToString()}, {dispute.ProfileId}, {dispute.Reason}, {dispute.RequestedBy}, {dispute.Status}, {dispute.CreatedAtUtc.ToString("O")});
                """,
                cancellationToken);

            return dispute;
        }

        private static void Validate(StructuredFeedbackRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                throw new ArgumentException("Feedback category is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Context))
            {
                throw new ArgumentException("Feedback context is required.", nameof(request));
            }

            if (request.Severity is < 1 or > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Feedback severity must be between 1 and 5.");
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
    }
}
