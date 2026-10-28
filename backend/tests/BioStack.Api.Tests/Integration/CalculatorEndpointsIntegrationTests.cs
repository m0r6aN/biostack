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
public class CalculatorEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-calculators-{Guid.NewGuid():N}.db");
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

    private static StringContent Json(string body) => new(body, System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task Conversion_WithoutFactor_UsesServerUnitTable()
    {
        var response = await _client.PostAsync("/api/v1/calculators/conversion", Json("{\"amount\":2,\"fromUnit\":\"mg\",\"toUnit\":\"mcg\"}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("2000", body);
    }

    [Theory]
    [InlineData("{\"amount\":2,\"fromUnit\":\"mg\",\"toUnit\":\"mcg\",\"conversionFactor\":5}")]
    [InlineData("{\"amount\":2,\"fromUnit\":\"mg\",\"toUnit\":\"mcg\",\"ConversionFactor\":1000}")]
    [InlineData("{\"amount\":2,\"fromUnit\":\"mg\",\"toUnit\":\"mcg\",\"conversionFactor\":null}")]
    public async Task Conversion_WithClientSuppliedFactor_IsRejectedWith400(string body)
    {
        var response = await _client.PostAsync("/api/v1/calculators/conversion", Json(body));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Conversion_WithUnsupportedUnit_Returns400()
    {
        var response = await _client.PostAsync("/api/v1/calculators/conversion", Json("{\"amount\":2,\"fromUnit\":\"iu\",\"toUnit\":\"mg\"}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Conversion_WithOverflowingAmount_Returns400Not500()
    {
        var response = await _client.PostAsync("/api/v1/calculators/conversion", Json("{\"amount\":79228162514264337593543950335,\"fromUnit\":\"g\",\"toUnit\":\"mcg\"}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ProtocolComputationWriteRoute_IsNotRegistered()
    {
        // The unused POST /api/v1/protocols/{id}/computations endpoint was removed. Existing computation rows are
        // still read by the timeline/drift analytics, but nothing can create new ones through the API.
        var routes = _factory.Services
            .GetServices<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .ToList();

        Assert.NotEmpty(routes);
        Assert.DoesNotContain(routes, route => route.Contains("computations", StringComparison.OrdinalIgnoreCase));
    }
}
