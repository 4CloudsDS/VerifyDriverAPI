using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public interface IFeedbackModerationService
    {
        Task<StructuredFeedbackDto> SubmitAsync(StructuredFeedbackRequest request, CancellationToken cancellationToken);

        Task<IReadOnlyList<StructuredFeedbackDto>> GetQueueAsync(CancellationToken cancellationToken);

        Task<DisputeDto?> DisputeAsync(Guid feedbackId, DisputeRequest request, CancellationToken cancellationToken);
    }
}
