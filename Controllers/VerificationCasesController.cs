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
        public ActionResult<VerificationCaseDto> Create(CreateVerificationCaseRequest request)
        {
            try
            {
                var verificationCase = _verificationCases.Create(request);
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
        public ActionResult<VerificationCaseDto> Get(Guid caseId)
        {
            var verificationCase = _verificationCases.Get(caseId);
            return verificationCase is null ? NotFound() : verificationCase;
        }

        [HttpPatch("{caseId:guid}/status")]
        [ProducesResponseType(typeof(VerificationCaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<VerificationCaseDto> UpdateStatus(
            Guid caseId,
            UpdateVerificationCaseStatusRequest request)
        {
            try
            {
                var verificationCase = _verificationCases.UpdateStatus(caseId, request.Status);
                return verificationCase is null ? NotFound() : verificationCase;
            }
            catch (ArgumentException ex)
            {
                return Problem(title: "Invalid verification status.", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }
    }
}
