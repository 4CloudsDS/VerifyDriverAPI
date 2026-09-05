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
        public async Task<ActionResult<StructuredFeedbackDto>> Submit(
            StructuredFeedbackRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var feedback = await _feedback.SubmitAsync(request, cancellationToken);
                return Accepted(feedback);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid feedback submission.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{feedbackId:guid}/dispute")]
        [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DisputeDto>> Dispute(
            Guid feedbackId,
            DisputeRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var dispute = await _feedback.DisputeAsync(feedbackId, request, cancellationToken);
                return dispute is null
                    ? Problem(title: "Feedback not found.", detail: $"No feedback exists for id {feedbackId}.", statusCode: StatusCodes.Status404NotFound)
                    : Accepted(dispute);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid feedback dispute.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }
    }
}
