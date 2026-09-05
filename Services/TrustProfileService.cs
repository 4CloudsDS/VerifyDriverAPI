using Microsoft.EntityFrameworkCore;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Models;

namespace VerifyDriversAPI.Services
{
    public sealed class TrustProfileService : ITrustProfileService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserContext _currentUser;

        public TrustProfileService(AppDbContext context, ICurrentUserContext currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<TrustProfileDto>> GetProfilesAsync(CancellationToken cancellationToken)
        {
            var users = await ProfileQuery().ToListAsync(cancellationToken);
            return users.Select(MapProfile).ToList();
        }

        public async Task<TrustProfileDto?> GetProfileAsync(int userId, CancellationToken cancellationToken)
        {
            var user = await ProfileQuery().FirstOrDefaultAsync(item => item.uID == userId, cancellationToken);
            return user is null ? null : MapProfile(user);
        }

        public async Task<ProfileSearchResponse> SearchAsync(ProfileSearchRequest request, CancellationToken cancellationToken)
        {
            var query = request.Query?.Trim() ?? string.Empty;
            var mode = string.IsNullOrWhiteSpace(request.Mode) ? "profile" : request.Mode.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                return new ProfileSearchResponse(query, mode, request.Intent, request.RelationshipType, 0, []);
            }

            var profiles = await GetProfilesAsync(cancellationToken);
            var isOpportunity = mode.Equals("opportunity", StringComparison.OrdinalIgnoreCase);
            var excludeCurrentUser = ShouldExcludeCurrentUser(mode);
            var matches = profiles
                .Where(profile => excludeCurrentUser ? profile.UserId != _currentUser.UserId : true)
                .Where(profile => MatchesByMode(profile, query, mode, isOpportunity))
                .Where(profile => MatchesIntent(profile, request.Intent))
                .Select(profile => isOpportunity ? profile with { RankingSignals = OpportunitySignals(profile, request) } : profile)
                .OrderByDescending(profile => isOpportunity ? profile.TrustScore : 0)
                .ThenBy(profile => RiskSort(profile.RiskLevel))
                .ThenBy(profile => profile.Name)
                .ToList();

            return new ProfileSearchResponse(query, mode, request.Intent, request.RelationshipType, matches.Count, matches);
        }

        private IQueryable<User> ProfileQuery()
        {
            return _context.Users
                .AsNoTracking()
                .Include(user => user.Vehicle)
                    .ThenInclude(vehicle => vehicle!.Platform)
                .Include(user => user.Vehicle)
                    .ThenInclude(vehicle => vehicle!.Partner)
                .Include(user => user.Partner)
                .Include(user => user.UserType);
        }

        private static TrustProfileDto MapProfile(User user)
        {
            var trustScore = ToTrustScore(user.uRating);
            var riskLevel = trustScore >= 80 ? "Low" : trustScore >= 60 ? "Review" : "High";
            var role = string.IsNullOrWhiteSpace(user.UserType?.U_T_description)
                ? "Professional driver"
                : user.UserType.U_T_description;
            var vehicle = user.Vehicle is null
                ? null
                : new VehicleSummaryDto(
                    user.Vehicle.vID,
                    user.Vehicle.vregistration,
                    string.Join(" ", new[] { user.Vehicle.vMake, user.Vehicle.vModel_name, user.Vehicle.vModel_year }
                        .Where(value => !string.IsNullOrWhiteSpace(value))));
            var partnerName = user.Partner?.pName ?? user.Vehicle?.Partner?.pName;
            var partner = partnerName is null
                ? null
                : new PartnerSummaryDto(user.Partner?.pID ?? user.Vehicle?.Partner?.pID ?? 0, partnerName);
            var platforms = new[] { user.Vehicle?.Platform?.pName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList();
            var signals = new List<string>
            {
                $"Rating {user.uRating:0.0}",
                $"Risk {riskLevel}",
                vehicle?.Registration ?? "Vehicle pending"
            };

            return new TrustProfileDto(
                user.uID,
                string.IsNullOrWhiteSpace(user.uNames) ? $"Profile {user.uID}" : user.uNames,
                role,
                user.uRating,
                trustScore,
                riskLevel,
                vehicle,
                partner,
                platforms,
                signals,
                new FeedbackSummaryDto(0, 0, "No moderated feedback summary is available yet."),
                ["Trust score", "Risk level", "Vehicle context"]);
        }

        private static int ToTrustScore(decimal rating)
        {
            var score = rating <= 5 ? (int)Math.Round(rating * 20) : (int)Math.Round(rating);
            return Math.Clamp(score, 0, 100);
        }

        private static bool MatchesKnownProfile(TrustProfileDto profile, string query)
        {
            return Contains(profile.Name, query)
                || Contains(profile.Vehicle?.Registration, query);
        }

        private static bool MatchesOpportunity(TrustProfileDto profile, string query)
        {
            return MatchesKnownProfile(profile, query)
                || Contains(profile.Role, query)
                || Contains(profile.Vehicle?.Description, query)
                || Contains(profile.Partner?.Name, query)
                || profile.Platforms.Any(platform => Contains(platform, query))
                || profile.Signals.Any(signal => Contains(signal, query));
        }

        private static bool MatchesByMode(TrustProfileDto profile, string query, string mode, bool isOpportunity)
        {
            if (isOpportunity || mode.Equals("verification", StringComparison.OrdinalIgnoreCase))
            {
                return MatchesOpportunity(profile, query);
            }

            return MatchesKnownProfile(profile, query);
        }

        private static bool ShouldExcludeCurrentUser(string mode)
        {
            return mode.Equals("profile", StringComparison.OrdinalIgnoreCase)
                || mode.Equals("opportunity", StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesIntent(TrustProfileDto profile, string? intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
            {
                return true;
            }

            return intent.Trim() switch
            {
                "looking-for-driver" => IsAny(profile.Role, "Rideshare", "Delivery", "Trucking", "Driver"),
                "driver-looking-for-owner" => IsAny(profile.Role, "Owner", "Fleet"),
                "fleet-owner-looking-for-partners" => IsAny(profile.Role, "Owner", "Fleet", "Platform"),
                "platform-vetting-profiles" => IsAny(profile.Role, "Rideshare", "Delivery", "Trucking", "Owner", "Fleet"),
                _ => true
            };
        }

        private static bool IsAny(string value, params string[] expected)
        {
            return expected.Any(item => value.Contains(item, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<string> OpportunitySignals(TrustProfileDto profile, ProfileSearchRequest request)
        {
            var signals = new List<string>
            {
                $"Trust score {profile.TrustScore}",
                $"Risk {profile.RiskLevel}",
                profile.Vehicle is null ? "Vehicle context pending" : $"Vehicle {profile.Vehicle.Registration}"
            };

            if (!string.IsNullOrWhiteSpace(request.RelationshipType))
            {
                signals.Add($"Relationship fit: {request.RelationshipType}");
            }

            return signals;
        }

        private static int RiskSort(string riskLevel)
        {
            return riskLevel.Equals("Low", StringComparison.OrdinalIgnoreCase) ? 0
                : riskLevel.Equals("Review", StringComparison.OrdinalIgnoreCase) ? 1
                : 2;
        }

        private static bool Contains(string? value, string query)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains(query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
