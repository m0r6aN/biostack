namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Application.Services;
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

[Trait("Category", "Integration")]
public sealed class ProtocolOperationsReportEndpointIntegrationTests : IAsyncLifetime
{
    private static readonly string[] ForbiddenTerms =
    [
        "recommend",
        "diagnos",
        "dosing instruction",
        "treatment",
        "prescri",
        "protocol intelligence"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-ops-report-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                            ["FrontendUrl"] = "http://localhost:3043",
                            ["PublicApiUrl"] = "http://localhost:5000",
                            ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac"
                        }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await SignInAsync("ops-report-user@example.com");
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
    public async Task GetOperationsReportExport_ReturnsOk_WithExportShape_ForSeededProfile()
    {
        var profile = await CreateProfileAsync("Ops Report Export Seeded");
        await CreateActiveCompoundAsync(profile.Id, "BPC-157");
        await SaveProtocolAsync(profile.Id, "Operations export protocol");

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/operations-report/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var export = await response.Content.ReadFromJsonAsync<ProtocolOperationsReportExport>(JsonOptions);

        Assert.NotNull(export);
        Assert.Equal(profile.Id, export!.Metadata.ProfileId);
        Assert.Equal(ProtocolOperationsReportExportService.SchemaVersion, export.Metadata.SchemaVersion);
        Assert.NotEqual(default, export.Metadata.GeneratedAtUtc);
        Assert.NotNull(export.Report);
        Assert.Equal(profile.Id, export.Report.ProfileId);
        Assert.Equal(1, export.Report.Summary.ActiveCompoundsCount);
        Assert.Equal("SHA-256", export.Integrity.HashAlgorithm);
        Assert.Matches("^[0-9a-f]{64}$", export.Integrity.ContentHash);
        Assert.Contains("observational", export.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("medical", export.Disclaimer, StringComparison.OrdinalIgnoreCase);

        var raw = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        foreach (var term in ForbiddenTerms)
        {
            Assert.DoesNotContain(term, raw, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task GetOperationsReportExport_IsDeterministic_ForRepeatedCalls()
    {
        await using var deterministicFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IProtocolOperationsReportService>();
                services.AddSingleton<IProtocolOperationsReportService>(new DeterministicProtocolOperationsReportService());
            });
        });

        using var client = deterministicFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        const string email = "ops-report-deterministic@example.com";
        var profileId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        await SignInAsync(client, deterministicFactory, email);

        var endpoint = $"/api/v1/profiles/{profileId}/protocol/operations-report/export";
        var export1 = await client.GetFromJsonAsync<ProtocolOperationsReportExport>(endpoint, JsonOptions);
        var export2 = await client.GetFromJsonAsync<ProtocolOperationsReportExport>(endpoint, JsonOptions);

        Assert.NotNull(export1);
        Assert.NotNull(export2);
        Assert.Equal(export1!.Integrity.ContentHash, export2!.Integrity.ContentHash);
        Assert.Matches("^[0-9a-f]{64}$", export1.Integrity.ContentHash);
        Assert.Matches("^[0-9a-f]{64}$", export2.Integrity.ContentHash);
    }

    [Fact]
    public void GetOperationsReportExport_RouteAndName_DoNotContainProtocolIntelligenceLanguage()
    {
        const string route = "/api/v1/profiles/{profileId}/protocol/operations-report/export";
        const string endpointName = "GetProtocolOperationsReportExport";

        Assert.DoesNotContain("intelligence", route, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("intelligence", endpointName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetOperationsReportExport_EnforcesOwnership_AcrossUsers()
    {
        var profile = await CreateProfileAsync("Ops Report Owner");

        await SignInAsync("ops-report-intruder@example.com");
        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/operations-report/export");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> SignInAsync(string email)
    {
        return await SignInAsync(_client, _factory, email);
    }

    private static async Task<Guid> SignInAsync(
        HttpClient client,
        WebApplicationFactory<Program> factory,
        string email)
    {
        await client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/profiles"),
            JsonOptions);

        using var doc = await JsonDocument.ParseAsync(await client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await client.GetAsync($"{uri.AbsolutePath}{uri.Query}");

        var consent = await client.PostAsJsonAsync("/api/v1/consent", new { }, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private async Task<ProfileResponse> CreateProfileAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private async Task CreateActiveCompoundAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            new CreateCompoundRequest(
                name,
                CompoundCategory.Peptide,
                DateTime.UtcNow.Date.AddDays(-7),
                null,
                CompoundStatus.Active,
                "notes",
                SourceType.Manual),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task SaveProtocolAsync(Guid profileId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/protocols",
            new SaveProtocolRequest(name),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private sealed class DeterministicProtocolOperationsReportService : IProtocolOperationsReportService
    {
        public Task<ProtocolOperationsReport> GetReportAsync(Guid profileId, CancellationToken ct = default)
        {
            var report = new ProtocolOperationsReport(
                profileId,
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc),
                new ProtocolOperationsSummary(
                    ActiveCompoundsCount: 1,
                    LoggedDosesCount: 1,
                    CheckInCount: 1,
                    MonitoringEntryCount: 3,
                    MilestoneCount: 1,
                    EvidenceReferenceCount: 0,
                    LatestActivityUtc: new DateTime(2026, 1, 7, 8, 0, 0, DateTimeKind.Utc)),
                new List<ProtocolOperationsEvent>
                {
                    new("CompoundStarted", new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc), "Compound activity logged."),
                    new("CheckInCreated", new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc), "Check-in logged.")
                },
                Array.Empty<ProtocolOperationsEvidenceReference>(),
                ["No evidence references recorded."]);

            return Task.FromResult(report);
        }
    }
}
