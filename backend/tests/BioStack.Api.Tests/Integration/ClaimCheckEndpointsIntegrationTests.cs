namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Api;
using BioStack.Api.Endpoints;
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
public sealed class ClaimCheckEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _userA = null!;
    private HttpClient _userB = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-check-{Guid.NewGuid():N}.db");
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
    public async Task Log_StoresOnlyAHash_NoRawQuery()
    {
        var res = await _userA.PostAsJsonAsync("/api/v1/check/log", new { query = "MK-677 has no side effects", matchedClaimId = (string?)null, doseIntent = false });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _userA.PostAsJsonAsync("/api/v1/check/log", new { query = "x", doseIntent = false })).StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var row = await db.ClaimCheckQueries.SingleAsync();
        Assert.Equal(ClaimCheckEndpoints.HashQuery("MK-677 has no side effects"), row.QueryHash);
        Assert.Null(row.MatchedClaimId);
    }

    [Fact]
    public async Task Submission_IsSanitized_Queued_AndEmergencyGetsTheSafetyMessage()
    {
        var res = await _userA.PostAsJsonAsync("/api/v1/check/submissions", new
        {
            compoundSlug = "bpc-157", claim = "Someone on @guru said 500mcg from peptidesrus.com heals tendons", seenOn = "instagram",
            experience = "side-effect", theme = "chest pain at 2mg",
        });
        Assert.Equal(HttpStatusCode.Accepted, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("redacted").GetBoolean());
        Assert.Equal(ClaimSubmissionSanitizer.SafetyMessage, body.GetProperty("safetyMessage").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var row = await db.ClaimSubmissions.SingleAsync();
        Assert.Equal("pending", row.Status);
        Assert.True(row.Emergency);
        Assert.DoesNotContain("500mcg", row.ClaimText);
        Assert.DoesNotContain("@guru", row.ClaimText);
        Assert.DoesNotContain("peptidesrus.com", row.ClaimText);
        Assert.DoesNotContain("2mg", row.ThemeText);
        Assert.Equal(32, row.SubmitterKey.Length);
    }

    [Fact]
    public async Task Submission_RejectsUnknownFields_AndHoneypotIsSilentlyDropped()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _userA.PostAsJsonAsync("/api/v1/check/submissions", new { compoundSlug = "bpc-157", claim = "heals", seenOn = "billboard" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _userA.PostAsJsonAsync("/api/v1/check/submissions", new { compoundSlug = "bpc-157", claim = "heals", seenOn = "reddit", experience = "cured" })).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await _userA.PostAsJsonAsync("/api/v1/check/submissions", new { compoundSlug = "bpc-157", claim = "heals tendons", seenOn = "reddit", website = "http://bot" })).StatusCode);
        using var scope = _factory.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<BioStackDbContext>().ClaimSubmissions.CountAsync());
    }

    [Fact]
    public void Migration_IsDiscoverable()
    {
        using var scope = _factory.Services.CreateScope();
        var script = scope.ServiceProvider.GetRequiredService<BioStackDbContext>().GetService<IMigrator>()
            .GenerateScript("20261024010000_MapEvidenceTierToSevenTierScale", "20261025010000_AddClaimCheckerAndSubmissions");
        Assert.Contains("CREATE TABLE \"ClaimSubmissions\"", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"ClaimCheckQueries\"", script, StringComparison.Ordinal);
    }
}
