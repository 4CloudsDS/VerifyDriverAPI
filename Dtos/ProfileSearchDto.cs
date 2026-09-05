namespace VerifyDriversAPI.Dtos
{
    public sealed record ProfileSearchRequest(
        string? Query,
        string? Mode,
        string? Intent,
        string? RelationshipType);

    public sealed record ProfileSearchResponse(
        string Query,
        string Mode,
        string? Intent,
        string? RelationshipType,
        int ResultCount,
        IReadOnlyList<TrustProfileDto> Results);
}
