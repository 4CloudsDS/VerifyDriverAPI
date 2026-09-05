namespace VerifyDriversAPI.Dtos
{
    public sealed record MeWorkspaceDto(
        TrustProfileDto? Profile,
        ProfileEditorDto ProfileEditor,
        IReadOnlyList<VehicleWorkspaceDto> Vehicles,
        IReadOnlyList<VerificationCaseDto> RelationshipRequests,
        IReadOnlyList<MarketplaceRelationshipDto> Relationships);

    public sealed record ProfileEditorDto(
        int UserId,
        string Name,
        decimal Rating,
        int VehicleId,
        int PartnerId,
        int UserTypeId);

    public sealed record UpdateProfileRequest(
        string Name,
        decimal Rating,
        int VehicleId,
        int PartnerId,
        int UserTypeId);

    public sealed record UpsertVehicleRequest(
        string Registration,
        string Make,
        string ModelName,
        string ModelYear,
        int PlatformId,
        int PartnerId);

    public sealed record VehicleWorkspaceDto(
        int VehicleId,
        string Registration,
        string Description,
        int PlatformId,
        string Platform,
        int PartnerId,
        string Partner,
        int OwnerUserId);

    public sealed record RelationshipUpdateRequest(
        string VerificationStatus,
        string AvailabilityStatus);

    public sealed record VerificationRulesResponse(
        string ProfileType,
        IReadOnlyList<string> AllowedCaseTypes,
        IReadOnlyList<string> RequiredEvidenceTypes,
        string Guidance);

    public sealed record AdminDashboardDto(
        string MarketFilter,
        ModerationQueueDto Moderation,
        TrustSignalSummaryDto TrustSignals,
        SeedCoverageDto SeedCoverage);

    public sealed record TrustSignalSummaryDto(
        int ProfilesMonitored,
        int ReviewRisk,
        int HighRisk,
        int AverageTrustScore);

    public sealed record SeedCoverageDto(
        int TotalRelationships,
        int AvailableRelationships,
        int VerifiedRelationships,
        IReadOnlyList<string> RelationshipTypes);
}
