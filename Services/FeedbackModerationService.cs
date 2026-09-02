using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public sealed class FeedbackModerationService : IFeedbackModerationService
    {
        private readonly List<StructuredFeedbackDto> _queue = [];
        private readonly object _syncRoot = new();

        public StructuredFeedbackDto Submit(StructuredFeedbackRequest request)
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

            var feedback = new StructuredFeedbackDto(
                Guid.NewGuid(),
                request.Category.Trim(),
                request.Severity,
                request.Context.Trim(),
                request.RelatedProfileId,
                string.IsNullOrWhiteSpace(request.RelatedEntity) ? null : request.RelatedEntity.Trim(),
                string.IsNullOrWhiteSpace(request.SubmitterType) ? "Public" : request.SubmitterType.Trim(),
                "PendingModeration",
                DateTimeOffset.UtcNow);

            lock (_syncRoot)
            {
                _queue.Add(feedback);
            }

            return feedback;
        }

        public IReadOnlyList<StructuredFeedbackDto> GetQueue()
        {
            lock (_syncRoot)
            {
                return _queue.ToList();
            }
        }
    }
}
