namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Text.Json;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "Integration")]
public class ReceiptEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var dbPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"receipt-test-{Guid.NewGuid():N}.db");

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
            });

        _client = _factory.CreateClient();

        // Trigger schema creation by making a cheap request
        await _client.GetAsync("/health");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetReceiptByUri_UnknownUri_Returns404()
    {
        var encoded = Uri.EscapeDataString("keon://receipt/does-not-exist");
        var response = await _client.GetAsync($"/api/v1/receipts/{encoded}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptByUri_KnownUri_Returns200WithCorrectShape()
    {
        var receiptUri = $"keon://receipt/integration-test-{Guid.NewGuid():N}";
        var subjectUri = "biostack://srb/proto-integration-001";

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        await spine.AppendAsync(new SpineEntry
        {
            ReceiptUri = receiptUri,
            SubjectUri = subjectUri,
            TenantId = "test-tenant",
            ActorId = "test-actor",
            TimestampUtc = DateTime.UtcNow,
            Decision = "commentary-only",
            PolicyHashValue = "sha256:policyabc",
            PolicyHashVersion = "v1.0",
            InputHash = "sha256:inputxyz",
            EvidenceRefsJson = "[\"ref-a\",\"ref-b\"]",
            EffectStatus = "commentary-only",
        });

        var encoded = Uri.EscapeDataString(receiptUri);
        var response = await _client.GetAsync($"/api/v1/receipts/{encoded}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal(receiptUri, root.GetProperty("receiptUri").GetString());
        Assert.Equal(subjectUri, root.GetProperty("subjectUri").GetString());
        Assert.Equal("test-tenant", root.GetProperty("tenantId").GetString());
        Assert.Equal("test-actor", root.GetProperty("actorId").GetString());
        Assert.Equal("commentary-only", root.GetProperty("decision").GetString());
        Assert.Equal("commentary-only", root.GetProperty("effectStatus").GetString());

        var policyHash = root.GetProperty("policyHash");
        Assert.Equal("sha256:policyabc", policyHash.GetProperty("value").GetString());
        Assert.Equal("v1.0", policyHash.GetProperty("version").GetString());

        Assert.Equal("sha256:inputxyz", root.GetProperty("inputHash").GetString());

        var evidenceRefs = root.GetProperty("evidenceRefs");
        Assert.Equal(JsonValueKind.Array, evidenceRefs.ValueKind);
        Assert.Equal(2, evidenceRefs.GetArrayLength());
    }

    [Fact]
    public async Task GetReceiptsBySubject_ReturnsListForKnownSubject()
    {
        var subjectUri = $"biostack://srb/subject-{Guid.NewGuid():N}";

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        await spine.AppendAsync(new SpineEntry
        {
            ReceiptUri = $"keon://receipt/{Guid.NewGuid():N}",
            SubjectUri = subjectUri,
            TenantId = "t",
            ActorId = "a",
            TimestampUtc = DateTime.UtcNow,
            Decision = "non-effecting",
            PolicyHashValue = "h",
            PolicyHashVersion = "v1",
            InputHash = "i",
            EffectStatus = "non-effecting",
        });

        var encodedSubject = Uri.EscapeDataString(subjectUri);
        var response = await _client.GetAsync($"/api/v1/receipts?subject={encodedSubject}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var arr = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.True(arr.GetArrayLength() >= 1);
    }
}
