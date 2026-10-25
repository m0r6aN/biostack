namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class CommunityEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-community-{Guid.NewGuid():N}.db");
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
                    services.AddDbContext<BioStackDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
                });
            });
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch (IOException) { }
    }

    private async Task<List<Guid>> SeedAsync(int people, string experience, string compound = "bpc-157")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        await db.Database.EnsureCreatedAsync();
        var rows = Enumerable.Range(0, people).Select(i => new ClaimSubmission
        {
            Id = Guid.NewGuid(), CompoundSlug = compound, ClaimText = "heals tendons", SeenOn = "reddit", Experience = experience,
            ThemeText = "felt sick at [removed]", SubmitterKey = $"{experience}-{i}".PadRight(32, 'x'), CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
        }).ToList();
        db.ClaimSubmissions.AddRange(rows);
        await db.SaveChangesAsync();
        return rows.Select(r => r.Id).ToList();
    }

    [Fact]
    public async Task Moderation_IsAdminOnly()
    {
        var ids = await SeedAsync(1, "side-effect");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/admin/community/submissions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync($"/api/v1/admin/community/submissions/{ids[0]}/approve", new { theme = "nausea" })).StatusCode);
    }

    [Fact]
    public async Task ThemesAppearOnlyAfterKIndependentApprovals_AndExposeNoText()
    {
        var sideEffects = await SeedAsync(10, "side-effect");
        var benefits = await SeedAsync(9, "benefit");
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "community-mod@example.com");

        var queue = await _client.GetFromJsonAsync<JsonElement>("/api/v1/admin/community/submissions");
        Assert.Equal(19, queue.GetArrayLength());

        // Controlled vocabulary and kind matching are enforced.
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync($"/api/v1/admin/community/submissions/{sideEffects[0]}/approve", new { theme = "miracle" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync($"/api/v1/admin/community/submissions/{sideEffects[0]}/approve", new { theme = "less-pain" })).StatusCode);

        for (var i = 0; i < 9; i++)
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/v1/admin/community/submissions/{sideEffects[i]}/approve", new { theme = "nausea" })).StatusCode);
        foreach (var id in benefits)
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/v1/admin/community/submissions/{id}/approve", new { theme = "less-pain" })).StatusCode);

        var below = await _client.GetFromJsonAsync<JsonElement>("/api/v1/community/bpc-157/themes");
        Assert.Equal(0, below.GetProperty("sideEffects").GetArrayLength());
        Assert.Equal(0, below.GetProperty("benefits").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, below.GetProperty("submissions").ValueKind);
        Assert.Equal(10, below.GetProperty("minCount").GetInt32());

        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/v1/admin/community/submissions/{sideEffects[9]}/approve", new { theme = "nausea" })).StatusCode);
        var res = await _client.GetAsync("/api/v1/community/bpc-157/themes");
        var raw = await res.Content.ReadAsStringAsync();
        var shown = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("Nausea", Assert.Single(shown.GetProperty("sideEffects").EnumerateArray()).GetProperty("label").GetString());
        Assert.Equal(0, shown.GetProperty("benefits").GetArrayLength());
        Assert.Equal(10, shown.GetProperty("submissions").GetInt32());
        Assert.DoesNotContain("heals tendons", raw);
        Assert.DoesNotContain("felt sick", raw);
        Assert.DoesNotContain("side-effect-", raw);

        // Rejecting drops a person back below k.
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/admin/community/submissions/{sideEffects[0]}/reject", null)).StatusCode);
        var after = await _client.GetFromJsonAsync<JsonElement>("/api/v1/community/bpc-157/themes");
        Assert.Equal(0, after.GetProperty("sideEffects").GetArrayLength());
    }

    [Fact]
    public async Task UnknownOrBadSlug_ReturnsEmptyOrNotFound()
    {
        await SeedAsync(0, "benefit");
        var empty = await _client.GetFromJsonAsync<JsonElement>("/api/v1/community/never-heard-of-it/themes");
        Assert.Equal(0, empty.GetProperty("sideEffects").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/v1/community/BAD%20SLUG!/themes")).StatusCode);
    }
}
