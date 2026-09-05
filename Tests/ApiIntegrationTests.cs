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
        var verification = await _client.GetFromJsonAsync<ProfileSearchResponse>("/api/Profiles/search?query=Thabo&mode=verification");
        var knownProfile = await _client.GetFromJsonAsync<ProfileSearchResponse>("/api/Profiles/search?query=Nomsa&mode=profile");
        var opportunity = await _client.GetFromJsonAsync<ProfileSearchResponse>("/api/Profiles/search?query=Aisha&mode=opportunity&intent=driver-looking-for-owner&relationshipType=Fleet%20contract");

        Assert.NotNull(verification);
        Assert.NotNull(knownProfile);
        Assert.NotNull(opportunity);
        Assert.Contains(verification.Results, result => result.UserId == 10);
        Assert.True(knownProfile.ResultCount > 0);
        Assert.True(opportunity.ResultCount > 0);
        Assert.DoesNotContain(knownProfile.Results, result => result.UserId == 10);
        Assert.All(opportunity.Results, result => Assert.NotEmpty(result.RankingSignals));
    }

    [Fact]
    public async Task Verification_case_lifecycle_and_moderation_queue_are_available()
    {
        var created = await _client.PostAsJsonAsync("/api/VerificationCases", new CreateVerificationCaseRequest(
            "Driver identity",
            "Fleet contract",
            10,
            "Fleet owner",
            [new DocumentEvidenceRequest("Driver licence", "licence.pdf", "application/pdf", 1200)],
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

    [Fact]
    public async Task Current_user_workspace_vehicle_relationship_and_admin_contracts_are_available()
    {
        var me = await _client.GetFromJsonAsync<MeWorkspaceDto>("/api/Me");
        var vehicles = await _client.GetFromJsonAsync<IReadOnlyList<VehicleWorkspaceDto>>("/api/Vehicles/mine");
        var rules = await _client.GetFromJsonAsync<VerificationRulesResponse>("/api/VerificationCases/rules?profileType=Owner");
        var relationships = await _client.GetFromJsonAsync<IReadOnlyList<MarketplaceRelationshipDto>>("/api/Relationships/mine");
        var admin = await _client.GetFromJsonAsync<AdminDashboardDto>("/api/Moderation/dashboard?market=All%20drivers");

        Assert.NotNull(me);
        Assert.NotNull(vehicles);
        Assert.NotNull(rules);
        Assert.NotNull(relationships);
        Assert.NotNull(admin);
        Assert.NotEmpty(vehicles);
        Assert.DoesNotContain("Driver identity", rules.AllowedCaseTypes);
        Assert.NotNull(me.ProfileEditor);
        Assert.True(admin.TrustSignals.ProfilesMonitored > 0);

        var relationship = Assert.Single(relationships.Where(item => item.RelationshipId == "rel-rideshare-10"));
        using var update = await _client.PatchAsJsonAsync(
            $"/api/Relationships/{relationship.RelationshipId}",
            new RelationshipUpdateRequest(relationship.VerificationStatus, "Active"));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }
}
