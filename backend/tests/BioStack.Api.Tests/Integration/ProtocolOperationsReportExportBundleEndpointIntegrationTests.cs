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
public sealed class ProtocolOperationsReportExportBundleEndpointIntegrationTests : IAsyncLifetime
{
    private static readonly string[] ForbiddenTerms =
    {
        "recommend",
        "diagnos",
        "dosing instruction",
        "treatment",
        "prescri",
        "medical advice",
        "medical-advice",
        "protocol intelligence",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-ops-report-bundle-{Guid.NewGuid():N}.db");

        _factory = CreateFactory(_dbPath);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        await SignInAsync(_client, _factory, "ops-report-bundle-user@example.com");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();

        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task GetOperationsReportExportBundle_ReturnsOk_WithBundleShape_ForSeededProfile()
    {
        var profile = await CreateProfileAsync(_client, "Ops Report Bundle Owner");

        var response = await _client.GetAsync(
            $"/api/v1/profiles/{profile.Id}/protocol/operations-report/export/bundle");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bundle = await response.Content.ReadFromJsonAsync<ProtocolOperationsExportBundle>(JsonOptions);
        Assert.NotNull(bundle);
        Assert.Equal("1.0.0", bundle!.Metadata.SchemaVersion);
        Assert.Equal(profile.Id, bundle.Metadata.ProfileId);
        Assert.NotEqual(default, bundle.Metadata.GeneratedAtUtc);

        Assert.NotNull(bundle.ReportExport);
        Assert.Equal(profile.Id, bundle.ReportExport.Metadata.ProfileId);
        Assert.Equal(profile.Id, bundle.ReportExport.Report.ProfileId);

        var artifact = Assert.Single(bundle.Artifacts);
        Assert.Equal("protocol-operations-report-export-json", artifact.ArtifactId);
        Assert.Equal("application/json", artifact.MediaType);
        Assert.Equal("report-export", artifact.Role);
        Assert.Equal(bundle.ReportExport.Metadata.SchemaVersion, artifact.SchemaVersion);
        Assert.Equal(bundle.ReportExport.Integrity.ContentHash, artifact.ContentHash);

        Assert.Equal("SHA-256", bundle.Integrity.HashAlgorithm);
        Assert.Equal(bundle.ReportExport.Integrity.ContentHash, bundle.Integrity.ReportExportContentHash);
        Assert.Matches("^[0-9a-f]{64}$", bundle.Integrity.ReportExportContentHash);
        Assert.Matches("^[0-9a-f]{64}$", bundle.Integrity.BundleContentHash);
        Assert.Equal(
            ProtocolOperationsExportBundleService.ComputeBundleContentHash(
                bundle.Metadata,
                bundle.ReportExport,
                bundle.Artifacts,
                bundle.Disclaimer),
            bundle.Integrity.BundleContentHash);

        var raw = (await response.Content.ReadAsStringAsync()).ToLowerInvariant();
        foreach (var term in ForbiddenTerms)
        {
            Assert.DoesNotContain(term, raw, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task GetOperationsReportExportBundle_IsDeterministic_ForRepeatedCalls_WithDeterministicServiceOverride()
    {
        var deterministicFactory = CreateFactory(
            Path.Combine(Path.GetTempPath(), $"biostack-ops-report-bundle-deterministic-{Guid.NewGuid():N}.db"),
            services =>
            {
                services.RemoveAll<IProtocolOperationsReportExportService>();
                services.AddSingleton<IProtocolOperationsReportExportService>(
                    new DeterministicProtocolOperationsReportExportService());

                services.RemoveAll<IProtocolOperationsExportBundleService>();
                services.AddScoped<IProtocolOperationsExportBundleService>(sp =>
                    new ProtocolOperationsExportBundleService(
                        sp.GetRequiredService<IProtocolOperationsReportExportService>(),
                        () => new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc)));
            });

        await using var _ = deterministicFactory;
        using var client = deterministicFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SignInAsync(client, deterministicFactory, "ops-report-bundle-deterministic@example.com");

        var profileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var endpoint = $"/api/v1/profiles/{profileId}/protocol/operations-report/export/bundle";

        var bundle1 = await client.GetFromJsonAsync<ProtocolOperationsExportBundle>(endpoint, JsonOptions);
        var bundle2 = await client.GetFromJsonAsync<ProtocolOperationsExportBundle>(endpoint, JsonOptions);

        Assert.NotNull(bundle1);
        Assert.NotNull(bundle2);
        Assert.Equal(bundle1!.Integrity.BundleContentHash, bundle2!.Integrity.BundleContentHash);
        Assert.Equal(bundle1.Integrity.ReportExportContentHash, bundle2.Integrity.ReportExportContentHash);
        Assert.Matches("^[0-9a-f]{64}$", bundle1.Integrity.BundleContentHash);
    }

    [Fact]
    public async Task GetOperationsReportExportBundle_EnforcesOwnership_AcrossUsers()
    {
        var profile = await CreateProfileAsync(_client, "Ops Report Bundle Owner");
        await SignInAsync(_client, _factory, "ops-report-bundle-intruder@example.com");

        var response = await _client.GetAsync($"/api/v1/profiles/{profile.Id}/protocol/operations-report/export/bundle");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void GetOperationsReportExportBundle_RouteAndName_DoNotContainProtocolIntelligenceLanguage()
    {
        const string route = "/api/v1/profiles/{profileId}/protocol/operations-report/export/bundle";
        const string endpointName = "GetProtocolOperationsReportExportBundle";

        Assert.DoesNotContain("intelligence", route, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("intelligence", endpointName, StringComparison.OrdinalIgnoreCase);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string dbPath,
        Action<IServiceCollection>? configureServices = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}",
                        ["FrontendUrl"] = "http://localhost:3043",
                        ["PublicApiUrl"] = "http://localhost:5000",
                        ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                    }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveBioStackDbContext();
                    services.AddDbContext<BioStackDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
                    configureServices?.Invoke(services);
                });
            });
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
        return await db.AppUsers.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
    }

    private static async Task<ProfileResponse> CreateProfileAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest(displayName, Sex.Unspecified, 80m, 35, "goal", "notes"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions))!;
    }

    private sealed class DeterministicProtocolOperationsReportExportService : IProtocolOperationsReportExportService
    {
        public Task<ProtocolOperationsReportExport> GetExportAsync(Guid profileId, CancellationToken ct = default)
        {
            var report = new ProtocolOperationsReport(
                profileId,
                null,
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
                    new("CheckInCreated", new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc), "Check-in logged."),
                },
                Array.Empty<ProtocolOperationsEvidenceReference>(),
                new List<string> { "No evidence references recorded." });

            var export = new ProtocolOperationsReportExport(
                new ProtocolOperationsReportExportMetadata(
                    ProtocolOperationsReportExportService.SchemaVersion,
                    new DateTime(2026, 1, 8, 1, 0, 0, DateTimeKind.Utc),
                    report.ProfileId,
                    report.ProtocolId),
                report,
                new ProtocolOperationsReportExportIntegrity(
                    ProtocolOperationsReportExportService.HashAlgorithmName,
                    ProtocolOperationsReportExportService.ComputeContentHash(report)),
                ProtocolOperationsReportExportService.Disclaimer);

            return Task.FromResult(export);
        }
    }
}
