using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace VerifyDriversAPI.Security
{
    public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
    }

    public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public const string SchemeName = "ApiKey";

        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration configuration,
            IHostEnvironment environment)
            : base(options, logger, encoder)
        {
            _configuration = configuration;
            _environment = environment;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (_environment.IsDevelopment()
                && !_configuration.GetSection("Authentication:ApiKeys").GetChildren().Any())
            {
                return Task.FromResult(Success(
                    "development-admin",
                    _configuration.GetValue("Authentication:DevelopmentUserId", 10),
                    ["Admin", "Moderator", "PublicUser", "Driver", "Owner", "Fleet", "Platform"]));
            }

            if (!Request.Headers.TryGetValue("X-API-Key", out var providedKey)
                || string.IsNullOrWhiteSpace(providedKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            foreach (var apiKey in _configuration.GetSection("Authentication:ApiKeys").GetChildren())
            {
                var configuredKey = apiKey["Key"];
                if (!string.Equals(configuredKey, providedKey.ToString(), StringComparison.Ordinal))
                {
                    continue;
                }

                var roles = apiKey.GetSection("Roles").Get<string[]>() ?? ["PublicUser"];
                var userId = int.TryParse(apiKey["UserId"], out var configuredUserId) ? configuredUserId : 10;
                return Task.FromResult(Success(apiKey["Name"] ?? "api-client", userId, roles));
            }

            return Task.FromResult(AuthenticateResult.Fail("The supplied API key is invalid."));
        }

        private static AuthenticateResult Success(string name, int userId, IReadOnlyList<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, name),
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            var identity = new ClaimsIdentity(claims, SchemeName);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
        }
    }
}
