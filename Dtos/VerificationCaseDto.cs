namespace VerifyDriversAPI.Dtos
{
    public sealed record CreateVerificationCaseRequest(
        string CaseType,
        string RelationshipContext,
        int PrimaryProfileId,
        string? Counterparty,
        IReadOnlyList<DocumentEvidenceRequest>? Evidence,
        IReadOnlyList<CounterpartyConfirmationRequest>? Confirmations);

    public sealed record DocumentEvidenceRequest(
        string DocumentType,
        string FileName,
        string? ContentType,
        long? SizeBytes);

    public sealed record CounterpartyConfirmationRequest(
        string Counterparty,
        string Claim,
        string? State);

    public sealed record UpdateVerificationCaseStatusRequest(string Status);

    public sealed record VerificationCaseDto(
        Guid CaseId,
        string CaseType,
        string RelationshipContext,
        int PrimaryProfileId,
        string? Counterparty,
        string Status,
        IReadOnlyList<DocumentEvidenceDto> Evidence,
        IReadOnlyList<CounterpartyConfirmationDto> Confirmations,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        string PrivacyStatus = "PrivateDocuments");

    public sealed record DocumentEvidenceDto(
        Guid DocumentId,
        string DocumentType,
        string FileName,
        string? ContentType,
        long? SizeBytes,
        bool PubliclyVisible);

    public sealed record CounterpartyConfirmationDto(
        Guid ConfirmationId,
        string Counterparty,
        string Claim,
        string State);

    public sealed record ModerationQueueDto(
        IReadOnlyList<StructuredFeedbackDto> Feedback,
        IReadOnlyList<VerificationCaseDto> VerificationCases,
        IReadOnlyList<string> DuplicateProfiles,
        IReadOnlyList<string> SuspiciousActivity);

    public sealed record MarketplaceRelationshipDto(
        string RelationshipId,
        int DriverUserId,
        int? OwnerUserId,
        int? VehicleId,
        int? PlatformId,
        int? PartnerId,
        string RelationshipType,
        string VerificationStatus,
        string AvailabilityStatus,
        DateTimeOffset CreatedAtUtc);
}
