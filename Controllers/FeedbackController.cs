using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class FeedbackController : ControllerBase
    {
        private readonly IFeedbackModerationService _feedback;

        public FeedbackController(IFeedbackModerationService feedback)
        {
            _feedback = feedback;
        }

        [HttpPost]
        [ProducesResponseType(typeof(StructuredFeedbackDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<StructuredFeedbackDto> Submit(StructuredFeedbackRequest request)
        {
            try
            {
                var feedback = _feedback.Submit(request);
                return Accepted(feedback);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid feedback submission.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }
    }
}
