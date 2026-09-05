using VerifyDriversAPI.Dtos;

namespace VerifyDriversAPI.Services
{
    public static class VerificationRuleCatalog
    {
        private static readonly IReadOnlyList<string> DriverCaseTypes =
        [
            "Driver identity",
            "Driver to owner relationship",
            "Driver to vehicle relationship",
            "Employment or referral history",
            "Platform or fleet association"
        ];

        private static readonly IReadOnlyList<string> OwnerCaseTypes =
        [
            "Vehicle to owner relationship",
            "Driver to owner relationship",
            "Employment or referral history",
            "Platform or fleet association"
        ];

        private static readonly IReadOnlyList<string> FleetCaseTypes =
        [
            "Vehicle to owner relationship",
            "Employment or referral history",
            "Platform or fleet association"
        ];

        private static readonly IReadOnlyList<string> PlatformCaseTypes =
        [
            "Platform or fleet association",
            "Employment or referral history"
        ];

        public static VerificationRulesResponse ForProfileType(string? profileType)
        {
            var type = Normalize(profileType);
            return type switch
            {
                "Owner" => new(type, OwnerCaseTypes, ["Vehicle proof"], "Owner profiles should verify vehicle ownership and counterparty relationship claims, not driver licence credentials."),
                "Fleet" => new(type, FleetCaseTypes, ["Vehicle proof", "Referral"], "Fleet profiles should verify vehicle ownership, partner authority, and employment or platform associations."),
                "Platform" => new(type, PlatformCaseTypes, ["Referral"], "Platform profiles should verify operating associations, fleet coverage, and contested relationship claims."),
                _ => new("Driver", DriverCaseTypes, ["Driver licence"], "Driver profiles should verify identity and licence evidence before relationship approval.")
            };
        }

        public static string Normalize(string? profileType)
        {
            if (string.IsNullOrWhiteSpace(profileType))
            {
                return "Driver";
            }

            if (profileType.Contains("owner", StringComparison.OrdinalIgnoreCase))
            {
                return "Owner";
            }

            if (profileType.Contains("fleet", StringComparison.OrdinalIgnoreCase))
            {
                return "Fleet";
            }

            if (profileType.Contains("platform", StringComparison.OrdinalIgnoreCase))
            {
                return "Platform";
            }

            return "Driver";
        }
    }
}
