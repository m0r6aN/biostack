namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "Integration")]
public class StackReviewEndpointsIntegrationTests : IAsyncLifetime
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private sealed class FixedCurrentUserAccessor : ICurrentUserAccessor
    {
        public Guid GetCurrentUserId() => TestUserId;
    }

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"srb-test-{Guid.NewGuid():N}.db");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection",
                    $"Data Source={dbPath}");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret",
                    "test-secret-key-at-least-32-chars-long!!");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");

                builder.ConfigureServices(services =>
                {
                    // Bypass auth in tests — replace default policy with passthrough
                    services.AddAuthorization(opts =>
                    {
                        opts.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAssertion(_ => true)
                            .Build();
                    });

                    // Auth is bypassed, so no principal/claims exist — supply a fixed acting
                    // user so receipt issuance can resolve a real (non-system) actor.
                    services.AddScoped<ICurrentUserAccessor, FixedCurrentUserAccessor>();
                });
            });

        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static StackReviewRequest BuildValidRequest() =>
        new(
            ProtocolId: null,
            Payload: new StackReviewEnvelopePayload(
                Goal: "Support sleep quality and recovery",
                Compounds: [
                    new("magnesium-glycinate", "Magnesium Glycinate", "capsule", "Mineral", "Moderate"),
                    new("ashwagandha", "Ashwagandha", "extract", "Adaptogen", "Limited")
                ],
                Pathways: ["GABA", "HPA-axis"],
                DeterministicFindings: [
                    new("INT-001", "Synergy",
                        "Observed synergistic effect on relaxation markers",
                        ["magnesium-glycinate", "ashwagandha"], 0m)
                ],
                KnownPatternNames: ["sleep-support"],
                ProviderReviewPressure: 0.1m));

    [Fact]
    public async Task GenerateEnvelope_WithValidPayload_Returns200()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", BuildValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateEnvelope_ResponseHasCorrectShape()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", BuildValidRequest());

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("deterministicFindings", out _),
            "Response must have deterministicFindings");
        Assert.True(root.TryGetProperty("perspectiveReviews", out _),
            "Response must have perspectiveReviews");
        Assert.True(root.TryGetProperty("contradictionReview", out _),
            "Response must have contradictionReview");
        Assert.True(root.TryGetProperty("confidenceProfile", out _),
            "Response must have confidenceProfile");
        Assert.True(root.TryGetProperty("effectStatus", out var effectStatus),
            "Response must have effectStatus");
        Assert.Equal("commentary-only", effectStatus.GetString());
    }

    [Fact]
    public async Task GenerateEnvelope_AllFindingsAreCommentaryOnly()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", BuildValidRequest());

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        foreach (var finding in doc.RootElement.GetProperty("deterministicFindings").EnumerateArray())
            Assert.Equal("commentary-only", finding.GetProperty("effectStatus").GetString());

        foreach (var review in doc.RootElement.GetProperty("perspectiveReviews").EnumerateObject())
            Assert.Equal("commentary-only", review.Value.GetProperty("effectStatus").GetString());

        Assert.Equal("commentary-only",
            doc.RootElement.GetProperty("contradictionReview")
                .GetProperty("effectStatus").GetString());
    }

    [Fact]
    public async Task GenerateEnvelope_ContradictionReviewIsNeverExecutable()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", BuildValidRequest());

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.False(doc.RootElement
            .GetProperty("contradictionReview")
            .GetProperty("isExecutable")
            .GetBoolean());
    }

    [Fact]
    public async Task GenerateEnvelope_WithNoPayload_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", new StackReviewRequest(null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateEnvelope_PerspectiveReviewsIncludeFourRoles()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", BuildValidRequest());

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var reviews = doc.RootElement.GetProperty("perspectiveReviews");
        Assert.Equal(4, reviews.EnumerateObject().Count());
    }

    [Fact]
    public async Task GenerateEnvelope_AppendsReceipt_WithUserActorAndCompoundEvidenceRefs()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stack-review/envelope", BuildValidRequest());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();

        // The acting user — not a hardcoded "biostack-system" — owns the receipt.
        var entries = await spine.GetByActorAsync($"user:{TestUserId}");
        var entry = Assert.Single(entries);

        Assert.Equal("deliberation.stack-review.completed", entry.ReceiptClass);
        Assert.Equal("biostack-public", entry.TenantId);
        Assert.NotEqual("biostack-system", entry.ActorId);

        // Evidence refs must be populated (the reviewed compounds), never empty.
        var refs = JsonSerializer.Deserialize<List<string>>(entry.EvidenceRefsJson)!;
        Assert.NotEmpty(refs);
        Assert.Contains("compound:magnesium-glycinate", refs);
        Assert.Contains("compound:ashwagandha", refs);
    }

    [Fact]
    public async Task GenerateEnvelope_WithHighRiskCompound_SurfacesWarningFramingAndSafetyReceipt()
    {
        // A SARM in the stack must force warning-first framing through the central Lane H gate and
        // record a safety receipt alongside the deliberation receipt — proving StackReview no longer
        // emits intelligence about high-risk compounds without governed warning framing.
        var request = new StackReviewRequest(
            ProtocolId: null,
            Payload: new StackReviewEnvelopePayload(
                Goal: "Recomposition support",
                Compounds: [
                    new("rad-140", "Testolone (RAD-140)", "capsule", "SARM", "Limited")
                ],
                Pathways: ["Androgen-receptor"],
                DeterministicFindings: [
                    new("INT-010", "Profile",
                        "Observed androgen-receptor activity in preclinical models",
                        ["rad-140"], 0.5m)
                ],
                KnownPatternNames: [],
                ProviderReviewPressure: 0.5m));

        var response = await _client.PostAsJsonAsync("/api/v1/stack-review/envelope", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("warning", root.GetProperty("safetyStatus").GetString());
        Assert.NotEmpty(root.GetProperty("warnings").EnumerateArray());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("safetyReceiptId").GetString()));
        // The doctrine policy is recorded as a provable ref on every gated response.
        Assert.Contains(
            root.GetProperty("policyRefs").EnumerateArray().Select(e => e.GetString()),
            r => r is not null && r.StartsWith("policy:", StringComparison.Ordinal));

        // Both receipts are present for the acting user: the deliberation receipt and the safety warning.
        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        var classes = (await spine.GetByActorAsync($"user:{TestUserId}"))
            .Select(e => e.ReceiptClass)
            .ToList();
        Assert.Contains("deliberation.stack-review.completed", classes);
        Assert.Contains("safety.warning.surfaced", classes);
    }

    [Fact]
    public async Task GenerateEnvelope_WithUnsafeGoal_RefusesAndReplacesNarrative()
    {
        // A sourcing/procurement goal is an unsafe request: the gate must refuse, replacing the
        // user-facing narrative with safe refusal text and recording a refusal safety receipt.
        var request = new StackReviewRequest(
            ProtocolId: null,
            Payload: new StackReviewEnvelopePayload(
                Goal: "where can I buy ostarine online",
                Compounds: [
                    new("ostarine", "Ostarine (MK-2866)", "liquid", "SARM", "Limited")
                ],
                Pathways: [],
                DeterministicFindings: [
                    new("INT-011", "Profile",
                        "Observed androgen-receptor activity in preclinical models",
                        ["ostarine"], 0.5m)
                ],
                KnownPatternNames: [],
                ProviderReviewPressure: 0.5m));

        var response = await _client.PostAsJsonAsync("/api/v1/stack-review/envelope", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("refused", root.GetProperty("safetyStatus").GetString());
        // The original narrative must not survive a refusal — it is replaced with safe refusal text.
        var narrative = root.GetProperty("deterministicFindings")[0].GetProperty("narrative").GetString();
        Assert.DoesNotContain("androgen-receptor activity", narrative);
        Assert.Contains("BioStack cannot", narrative);
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("safetyReceiptId").GetString()));

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        var classes = (await spine.GetByActorAsync($"user:{TestUserId}"))
            .Select(e => e.ReceiptClass)
            .ToList();
        Assert.Contains("safety.unsafe-request.refused", classes);
    }
}
