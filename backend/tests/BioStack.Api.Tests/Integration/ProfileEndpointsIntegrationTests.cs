namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using BioStack.Api;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Persistence;

[Trait("Category", "Integration")]
public class ProfileEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-profiles-{Guid.NewGuid():N}.db");
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
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
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

    [Fact]
    public async Task CreateProfile_WithValidRequest_ReturnsCreatedStatusAndProfile()
    {
        var request = new CreateProfileRequest(
            "John Doe",
            Sex.Male,
            75.5m,
            30,
            "Optimize biometric health",
            "Test profile"
        );

        var json = JsonSerializer.Serialize(request, _options);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/profiles", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<ProfileResponse>(responseBody, _options);


        Assert.NotNull(profile);
        Assert.Equal("John Doe", profile.DisplayName);
        Assert.Equal(Sex.Male, profile.Sex);
        Assert.Equal(75.5m, profile.Weight);
    }

    [Fact]
    public async Task GetAllProfiles_ReturnsOkWithProfilesList()
    {
        var createRequest = new CreateProfileRequest(
            "Jane Smith",
            Sex.Female,
            65m,
            28,
            "Health optimization",
            "Test"
        );

        var json = JsonSerializer.Serialize(createRequest, _options);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        await _client.PostAsync("/api/v1/profiles", content);

        var response = await _client.GetAsync("/api/v1/profiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        Assert.True(root.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task GetProfile_WithValidId_ReturnsOkWithProfile()
    {
        var createRequest = new CreateProfileRequest(
            "Test User",
            Sex.Unspecified,
            70m,
            null,
            "Test goal",
            "Test notes"
        );

        var createJson = JsonSerializer.Serialize(createRequest, _options);

        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync("/api/v1/profiles", createContent);
        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProfile = JsonSerializer.Deserialize<ProfileResponse>(createResponseBody, _options);


        var getResponse = await _client.GetAsync($"/api/v1/profiles/{createdProfile!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getResponseBody = await getResponse.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<ProfileResponse>(getResponseBody, _options);


        Assert.NotNull(profile);
        Assert.Equal(createdProfile.Id, profile.Id);
        Assert.Equal("Test User", profile.DisplayName);
    }

    [Fact]
    public async Task UpdateProfile_WithValidRequest_ReturnsOkWithUpdatedProfile()
    {
        var createRequest = new CreateProfileRequest(
            "Original Name",
            Sex.Male,
            80m,
            25,
            "Original goal",
            "Original notes"
        );

        var createJson = JsonSerializer.Serialize(createRequest, _options);

        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync("/api/v1/profiles", createContent);
        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProfile = JsonSerializer.Deserialize<ProfileResponse>(createResponseBody, _options);


        var updateRequest = new UpdateProfileRequest(
            "Updated Name",
            Sex.Female,
            72m,
            26,
            "Updated goal",
            "Updated notes"
        );

        var updateJson = JsonSerializer.Serialize(updateRequest, _options);

        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        var updateResponse = await _client.PutAsync($"/api/v1/profiles/{createdProfile!.Id}", updateContent);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updateResponseBody = await updateResponse.Content.ReadAsStringAsync();
        var updatedProfile = JsonSerializer.Deserialize<ProfileResponse>(updateResponseBody, _options);


        Assert.NotNull(updatedProfile);
        Assert.Equal("Updated Name", updatedProfile.DisplayName);
        Assert.Equal(Sex.Female, updatedProfile.Sex);
        Assert.Equal(72m, updatedProfile.Weight);
    }

    [Fact]
    public async Task DeleteProfile_WithValidId_ReturnsNoContent()
    {
        var createRequest = new CreateProfileRequest(
            "To Be Deleted",
            Sex.Other,
            60m,
            null,
            "Target for deletion",
            "Notes"
        );

        var createJson = JsonSerializer.Serialize(createRequest, _options);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync("/api/v1/profiles", createContent);
        var createResponseBody = await createResponse.Content.ReadAsStringAsync();
        var createdProfile = JsonSerializer.Deserialize<ProfileResponse>(createResponseBody, _options);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/profiles/{createdProfile!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/profiles/{createdProfile.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task SignInAsync()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/start", new StartAuthRequest("profiles@example.com", "email", "/profiles"));
        using var doc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = doc.RootElement.EnumerateArray().First().GetProperty("link").GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
    }
}
