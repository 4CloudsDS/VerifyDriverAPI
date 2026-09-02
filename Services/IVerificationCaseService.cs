using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public interface IVerificationCaseService
    {
        VerificationCaseDto Create(CreateVerificationCaseRequest request);

        VerificationCaseDto? Get(Guid caseId);

        VerificationCaseDto? UpdateStatus(Guid caseId, string status);

        IReadOnlyList<VerificationCaseDto> GetQueue();
    }
}
