namespace VerifyDriversAPI.Dtos
{
    public sealed record TrustProfileDto(
        int UserId,
        string Name,
        string Role,
        decimal Rating,
        int TrustScore,
        string RiskLevel,
        VehicleSummaryDto? Vehicle,
        PartnerSummaryDto? Partner,
        IReadOnlyList<string> Platforms,
        IReadOnlyList<string> Signals,
        FeedbackSummaryDto FeedbackSummary,
        IReadOnlyList<string> RankingSignals);

    public sealed record VehicleSummaryDto(
        int VehicleId,
        string Registration,
        string Description);

    public sealed record PartnerSummaryDto(
        int PartnerId,
        string Name);

    public sealed record FeedbackSummaryDto(
        int TotalReports,
        int PendingModeration,
        string Summary);
}
