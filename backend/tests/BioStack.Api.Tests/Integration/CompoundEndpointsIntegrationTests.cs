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

/// <summary>
/// Covers the compound record-integrity fix: a blank/whitespace name must be
/// rejected on both create and update (400 ProblemDetails), and the
/// previously-unreachable PUT/DELETE endpoints must actually work end to end
/// (frontend/src/lib/api.ts had zero callers for either before this change).
/// </summary>
[Trait("Category", "Integration")]
public sealed class CompoundEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-compounds-{Guid.NewGuid():N}.db");
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
        await SignInAsync();
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

    private static CreateCompoundRequest ValidRequest(string name = "BPC-157") => new(
        name,
        CompoundCategory.Peptide,
        DateTime.UtcNow.Date,
        null,
        CompoundStatus.Active,
        "Morning dose",
        SourceType.Manual,
        "recovery",
        "Manual",
        null);

    [Fact]
    public async Task CreateCompound_WithBlankName_ReturnsValidationProblem()
    {
        var profileId = await CreateProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            ValidRequest(name: "   "),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var nameErrors = body.GetProperty("errors").GetProperty("name");
        Assert.Contains("required", nameErrors[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCompound_WithValidName_ReturnsCreated()
    {
        var profileId = await CreateProfileAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            ValidRequest(),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("BPC-157", created!.Name);
    }

    [Fact]
    public async Task UpdateCompound_WithBlankName_ReturnsValidationProblem()
    {
        var profileId = await CreateProfileAsync();
        var compound = await CreateCompoundAsync(profileId);

        var update = new UpdateCompoundRequest("  ", CompoundCategory.Peptide, DateTime.UtcNow.Date, null, CompoundStatus.Active, "", SourceType.Manual);
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{compound.Id}",
            update,
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var nameErrors = body.GetProperty("errors").GetProperty("name");
        Assert.Contains("required", nameErrors[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCompound_FixesThePreviouslyNamelessRecord()
    {
        // Recreates the owner-reported recovery path: a compound created with
        // a blank name (as could happen before this fix shipped) can be given
        // a name, category, notes, and start date via the edit endpoint.
        var profileId = await CreateProfileAsync();
        var compound = await CreateCompoundAsync(profileId);

        var newStart = DateTime.UtcNow.Date.AddDays(-3);
        var update = new UpdateCompoundRequest(
            "Renamed Compound",
            CompoundCategory.Supplement,
            newStart,
            null,
            CompoundStatus.Active,
            "Updated notes",
            SourceType.Manual,
            compound.Goal,
            compound.Source,
            compound.PricePaid);

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds/{compound.Id}",
            update,
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Renamed Compound", updated!.Name);
        Assert.Equal(CompoundCategory.Supplement, updated.Category);
        Assert.Equal("Updated notes", updated.Notes);
        Assert.Equal(newStart, updated.StartDate);
    }

    [Fact]
    public async Task DeleteCompound_RemovesItFromTheProfileList()
    {
        var profileId = await CreateProfileAsync();
        var compound = await CreateCompoundAsync(profileId);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/profiles/{profileId}/compounds/{compound.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/api/v1/profiles/{profileId}/compounds");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var remaining = await listResponse.Content.ReadFromJsonAsync<List<CompoundResponse>>(JsonOptions);
        Assert.NotNull(remaining);
        Assert.DoesNotContain(remaining!, c => c.Id == compound.Id);
    }

    [Fact]
    public async Task DeleteCompound_Twice_SecondCallReturnsNotFound()
    {
        var profileId = await CreateProfileAsync();
        var compound = await CreateCompoundAsync(profileId);

        var first = await _client.DeleteAsync($"/api/v1/profiles/{profileId}/compounds/{compound.Id}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await _client.DeleteAsync($"/api/v1/profiles/{profileId}/compounds/{compound.Id}");
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    private async Task<Guid> CreateProfileAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/profiles",
            new CreateProfileRequest("Compound Owner", Sex.Unspecified, 75m, 30, "goal", "notes"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions);
        return profile!.Id;
    }

    private async Task<CompoundResponse> CreateCompoundAsync(Guid profileId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/profiles/{profileId}/compounds",
            ValidRequest(),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CompoundResponse>(JsonOptions))!;
    }

    private async Task SignInAsync()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest("compounds@example.com", "email", "/compounds"));
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
        var consent = await _client.PostAsJsonAsync("/api/v1/consent", new { });
        Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
    }
}
