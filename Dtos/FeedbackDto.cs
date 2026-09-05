namespace VerifyDriversAPI.Dtos
{
    public sealed record StructuredFeedbackRequest(
        string Category,
        int Severity,
        string Context,
        int? RelatedProfileId,
        string? RelatedEntity,
        string? SubmitterType);

    public sealed record StructuredFeedbackDto(
        Guid FeedbackId,
        string Category,
        int Severity,
        string Context,
        int? RelatedProfileId,
        string? RelatedEntity,
        string SubmitterType,
        string ModerationStatus,
        DateTimeOffset SubmittedAtUtc,
        string DisputeStatus = "None",
        string PrivacyStatus = "ModeratedPrivateUntilApproved");

    public sealed record DisputeRequest(
        int ProfileId,
        string Reason,
        string RequestedBy);

    public sealed record DisputeDto(
        Guid DisputeId,
        Guid TargetId,
        int ProfileId,
        string Reason,
        string RequestedBy,
        string Status,
        DateTimeOffset CreatedAtUtc);
}
