using System.Security.Claims;

namespace VerifyDriversAPI.Services
{
    public sealed class CurrentUserContext : ICurrentUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public CurrentUserContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public int UserId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var claimValue = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(claimValue, out var claimUserId) && claimUserId > 0)
                {
                    return claimUserId;
                }

                if (httpContext?.Request.Headers.TryGetValue("X-User-Id", out var headerValue) == true
                    && int.TryParse(headerValue.ToString(), out var headerUserId)
                    && headerUserId > 0)
                {
                    return headerUserId;
                }

                return _configuration.GetValue("Authentication:DevelopmentUserId", 10);
            }
        }
    }
}
