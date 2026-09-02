using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public interface IFeedbackModerationService
    {
        StructuredFeedbackDto Submit(StructuredFeedbackRequest request);

        IReadOnlyList<StructuredFeedbackDto> GetQueue();
    }
}
