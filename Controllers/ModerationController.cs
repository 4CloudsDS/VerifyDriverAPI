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
        private readonly ITrustProfileService _profiles;

        public ModerationController(
            IFeedbackModerationService feedback,
            IVerificationCaseService verificationCases,
            ITrustProfileService profiles)
        {
            _feedback = feedback;
            _verificationCases = verificationCases;
            _profiles = profiles;
        }

        [HttpGet("queue")]
        [ProducesResponseType(typeof(ModerationQueueDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ModerationQueueDto>> GetQueue(
            [FromQuery] string? market,
            CancellationToken cancellationToken)
        {
            return new ModerationQueueDto(
                FilterFeedback(await _feedback.GetQueueAsync(cancellationToken), market).ToList(),
                FilterCases(await _verificationCases.GetQueueAsync(cancellationToken), market).ToList(),
                [],
                []);
        }

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<AdminDashboardDto>> GetDashboard(
            [FromQuery] string? market,
            CancellationToken cancellationToken)
        {
            var marketFilter = string.IsNullOrWhiteSpace(market) ? "All drivers" : market.Trim();
            var profiles = (await _profiles.GetProfilesAsync(cancellationToken))
                .Where(profile => MatchesMarket(profile.Role, marketFilter))
                .ToList();
            var queue = await GetQueue(marketFilter, cancellationToken);
            var moderation = queue.Value ?? new ModerationQueueDto([], [], [], []);

            return new AdminDashboardDto(
                marketFilter,
                moderation,
                new TrustSignalSummaryDto(
                    profiles.Count,
                    profiles.Count(profile => profile.RiskLevel.Equals("Review", StringComparison.OrdinalIgnoreCase)),
                    profiles.Count(profile => profile.RiskLevel.Equals("High", StringComparison.OrdinalIgnoreCase)),
                    profiles.Count == 0 ? 0 : (int)Math.Round(profiles.Average(profile => profile.TrustScore))),
                new SeedCoverageDto(
                    profiles.Count,
                    profiles.Count(profile => profile.Platforms.Count > 0),
                    profiles.Count(profile => profile.RiskLevel.Equals("Low", StringComparison.OrdinalIgnoreCase)),
                    profiles.Select(profile => profile.Role).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item).ToList()));
        }

        private static IEnumerable<StructuredFeedbackDto> FilterFeedback(
            IReadOnlyList<StructuredFeedbackDto> feedback,
            string? market)
        {
            return feedback.Where(item =>
                    IsActionable(item.ModerationStatus)
                    || IsActionable(item.DisputeStatus))
                .Where(item => string.IsNullOrWhiteSpace(market)
                    || market.Equals("All drivers", StringComparison.OrdinalIgnoreCase)
                    || Contains(item.Context, market)
                    || Contains(item.Category, market)
                    || Contains(item.RelatedEntity, market));
        }

        private static IEnumerable<VerificationCaseDto> FilterCases(
            IReadOnlyList<VerificationCaseDto> cases,
            string? market)
        {
            return cases.Where(item => IsActionable(item.Status))
                .Where(item => string.IsNullOrWhiteSpace(market)
                    || market.Equals("All drivers", StringComparison.OrdinalIgnoreCase)
                    || Contains(item.CaseType, market)
                    || Contains(item.RelationshipContext, market)
                    || Contains(item.Counterparty, market));
        }

        private static bool MatchesMarket(string role, string market)
        {
            return market.Equals("All drivers", StringComparison.OrdinalIgnoreCase)
                || role.Contains(market, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsActionable(string status)
        {
            return status.Contains("Pending", StringComparison.OrdinalIgnoreCase)
                || status.Contains("Review", StringComparison.OrdinalIgnoreCase)
                || status.Contains("Disputed", StringComparison.OrdinalIgnoreCase)
                || status.Contains("Rejected", StringComparison.OrdinalIgnoreCase)
                || status.Contains("Submitted", StringComparison.OrdinalIgnoreCase);
        }

        private static bool Contains(string? value, string query)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains(query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
