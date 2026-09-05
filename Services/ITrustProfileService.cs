using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public interface ITrustProfileService
    {
        Task<IReadOnlyList<TrustProfileDto>> GetProfilesAsync(CancellationToken cancellationToken);

        Task<TrustProfileDto?> GetProfileAsync(int userId, CancellationToken cancellationToken);

        Task<ProfileSearchResponse> SearchAsync(ProfileSearchRequest request, CancellationToken cancellationToken);
    }
}
