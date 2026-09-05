using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ProfilesController : ControllerBase
    {
        private readonly ITrustProfileService _profiles;

        public ProfilesController(ITrustProfileService profiles)
        {
            _profiles = profiles;
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(ProfileSearchResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<ProfileSearchResponse>> Search(
            [FromQuery] string? query,
            [FromQuery] string? mode,
            [FromQuery] string? intent,
            [FromQuery] string? relationshipType,
            CancellationToken cancellationToken)
        {
            return await _profiles.SearchAsync(
                new ProfileSearchRequest(query, mode, intent, relationshipType),
                cancellationToken);
        }

        [HttpGet("trust/{userId:int}")]
        [ProducesResponseType(typeof(TrustProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TrustProfileDto>> GetTrustProfile(
            int userId,
            CancellationToken cancellationToken)
        {
            var profile = await _profiles.GetProfileAsync(userId, cancellationToken);
            return profile is null ? NotFound() : profile;
        }
    }
}
