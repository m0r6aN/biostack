namespace BioStack.Api.Tests.Health;

using System.Net;
using System.Text.Json;
using BioStack.Api.Health;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

/// <summary>Ledger gate G3: liveness stays dependency-free; readiness reflects the database.</summary>
[Trait("Category", "Integration")]
public sealed class HealthEndpointsTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"health-test-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
            builder.UseSetting("Database:Provider", "sqlite");
            builder.UseSetting("Jwt:Secret", "test-secret-key-at-least-32-chars-long!!");
            builder.UseSetting("Jwt:Issuer", "biostack");
            builder.UseSetting("Jwt:Audience", "biostack-ui");
        });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData(HealthEndpoints.ReadinessPath)]
    public async Task Health_Endpoints_Are_Anonymous_And_Healthy(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_Check_Is_Unhealthy_When_The_Database_Cannot_Be_Reached()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"no-such-dir-{Guid.NewGuid():N}", "x.db");
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite($"Data Source={missing};Mode=ReadOnly").Options;
        await using var db = new BioStackDbContext(options);

        var result = await new DatabaseReadinessHealthCheck(db).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("database unreachable", result.Description);
        Assert.Null(result.Exception); // no connection detail leaks into the probe response
    }

    [Fact]
    public async Task Readiness_Check_Is_Healthy_When_The_Database_Is_Reachable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ready-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite($"Data Source={path}").Options;
        await using var db = new BioStackDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var result = await new DatabaseReadinessHealthCheck(db).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void Probe_Config_Targets_The_Real_Health_Paths()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "infra", "azure", "container-app-probes.json"))) dir = dir.Parent;
        Assert.NotNull(dir);
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "infra", "azure", "container-app-probes.json")));
        var probes = doc.RootElement.GetProperty("api").GetProperty("probes").EnumerateArray()
            .ToDictionary(p => p.GetProperty("type").GetString()!, p => p.GetProperty("httpGet").GetProperty("path").GetString());
        Assert.Equal("/health", probes["Liveness"]);
        Assert.Equal("/health", probes["Startup"]);
        Assert.Equal(HealthEndpoints.ReadinessPath, probes["Readiness"]);
    }
}
