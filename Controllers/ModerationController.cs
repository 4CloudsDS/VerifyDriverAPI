using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        public ActionResult<ModerationQueueDto> GetQueue()
        {
            return new ModerationQueueDto(_feedback.GetQueue(), _verificationCases.GetQueue(), [], []);
        }
    }
}
