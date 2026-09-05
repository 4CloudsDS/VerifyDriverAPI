using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Models;

namespace VerifyDriversAPI.Services
{
    public sealed class CurrentUserWorkspaceService : ICurrentUserWorkspaceService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;
        private readonly ITrustProfileService _profiles;
        private readonly IVerificationCaseService _verificationCases;

        public CurrentUserWorkspaceService(
            AppDbContext context,
            ICurrentUserContext currentUser,
            ITrustProfileService profiles,
            IVerificationCaseService verificationCases)
        {
            _context = context;
            _currentUser = currentUser;
            _profiles = profiles;
            _verificationCases = verificationCases;
        }

        public async Task<MeWorkspaceDto> GetMeAsync(CancellationToken cancellationToken)
        {
            var user = await GetCurrentUserAsync(cancellationToken);
            var profile = await _profiles.GetProfileAsync(user.uID, cancellationToken);
            var vehicles = await GetVehiclesAsync(cancellationToken);
            var requests = (await _verificationCases.GetQueueAsync(cancellationToken))
                .Where(item => item.PrimaryProfileId == user.uID
                    || (!string.IsNullOrWhiteSpace(item.Counterparty)
                        && item.Counterparty.Contains(user.uNames, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var relationships = await GetRelationshipsAsync(user.uID, cancellationToken);

            return new MeWorkspaceDto(
                profile,
                new ProfileEditorDto(user.uID, user.uNames, user.uRating, user.uVID, user.uPartner_ID, user.uUsertype_ID),
                vehicles,
                requests,
                relationships);
        }

        public async Task<TrustProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            ValidateProfile(request);
            var user = await GetCurrentUserAsync(cancellationToken);
            await EnsureVehicleVisibleToCurrentUserAsync(request.VehicleId, cancellationToken);
            await EnsureReferenceExistsAsync(request.UserTypeId, "user type", _context.UserTypes.AnyAsync(item => item.U_T_ID == request.UserTypeId, cancellationToken));
            await EnsureReferenceExistsAsync(request.PartnerId, "partner", _context.Partners.AnyAsync(item => item.pID == request.PartnerId, cancellationToken));

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE _User
                SET uNames = {request.Name.Trim()},
                    uRating = {request.Rating},
                    uVID = {request.VehicleId},
                    uPartner_ID = {request.PartnerId},
                    uUsertype_ID = {request.UserTypeId}
                WHERE uID = {user.uID};
                """,
                cancellationToken);

            var updated = await _profiles.GetProfileAsync(user.uID, cancellationToken);
            return updated ?? throw new InvalidOperationException("Updated profile could not be loaded.");
        }

        public async Task<IReadOnlyList<VehicleWorkspaceDto>> GetVehiclesAsync(CancellationToken cancellationToken)
        {
            var user = await GetCurrentUserAsync(cancellationToken);
            var linkedVehicleIds = await GetOwnerVehicleIdsAsync(user.uID, cancellationToken);
            if (user.uVID > 0)
            {
                linkedVehicleIds.Add(user.uVID);
            }

            if (linkedVehicleIds.Count == 0)
            {
                return [];
            }

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Include(vehicle => vehicle.Platform)
                .Include(vehicle => vehicle.Partner)
                .Where(vehicle => linkedVehicleIds.Contains(vehicle.vID))
                .OrderBy(vehicle => vehicle.vregistration)
                .ToListAsync(cancellationToken);

            return vehicles.Select(vehicle => MapVehicle(vehicle, user.uID)).ToList();
        }

        public async Task<VehicleWorkspaceDto> AddVehicleAsync(UpsertVehicleRequest request, CancellationToken cancellationToken)
        {
            ValidateVehicle(request);
            var user = await GetCurrentUserAsync(cancellationToken);
            await EnsureReferenceExistsAsync(request.PlatformId, "platform", _context.Platforms.AnyAsync(item => item.pID == request.PlatformId, cancellationToken));
            await EnsureReferenceExistsAsync(request.PartnerId, "partner", _context.Partners.AnyAsync(item => item.pID == request.PartnerId, cancellationToken));

            var vehicle = new Vehicle
            {
                vregistration = request.Registration.Trim(),
                vMake = request.Make.Trim(),
                vModel_name = request.ModelName.Trim(),
                vModel_year = request.ModelYear.Trim(),
                vPlatform_ID = request.PlatformId,
                vPartner_ID = request.PartnerId
            };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync(cancellationToken);
            await LinkVehicleToOwnerAsync(user.uID, vehicle.vID, cancellationToken);

            var saved = await _context.Vehicles
                .AsNoTracking()
                .Include(item => item.Platform)
                .Include(item => item.Partner)
                .FirstAsync(item => item.vID == vehicle.vID, cancellationToken);

            return MapVehicle(saved, user.uID);
        }

        public async Task<VehicleWorkspaceDto?> UpdateVehicleAsync(int vehicleId, UpsertVehicleRequest request, CancellationToken cancellationToken)
        {
            ValidateVehicle(request);
            var user = await GetCurrentUserAsync(cancellationToken);
            await EnsureVehicleVisibleToCurrentUserAsync(vehicleId, cancellationToken);
            await EnsureReferenceExistsAsync(request.PlatformId, "platform", _context.Platforms.AnyAsync(item => item.pID == request.PlatformId, cancellationToken));
            await EnsureReferenceExistsAsync(request.PartnerId, "partner", _context.Partners.AnyAsync(item => item.pID == request.PartnerId, cancellationToken));

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(item => item.vID == vehicleId, cancellationToken);
            if (vehicle is null)
            {
                return null;
            }

            vehicle.vregistration = request.Registration.Trim();
            vehicle.vMake = request.Make.Trim();
            vehicle.vModel_name = request.ModelName.Trim();
            vehicle.vModel_year = request.ModelYear.Trim();
            vehicle.vPlatform_ID = request.PlatformId;
            vehicle.vPartner_ID = request.PartnerId;
            await _context.SaveChangesAsync(cancellationToken);
            await LinkVehicleToOwnerAsync(user.uID, vehicle.vID, cancellationToken);

            return MapVehicle(vehicle, user.uID);
        }

        private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .Include(item => item.UserType)
                .FirstOrDefaultAsync(item => item.uID == _currentUser.UserId, cancellationToken);

            return user ?? throw new InvalidOperationException($"Current user {_currentUser.UserId} does not exist.");
        }

        private async Task<HashSet<int>> GetOwnerVehicleIdsAsync(int userId, CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT vehicle_id FROM owner_vehicle_links WHERE owner_user_id = @userId ORDER BY vehicle_id;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@userId";
            parameter.Value = userId;
            command.Parameters.Add(parameter);

            var vehicleIds = new HashSet<int>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                vehicleIds.Add(reader.GetInt32(0));
            }

            return vehicleIds;
        }

        private async Task EnsureVehicleVisibleToCurrentUserAsync(int vehicleId, CancellationToken cancellationToken)
        {
            var vehicles = await GetVehiclesAsync(cancellationToken);
            if (!vehicles.Any(item => item.VehicleId == vehicleId))
            {
                throw new UnauthorizedAccessException($"Vehicle {vehicleId} is not owned by the current user.");
            }
        }

        private async Task LinkVehicleToOwnerAsync(int ownerUserId, int vehicleId, CancellationToken cancellationToken)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO owner_vehicle_links (owner_user_id, vehicle_id, created_at_utc)
                SELECT {ownerUserId}, {vehicleId}, {DateTimeOffset.UtcNow.ToString("O")}
                WHERE NOT EXISTS (
                    SELECT 1 FROM owner_vehicle_links WHERE owner_user_id = {ownerUserId} AND vehicle_id = {vehicleId}
                );
                """,
                cancellationToken);
        }

        private async Task<IReadOnlyList<MarketplaceRelationshipDto>> GetRelationshipsAsync(int userId, CancellationToken cancellationToken)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SELECT relationship_id, driver_user_id, owner_user_id, vehicle_id, platform_id, partner_id,
                       relationship_type, verification_status, availability_status, created_at_utc
                FROM marketplace_relationships
                WHERE driver_user_id = @userId OR owner_user_id = @userId
                ORDER BY created_at_utc DESC;
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@userId";
            parameter.Value = userId;
            command.Parameters.Add(parameter);

            var relationships = new List<MarketplaceRelationshipDto>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                relationships.Add(new MarketplaceRelationshipDto(
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    reader.GetString(6),
                    reader.GetString(7),
                    reader.GetString(8),
                    DateTimeOffset.Parse(reader.GetString(9))));
            }

