namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class GoalEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _userA = null!;
    private HttpClient _userB = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-goals-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options =>
                        options.UseSqlite($"Data Source={_dbPath}"));
                });
            });

        _userA = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _userB = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _userA.Dispose();
        _userB.Dispose();
        await _factory.DisposeAsync();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task GoalDefinitions_RequireAuthentication_AndExposeStableObservationalTaxonomy()
    {
        using var anonymous = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/goals")).StatusCode);

        await SignInAndAcceptConsentAsync(_userA, "goal-catalog@example.com");
        var definitions = await _userA.GetFromJsonAsync<GoalDefinitionResponse[]>("/api/v1/goals", JsonOptions);

        Assert.NotNull(definitions);
        Assert.Equal(24, definitions.Length);
        Assert.Equal(24, definitions.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            new[] { "recovery", "energy", "cognitive", "longevity", "performance", "skin", "organ" },
            definitions.Select(definition => definition.Category).Distinct(StringComparer.Ordinal));
        Assert.All(definitions, definition => Assert.True(definition.IsActive));
        Assert.Contains(definitions, definition =>
            definition.Id == "recovery-injury" &&
            definition.Name == "Injury recovery" &&
            definition.Description.StartsWith("Observe", StringComparison.Ordinal));
    }

    [Fact]
    public void ProfileGoalsMigration_IsDiscoverableAndCreatesTheJoinContract()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var script = db.GetService<IMigrator>().GenerateScript(
            "20260803120000_AddSpineChainCheckpoints",
            "20260828000000_AddProfileGoals");

        Assert.Contains("CREATE TABLE \"ProfileGoals\"", script, StringComparison.Ordinal);
        Assert.Contains("IX_ProfileGoals_ProfileId_GoalDefinitionId", script, StringComparison.Ordinal);
        Assert.Contains("FK_ProfileGoals_PersonProfiles_ProfileId", script, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProfileGoals_ReplaceAsASet_ValidateIds_AndPersistNestedDefinitions()
    {
        await SignInAndAcceptConsentAsync(_userA, "goal-owner@example.com");
        var profile = await CreateProfileAsync(_userA, "Goal Owner");

        var write = await _userA.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/goals",
            new SetProfileGoalsRequest(["energy-levels", "recovery-post-workout", "energy-levels"]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, write.StatusCode);

        var saved = await write.Content.ReadFromJsonAsync<ProfileGoalResponse[]>(JsonOptions);
        Assert.NotNull(saved);
        Assert.Equal(new[] { "recovery-post-workout", "energy-levels" }, saved.Select(goal => goal.GoalDefinitionId));
        Assert.All(saved, goal =>
        {
            Assert.Equal(profile.Id, goal.ProfileId);
            Assert.Equal(goal.GoalDefinitionId, goal.GoalDefinition.Id);
            Assert.NotEqual(Guid.Empty, goal.Id);
            Assert.NotEqual(default, goal.CreatedAtUtc);
        });

        var firstGoalId = saved.Single(goal => goal.GoalDefinitionId == "energy-levels").Id;
        var replace = await _userA.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/goals",
            new SetProfileGoalsRequest(["energy-levels", "cognitive-focus"]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
        var replaced = await replace.Content.ReadFromJsonAsync<ProfileGoalResponse[]>(JsonOptions);
        Assert.NotNull(replaced);
        Assert.Equal(new[] { "energy-levels", "cognitive-focus" }, replaced.Select(goal => goal.GoalDefinitionId));
        Assert.Equal(firstGoalId, replaced.Single(goal => goal.GoalDefinitionId == "energy-levels").Id);

        var invalid = await _userA.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/goals",
            new SetProfileGoalsRequest(["not-a-goal"]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var afterInvalid = await _userA.GetFromJsonAsync<ProfileGoalResponse[]>(
            $"/api/v1/profiles/{profile.Id}/goals",
            JsonOptions);
        Assert.Equal(new[] { "energy-levels", "cognitive-focus" }, afterInvalid!.Select(goal => goal.GoalDefinitionId));

        var clear = await _userA.PostAsJsonAsync(
            $"/api/v1/profiles/{profile.Id}/goals",
            new SetProfileGoalsRequest([]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        Assert.Empty((await clear.Content.ReadFromJsonAsync<ProfileGoalResponse[]>(JsonOptions))!);
    }

    [Fact]
    public async Task ProfileGoals_EnforceOwnershipAndConsent_WithoutBlockingOwnedReads()
    {
        await SignInAndAcceptConsentAsync(_userA, "goal-owner-a@example.com");
        await SignInAndAcceptConsentAsync(_userB, "goal-owner-b@example.com");
        var profileA = await CreateProfileAsync(_userA, "Owner A");

        var initialWrite = await _userA.PostAsJsonAsync(
            $"/api/v1/profiles/{profileA.Id}/goals",
            new SetProfileGoalsRequest(["performance-strength"]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, initialWrite.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _userB.GetAsync($"/api/v1/profiles/{profileA.Id}/goals")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _userB.PostAsJsonAsync(
                $"/api/v1/profiles/{profileA.Id}/goals",
                new SetProfileGoalsRequest([]),
                JsonOptions)).StatusCode);

        var decline = await _userA.PostAsJsonAsync("/api/v1/consent/decline", new RecordConsentRequest(null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, decline.StatusCode);
        var blockedWrite = await _userA.PostAsJsonAsync(
            $"/api/v1/profiles/{profileA.Id}/goals",
            new SetProfileGoalsRequest([]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, blockedWrite.StatusCode);
        var error = await blockedWrite.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal("consent_required", error.GetProperty("code").GetString());

        var readable = await _userA.GetFromJsonAsync<ProfileGoalResponse[]>(
            $"/api/v1/profiles/{profileA.Id}/goals",
            JsonOptions);
        Assert.Single(readable!);
        Assert.Equal("performance-strength", readable![0].GoalDefinitionId);
    }

    private async Task SignInAndAcceptConsentAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement
            .EnumerateArray()
            .Single(message => message.GetProperty("contact").GetString() == email)
            .GetProperty("link")
            .GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await client.PostAsJsonAsync("/api/v1/consent", new RecordConsentRequest(null), JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
    }

    private static async Task<ProfileResponse> CreateProfileAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "Observational context", "notes"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }
}
