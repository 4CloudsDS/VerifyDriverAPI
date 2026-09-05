using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ModeratorOnly")]
    public sealed class ModerationController : ControllerBase
    {
        private readonly IFeedbackModerationService _feedback;
        private readonly IVerificationCaseService _verificationCases;

        public ModerationController(
            IFeedbackModerationService feedback,
            IVerificationCaseService verificationCases)
        {
            _feedback = feedback;
            _verificationCases = verificationCases;
        }

        [HttpGet("queue")]
        [ProducesResponseType(typeof(ModerationQueueDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ModerationQueueDto>> GetQueue(CancellationToken cancellationToken)
        {
            return new ModerationQueueDto(
                await _feedback.GetQueueAsync(cancellationToken),
                await _verificationCases.GetQueueAsync(cancellationToken),
                [],
                []);
        }
    }
}
