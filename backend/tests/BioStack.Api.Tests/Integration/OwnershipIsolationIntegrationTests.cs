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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

public sealed class OwnershipIsolationIntegrationTests : IAsyncLifetime
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
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-ownership-{Guid.NewGuid():N}.db");
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
                    services.RemoveAll<DbContextOptions<BioStackDbContext>>();
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
    public async Task ProfileScopedEndpoints_BlockCrossUserAccess()
    {
        var userAId = await SignInAsync(_userA, "owner-a@example.com");
        var userBId = await SignInAsync(_userB, "owner-b@example.com");
        var profileA = await CreateProfileAsync(_userA, "Owner A");
        var profileB = await CreateProfileAsync(_userB, "Owner B");

        await AssertProfileOwnerAsync(profileA.Id, userAId);
        await AssertProfileOwnerAsync(profileB.Id, userBId);

        var compoundA = await CreateCompoundAsync(_userA, profileA.Id, "A compound");
        var compoundB = await CreateCompoundAsync(_userB, profileB.Id, "B compound");
        var checkInA = await CreateCheckInAsync(_userA, profileA.Id);
        await CreateCheckInAsync(_userB, profileB.Id);
        var phaseA = await CreatePhaseAsync(_userA, profileA.Id);
        await CreatePhaseAsync(_userB, profileB.Id);
        var protocolA = await SaveProtocolAsync(_userA, profileA.Id, "A protocol");
        var protocolB = await SaveProtocolAsync(_userB, profileB.Id, "B protocol");
        var runB = await StartRunAsync(_userB, protocolB.Id);

        await AssertOwnAccessStillWorks(profileA, compoundA, checkInA, phaseA, protocolA);
        await AssertProfileIsolation(profileA.Id, profileB.Id);
        await AssertCompoundIsolation(profileA.Id, compoundA.Id, profileB.Id, compoundB.Id);
        await AssertCheckInIsolation(profileA.Id, profileB.Id);
        await AssertProtocolPhaseIsolation(profileA.Id, profileB.Id);
        await AssertTimelineIsolation(profileA.Id, profileB.Id);
        await AssertProtocolIsolation(profileB.Id, protocolB.Id, runB.Id);
        await AssertChildCreateRequiresOwnedParent(profileB.Id);
    }

    private async Task AssertOwnAccessStillWorks(
        ProfileResponse profile,
        CompoundResponse compound,
        CheckInResponse checkIn,
        ProtocolPhaseResponse phase,
        ProtocolResponse protocol)
    {
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/profiles/{profile.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/profiles/{profile.Id}/compounds")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/profiles/{profile.Id}/checkins")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/profiles/{profile.Id}/phases")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/profiles/{profile.Id}/timeline")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/profiles/{profile.Id}/protocols")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userA.GetAsync($"/api/v1/protocols/{protocol.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _userA.PostAsync($"/api/v1/protocols/{protocol.Id}/runs", null)).StatusCode);
        Assert.NotEqual(Guid.Empty, compound.Id);
        Assert.NotEqual(Guid.Empty, checkIn.Id);
        Assert.NotEqual(Guid.Empty, phase.Id);
    }

    private async Task AssertProfileIsolation(Guid profileAId, Guid profileBId)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}")).StatusCode);

        var update = new UpdateProfileRequest("Blocked", Sex.Female, 70m, 30, "blocked", "blocked");
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PutAsJsonAsync($"/api/v1/profiles/{profileBId}", update, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.DeleteAsync($"/api/v1/profiles/{profileBId}")).StatusCode);

        var profiles = await _userA.GetFromJsonAsync<ProfileResponse[]>("/api/v1/profiles", JsonOptions);
        Assert.NotNull(profiles);
        Assert.Contains(profiles, profile => profile.Id == profileAId);
        Assert.DoesNotContain(profiles, profile => profile.Id == profileBId);
        Assert.Equal(HttpStatusCode.OK, (await _userB.GetAsync($"/api/v1/profiles/{profileBId}")).StatusCode);
    }

    private async Task AssertCompoundIsolation(Guid profileAId, Guid compoundAId, Guid profileBId, Guid compoundBId)
    {
        var compoundsA = await _userA.GetFromJsonAsync<CompoundResponse[]>($"/api/v1/profiles/{profileAId}/compounds", JsonOptions);
        Assert.NotNull(compoundsA);
        Assert.Contains(compoundsA, compound => compound.Id == compoundAId);
        Assert.DoesNotContain(compoundsA, compound => compound.Id == compoundBId);

        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/compounds")).StatusCode);

        var update = new UpdateCompoundRequest("Blocked", CompoundCategory.Peptide, DateTime.UtcNow.Date, null, CompoundStatus.Active, "blocked", SourceType.Manual);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PutAsJsonAsync($"/api/v1/profiles/{profileBId}/compounds/{compoundBId}", update, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PutAsJsonAsync($"/api/v1/profiles/{profileBId}/compounds/{compoundAId}", update, JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.DeleteAsync($"/api/v1/profiles/{profileBId}/compounds/{compoundBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _userB.GetAsync($"/api/v1/profiles/{profileBId}/compounds")).StatusCode);
    }

    private async Task AssertCheckInIsolation(Guid profileAId, Guid profileBId)
    {
        var checkInsA = await _userA.GetFromJsonAsync<CheckInResponse[]>($"/api/v1/profiles/{profileAId}/checkins", JsonOptions);
        Assert.NotNull(checkInsA);
        Assert.All(checkInsA, checkIn => Assert.Equal(profileAId, checkIn.PersonId));
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/checkins")).StatusCode);
    }

    private async Task AssertProtocolPhaseIsolation(Guid profileAId, Guid profileBId)
    {
        var phasesA = await _userA.GetFromJsonAsync<ProtocolPhaseResponse[]>($"/api/v1/profiles/{profileAId}/phases", JsonOptions);
        Assert.NotNull(phasesA);
        Assert.All(phasesA, phase => Assert.Equal(profileAId, phase.PersonId));
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/phases")).StatusCode);
    }

    private async Task AssertTimelineIsolation(Guid profileAId, Guid profileBId)
    {
        var timelineA = await _userA.GetFromJsonAsync<TimelineEventResponse[]>($"/api/v1/profiles/{profileAId}/timeline", JsonOptions);
        Assert.NotNull(timelineA);
        Assert.NotEmpty(timelineA);
        Assert.All(timelineA, @event => Assert.Equal(profileAId, @event.PersonId));
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/timeline")).StatusCode);
    }

    private async Task AssertProtocolIsolation(Guid profileBId, Guid protocolBId, Guid runBId)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/protocols")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/protocols/current-stack-intelligence")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/protocols/active-run")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/profiles/{profileBId}/protocols/mission-control")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/protocols/{protocolBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/protocols/{protocolBId}/review")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/protocols/{protocolBId}/patterns")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/protocols/{protocolBId}/drift")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.GetAsync($"/api/v1/protocols/{protocolBId}/sequence-expectation")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsync($"/api/v1/protocols/{protocolBId}/runs", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsync($"/api/v1/protocols/runs/{runBId}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsync($"/api/v1/protocols/runs/{runBId}/abandon", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/protocols/runs/{runBId}/evolve", new EvolveProtocolFromRunRequest("Blocked"), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/protocols/{protocolBId}/computations", new CreateProtocolComputationRequest(null, "blocked", "{}", "{}"), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/protocols/{protocolBId}/review/complete", new CompleteProtocolReviewRequest(runBId, "blocked"), JsonOptions)).StatusCode);
    }

    private async Task AssertChildCreateRequiresOwnedParent(Guid profileBId)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/profiles/{profileBId}/compounds", NewCompound("blocked"), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/profiles/{profileBId}/checkins", NewCheckIn(), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/profiles/{profileBId}/phases", NewPhase(), JsonOptions)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _userA.PostAsJsonAsync($"/api/v1/profiles/{profileBId}/protocols", new SaveProtocolRequest("blocked"), JsonOptions)).StatusCode);
    }

    private async Task<Guid> SignInAsync(HttpClient client, string email)
    {
        await client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
    }

    private async Task AssertProfileOwnerAsync(Guid profileId, Guid ownerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var savedOwnerId = await db.PersonProfiles.Where(profile => profile.Id == profileId).Select(profile => profile.OwnerId).SingleAsync();
        Assert.Equal(ownerId, savedOwnerId);
    }

    private static async Task<ProfileResponse> CreateProfileAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/v1/profiles", new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private static async Task<CompoundResponse> CreateCompoundAsync(HttpClient client, Guid profileId, string name)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/compounds", NewCompound(name), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
    }

    private static async Task<CheckInResponse> CreateCheckInAsync(HttpClient client, Guid profileId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/checkins", NewCheckIn(), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CheckInResponse>(JsonOptions))!;
    }

    private static async Task<ProtocolPhaseResponse> CreatePhaseAsync(HttpClient client, Guid profileId)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/phases", NewPhase(), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProtocolPhaseResponse>(JsonOptions))!;
    }

    private static async Task<ProtocolResponse> SaveProtocolAsync(HttpClient client, Guid profileId, string name)
    {
        var response = await client.PostAsJsonAsync($"/api/v1/profiles/{profileId}/protocols", new SaveProtocolRequest(name), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProtocolResponse>(JsonOptions))!;
    }

    private static async Task<ProtocolRunResponse> StartRunAsync(HttpClient client, Guid protocolId)
    {
        var response = await client.PostAsync($"/api/v1/protocols/{protocolId}/runs", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProtocolRunResponse>(JsonOptions))!;
    }

    private static CreateCompoundRequest NewCompound(string name) => new(
        name,
        CompoundCategory.Peptide,
        DateTime.UtcNow.Date,
        null,
        CompoundStatus.Active,
        "notes",
        SourceType.Manual,
        "goal",
        "manual",
        10m);

    private static CreateCheckInRequest NewCheckIn() => new(
        DateTime.UtcNow.Date,
        80m,
        8,
        7,
        6,
        7,
        Notes: "daily check-in");

    private static CreateProtocolPhaseRequest NewPhase() => new(
        "Phase",
        DateTime.UtcNow.Date,
        null,
        "phase notes");
}
