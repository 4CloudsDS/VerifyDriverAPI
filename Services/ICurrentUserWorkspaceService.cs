using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public interface ICurrentUserWorkspaceService
    {
        Task<MeWorkspaceDto> GetMeAsync(CancellationToken cancellationToken);

        Task<TrustProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken);

        Task<IReadOnlyList<VehicleWorkspaceDto>> GetVehiclesAsync(CancellationToken cancellationToken);

        Task<VehicleWorkspaceDto> AddVehicleAsync(UpsertVehicleRequest request, CancellationToken cancellationToken);

        Task<VehicleWorkspaceDto?> UpdateVehicleAsync(int vehicleId, UpsertVehicleRequest request, CancellationToken cancellationToken);
    }
}
