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
        DateTimeOffset SubmittedAtUtc);
}
