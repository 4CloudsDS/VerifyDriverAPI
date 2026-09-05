using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Data;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ProfilesController : ControllerBase
    {
        private readonly ITrustProfileService _profiles;
        private readonly ICurrentUserWorkspaceService _workspace;

        public ProfilesController(ITrustProfileService profiles, ICurrentUserWorkspaceService workspace)
        {
            _profiles = profiles;
            _workspace = workspace;
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

        [HttpPatch("me")]
        [ProducesResponseType(typeof(TrustProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TrustProfileDto>> UpdateCurrentUserProfile(
            UpdateProfileRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _workspace.UpdateProfileAsync(request, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid profile update.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Problem(title: "Profile update not authorized.", detail: ex.Message, statusCode: StatusCodes.Status403Forbidden);
            }
        }
    }
}
