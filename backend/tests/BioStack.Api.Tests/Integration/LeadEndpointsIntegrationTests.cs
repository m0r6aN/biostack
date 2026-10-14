namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BioStack.Api;
using BioStack.Infrastructure.Persistence;

[Trait("Category", "Integration")]
public class LeadEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-leads-{Guid.NewGuid():N}.db");
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
        _client = _factory.CreateClient();
        return Task.CompletedTask;
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
    public async Task CaptureLead_WithValidPayload_ReturnsNoContentAndPersistsNormalizedLead()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/leads/capture", new
        {
            Email = "  User@Example.com  ",
            Source = "reconstitution-calculator"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var stored = dbContext.LeadCaptures.Single();

        Assert.Equal("user@example.com", stored.Email);
        Assert.Equal("reconstitution-calculator", stored.Source);
    }

    [Fact]
    public async Task CaptureLead_WithDuplicatePayload_IsIdempotent()
    {
        var payload = new
        {
            Email = "user@example.com",
            Source = "clinics-coaches-pricing"
        };

        await _client.PostAsJsonAsync("/api/v1/leads/capture", payload);
        var second = await _client.PostAsJsonAsync("/api/v1/leads/capture", payload);

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

        Assert.Equal(1, dbContext.LeadCaptures.Count(lead => lead.Source == "clinics-coaches-pricing"));
    }

    [Fact]
    public async Task CaptureLead_WithBlankFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/leads/capture", new
        {
            Email = "   ",
            Source = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureLead_WithUnknownSourceOrHoneypot_DoesNotPersist()
    {
        var unknown = await _client.PostAsJsonAsync("/api/v1/leads/capture", new
        {
            Email = "user@example.com",
            Source = "not-a-product-source"
        });
        var honeypot = await _client.PostAsJsonAsync("/api/v1/leads/capture", new
        {
            Email = "bot@example.com",
            Source = "clinics-coaches-pricing",
            Website = "https://spam.example"
        });

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, honeypot.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Empty(dbContext.LeadCaptures);
    }
}
