namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Infrastructure.Keon;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "Integration")]
public class PolicyGateEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factoryDefault = null!;  // stub blocks (fail-closed)
    private WebApplicationFactory<Program> _factoryAllowAll = null!; // stub allows all

    private HttpClient _clientDefault = null!;
    private HttpClient _clientAllowAll = null!;

    public Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"pg-test-{Guid.NewGuid():N}.db");

        _factoryDefault = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret", "test-secret-key-at-least-32-chars-long!!");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");

                builder.ConfigureServices(services =>
                {
                    services.AddAuthorization(opts =>
                    {
                        opts.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAssertion(_ => true)
                            .Build();
                    });

                    // appsettings.Development.json sets StubAllowAll=true which would let
                    // all policy checks pass. For the fail-closed test factory we must
                    // replace the KeonRuntimeOptions singleton after it was registered by
                    // AddKeonRuntime so the stub uses StubAllowAll=false.
                    var existing = services.FirstOrDefault(d => d.ServiceType == typeof(KeonRuntimeOptions));
                    if (existing is not null)
                        services.Remove(existing);
                    services.AddSingleton(new KeonRuntimeOptions { StubAllowAll = false });
                });
            });

        var dbPath2 = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"pg-test2-{Guid.NewGuid():N}.db");

        _factoryAllowAll = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath2}");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret", "test-secret-key-at-least-32-chars-long!!");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");

                builder.ConfigureServices(services =>
                {
                    services.AddAuthorization(opts =>
                    {
                        opts.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAssertion(_ => true)
                            .Build();
                    });
                    // Enable stub allow-all so neutral text passes through
                    var existing = services.FirstOrDefault(d => d.ServiceType == typeof(KeonRuntimeOptions));
                    if (existing is not null)
                        services.Remove(existing);
                    services.AddSingleton(new KeonRuntimeOptions { StubAllowAll = true });
                });
            });

        _clientDefault  = _factoryDefault.CreateClient();
        _clientAllowAll = _factoryAllowAll.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _clientDefault.Dispose();
        _clientAllowAll.Dispose();
        await _factoryDefault.DisposeAsync();
        await _factoryAllowAll.DisposeAsync();
    }

    // ── /classify ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Classify_ProhibitedPhrase_Returns200WithBlockedDecision()
    {
        var payload = new
        {
            text = "you should take 500mg daily",
            context = "srb-finding",
            tenantId = "t1",
            actorId = "a1"
        };

        var response = await _clientDefault.PostAsJsonAsync("/api/v1/policy/classify", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("blocked", root.GetProperty("decision").GetString());
        Assert.True(root.GetProperty("locallyClassified").GetBoolean(),
            "Prohibited phrase should be caught by local classifier");
        Assert.True(root.TryGetProperty("blockReason", out var blockReason));
        Assert.Contains("local-classifier", blockReason.GetString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Classify_NeutralPhrase_WithStubBlocked_Returns200WithBlockedDecision()
    {
        // Default factory: stub is fail-closed (blocks everything not locally prohibited)
        var payload = new
        {
            text = "Magnesium supports sleep quality",
            context = "srb-finding",
            tenantId = "t1",
            actorId = "a1"
        };

        var response = await _clientDefault.PostAsJsonAsync("/api/v1/policy/classify", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("blocked", root.GetProperty("decision").GetString());
        Assert.False(root.GetProperty("locallyClassified").GetBoolean(),
            "Neutral phrase should reach Keon stub, not be blocked locally");
    }

    [Fact]
    public async Task Classify_NeutralPhrase_WithStubAllowAll_Returns200WithAllowedDecision()
    {
        var payload = new
        {
            text = "Studies suggest an association between ashwagandha and recovery markers.",
            context = "compound-dossier",
            tenantId = "t1",
            actorId = "a1"
        };

        var response = await _clientAllowAll.PostAsJsonAsync("/api/v1/policy/classify", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("allowed", root.GetProperty("decision").GetString());
        Assert.False(root.GetProperty("locallyClassified").GetBoolean());
    }

    [Fact]
    public async Task Classify_EmptyText_Returns400()
    {
        var payload = new
        {
            text = "",
            context = "srb-finding",
            tenantId = "t1",
            actorId = "a1"
        };

        var response = await _clientDefault.PostAsJsonAsync("/api/v1/policy/classify", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── /check (alias) ────────────────────────────────────────────────────────

    [Fact]
    public async Task Check_ProhibitedPhrase_Returns200WithBlockedDecision()
    {
        var payload = new
        {
            text = "this cures the condition",
            context = "mission-control",
            tenantId = "t1",
            actorId = "a1"
        };

        var response = await _clientDefault.PostAsJsonAsync("/api/v1/policy/check", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("blocked", doc.RootElement.GetProperty("decision").GetString());
    }

    // ── Response shape ────────────────────────────────────────────────────────

    [Fact]
    public async Task Classify_ResponseHasExpectedShape()
    {
        var payload = new
        {
            text = "Magnesium supports sleep quality",
            context = "srb-finding",
            tenantId = "t1",
            actorId = "a1"
        };

        var response = await _clientDefault.PostAsJsonAsync("/api/v1/policy/classify", payload);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("decision", out _),          "must have decision");
        Assert.True(root.TryGetProperty("blockReason", out _),       "must have blockReason");
        Assert.True(root.TryGetProperty("policyHash", out var ph),   "must have policyHash");
        Assert.True(root.TryGetProperty("locallyClassified", out _), "must have locallyClassified");
        Assert.True(ph.TryGetProperty("value", out _),               "policyHash must have value");
        Assert.True(ph.TryGetProperty("version", out _),             "policyHash must have version");
    }
}
