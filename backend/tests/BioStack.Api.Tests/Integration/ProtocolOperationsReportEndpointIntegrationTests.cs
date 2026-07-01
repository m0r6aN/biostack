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
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class ProtocolOperationsReportEndpointIntegrationTests : IAsyncLifetime
{
    private static readonly string[] ForbiddenTerms =
    {
        "recommend",
        "diagnos",
        "dosing instruction",
        "treatment",
        "prescri",
        "protocol intelligence",
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private Guid _userId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-ops-report-{Guid.NewGuid():N}.db");
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

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _userId = await SignInAsync("ops-report-user@example.com");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
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
    public async Task GetOperationsReport_ReturnsOk_WithReportShape_ForSeededProfile()
    {
        var profile = await CreateProfileAsync("Ops Report Seeded");
        await CreateActiveCompoundAsync(profile.Id, "Test Compound");
        await SaveProtocolAsync(profile.Id, "Ops Report Protocol");

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/operations-report");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<ProtocolOperationsReport>(JsonOptions);
        Assert.NotNull(report);
        Assert.Equal(profile.Id, report!.ProfileId);
        Assert.Equal(1, report.Summary.ActiveCompoundsCount);
        Assert.NotNull(report.RecentEvents);
        Assert.NotNull(report.EvidenceReferences);
        Assert.NotNull(report.Warnings);
    }

    [Fact]
    public async Task GetOperationsReport_ReturnsHonestEmptyState_ForProfileWithNoActivity()
    {
        var profile = await CreateProfileAsync("Ops Report Empty State");

        var report = await _client.GetFromJsonAsync<ProtocolOperationsReport>(
            $"/api/v1/profiles/{profile.Id}/protocol/operations-report", JsonOptions);

        Assert.NotNull(report);
        Assert.Null(report!.ProtocolId);
        Assert.Equal(0, report.Summary.ActiveCompoundsCount);
        Assert.Equal(0, report.Summary.LoggedDosesCount);
        Assert.Equal(0, report.Summary.CheckInCount);
        Assert.Equal(0, report.Summary.MonitoringEntryCount);
        Assert.Equal(0, report.Summary.EvidenceReferenceCount);
        Assert.Empty(report.RecentEvents);
        Assert.Empty(report.EvidenceReferences);
        Assert.NotEmpty(report.Warnings);
    }

    [Fact]
    public async Task GetOperationsReport_Response_ContainsNoForbiddenLanguage()
    {
        var profile = await CreateProfileAsync("Ops Report Language Check");
        await CreateActiveCompoundAsync(profile.Id, "Language Check Compound");
        await SaveProtocolAsync(profile.Id, "Language Check Protocol");

        var raw = await _client.GetStringAsync($"/api/v1/profiles/{profile.Id}/protocol/operations-report");
        var lowered = raw.ToLowerInvariant();

        foreach (var term in ForbiddenTerms)
        {
            Assert.DoesNotContain(term, lowered, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task GetOperationsReport_RouteAndName_DoNotContainProtocolIntelligenceLanguage()
    {
        const string route = "/api/v1/profiles/{profileId}/protocol/operations-report";
        const string endpointName = "GetProtocolOperationsReport";

        Assert.DoesNotContain("intelligence", route, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("intelligence", endpointName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetOperationsReport_EnforcesOwnership_AcrossUsers()
    {
        var profile = await CreateProfileAsync("Ops Report Owner");

        await SignInAsync("ops-report-intruder@example.com");
        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/operations-report");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest(email, "email", "/profiles"), JsonOptions);
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles", new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private async Task CreateActiveCompoundAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            new CreateCompoundRequest(name, CompoundCategory.Peptide, DateTime.UtcNow.Date.AddDays(-7), null, CompoundStatus.Active, "notes", SourceType.Manual, "goal", "manual", 10m),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task SaveProtocolAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/protocols", new SaveProtocolRequest(name), JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
