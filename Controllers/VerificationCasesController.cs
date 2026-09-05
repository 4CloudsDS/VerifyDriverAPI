using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VerifyDriversAPI.Dtos;
using VerifyDriversAPI.Services;

namespace VerifyDriversAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class VerificationCasesController : ControllerBase
    {
        private readonly IVerificationCaseService _verificationCases;

        public VerificationCasesController(IVerificationCaseService verificationCases)
        {
            _verificationCases = verificationCases;
        }

        [HttpPost]
        [ProducesResponseType(typeof(VerificationCaseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<VerificationCaseDto>> Create(
            CreateVerificationCaseRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var verificationCase = await _verificationCases.CreateAsync(request, cancellationToken);
                return CreatedAtAction(nameof(Get), new { caseId = verificationCase.CaseId }, verificationCase);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid verification case.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet("{caseId:guid}")]
        [ProducesResponseType(typeof(VerificationCaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VerificationCaseDto>> Get(Guid caseId, CancellationToken cancellationToken)
        {
            var verificationCase = await _verificationCases.GetAsync(caseId, cancellationToken);
            return verificationCase is null
                ? Problem(title: "Verification case not found.", detail: $"No verification case exists for id {caseId}.", statusCode: StatusCodes.Status404NotFound)
                : verificationCase;
        }

        [HttpPatch("{caseId:guid}/status")]
        [ProducesResponseType(typeof(VerificationCaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VerificationCaseDto>> UpdateStatus(
            Guid caseId,
            UpdateVerificationCaseStatusRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var verificationCase = await _verificationCases.UpdateStatusAsync(caseId, request.Status, cancellationToken);
                return verificationCase is null
                    ? Problem(title: "Verification case not found.", detail: $"No verification case exists for id {caseId}.", statusCode: StatusCodes.Status404NotFound)
                    : verificationCase;
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid verification status.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }

        [HttpPost("{caseId:guid}/dispute")]
        [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DisputeDto>> Dispute(
            Guid caseId,
            DisputeRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var dispute = await _verificationCases.DisputeAsync(caseId, request, cancellationToken);
                return dispute is null
                    ? Problem(title: "Verification case not found.", detail: $"No verification case exists for id {caseId}.", statusCode: StatusCodes.Status404NotFound)
                    : Accepted(dispute);
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid verification dispute.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }
    }
}