            return relationships;
        }

        private static VehicleWorkspaceDto MapVehicle(Vehicle vehicle, int ownerUserId)
        {
            var description = string.Join(" ", new[] { vehicle.vMake, vehicle.vModel_name, vehicle.vModel_year }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            return new VehicleWorkspaceDto(
                vehicle.vID,
                vehicle.vregistration,
                string.IsNullOrWhiteSpace(description) ? "Vehicle pending" : description,
                vehicle.vPlatform_ID,
                vehicle.Platform?.pName ?? $"Platform {vehicle.vPlatform_ID}",
                vehicle.vPartner_ID,
                vehicle.Partner?.pName ?? $"Partner {vehicle.vPartner_ID}",
                ownerUserId);
        }

        private static void ValidateProfile(UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Profile name is required.", nameof(request));
            }

            if (request.Rating is < 0 or > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Rating must be between 0 and 5.");
            }
        }

        private static void ValidateVehicle(UpsertVehicleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Registration))
            {
                throw new ArgumentException("Vehicle registration is required.", nameof(request));
            }

            if (request.PlatformId <= 0 || request.PartnerId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Platform and partner ids must be positive.");
            }
        }

        private static async Task EnsureReferenceExistsAsync(int id, string label, Task<bool> existsTask)
        {
            if (id <= 0 || !await existsTask)
            {
                throw new ArgumentException($"Referenced {label} {id} does not exist.");
            }
        }
    }
}
