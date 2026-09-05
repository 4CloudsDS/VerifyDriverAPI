using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using VerifyDriversAPI.Dtos;
using Xunit;

namespace VerifyDriverAPI.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_and_users_endpoints_return_success()
    {
        using var health = await _client.GetAsync("/api/Health");
        using var users = await _client.GetAsync("/api/Users");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
    }

    [Fact]
    public async Task Profile_search_supports_known_profile_and_opportunity_modes()
    {
        var knownProfile = await _client.GetFromJsonAsync<ProfileSearchResponse>("/api/Profiles/search?query=Thabo&mode=profile");
        var opportunity = await _client.GetFromJsonAsync<ProfileSearchResponse>("/api/Profiles/search?query=Fleet&mode=opportunity&intent=driver&relationshipType=Fleet%20contract");

        Assert.NotNull(knownProfile);
        Assert.NotNull(opportunity);
        Assert.True(knownProfile.ResultCount > 0);
        Assert.True(opportunity.ResultCount > 0);
        Assert.All(opportunity.Results, result => Assert.NotEmpty(result.RankingSignals));
    }

    [Fact]
    public async Task Verification_case_lifecycle_and_moderation_queue_are_available()
    {
        var created = await _client.PostAsJsonAsync("/api/VerificationCases", new CreateVerificationCaseRequest(
            "Relationship",
            "Fleet contract",
            10,
            "Fleet owner",
            [new DocumentEvidenceRequest("licence", "licence.pdf", "application/pdf", 1200)],
            [new CounterpartyConfirmationRequest("Fleet owner", "Confirmed relationship", "Requested")]));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var verificationCase = await created.Content.ReadFromJsonAsync<VerificationCaseDto>();
        Assert.NotNull(verificationCase);

        var status = await _client.PatchAsJsonAsync($"/api/VerificationCases/{verificationCase.CaseId}/status", new UpdateVerificationCaseStatusRequest("Submitted"));
        var dispute = await _client.PostAsJsonAsync($"/api/VerificationCases/{verificationCase.CaseId}/dispute", new DisputeRequest(10, "Driver right-of-reply requested.", "driver"));
        var moderation = await _client.GetAsync("/api/Moderation/queue");

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, dispute.StatusCode);
        Assert.Equal(HttpStatusCode.OK, moderation.StatusCode);
    }
}
