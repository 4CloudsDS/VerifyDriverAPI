using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public sealed class MeController : ControllerBase
    {
        private readonly ICurrentUserWorkspaceService _workspace;

        public MeController(ICurrentUserWorkspaceService workspace)
        {
            _workspace = workspace;
        }

        [HttpGet]
        [ProducesResponseType(typeof(MeWorkspaceDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<MeWorkspaceDto>> Get(CancellationToken cancellationToken)
        {
            try
            {
                return await _workspace.GetMeAsync(cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Problem(title: "Current user workspace unavailable.", detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
        }

        [HttpPatch("profile")]
        [ProducesResponseType(typeof(TrustProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TrustProfileDto>> UpdateProfile(
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
