using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public interface IVerificationCaseService
    {
        Task<VerificationCaseDto> CreateAsync(CreateVerificationCaseRequest request, CancellationToken cancellationToken);

        Task<VerificationCaseDto?> GetAsync(Guid caseId, CancellationToken cancellationToken);

        Task<VerificationCaseDto?> UpdateStatusAsync(Guid caseId, string status, CancellationToken cancellationToken);

        Task<IReadOnlyList<VerificationCaseDto>> GetQueueAsync(CancellationToken cancellationToken);

        Task<DisputeDto?> DisputeAsync(Guid caseId, DisputeRequest request, CancellationToken cancellationToken);
    }
}
