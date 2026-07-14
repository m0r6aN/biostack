namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Contracts.Requests;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Keon;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
    public async Task GetReceiptByUri_Unauthenticated_Returns401()
    {
        var encoded = Uri.EscapeDataString("keon://receipt/does-not-exist");
        var response = await _client.GetAsync($"/api/v1/receipts/{encoded}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptByUri_UnknownUri_Returns404()
    {
        await SignInAsync("receipt-unknown@example.com");
        var encoded = Uri.EscapeDataString("keon://receipt/does-not-exist");
        var response = await _client.GetAsync($"/api/v1/receipts/{encoded}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptByUri_KnownUri_Returns200WithCorrectShape()
    {
        var userId = await SignInAsync("receipt-owner@example.com");
        var receiptUri = $"keon://receipt/integration-test-{Guid.NewGuid():N}";
        var subjectUri = "biostack://srb/proto-integration-001";
        var actorId = ReceiptActor.User(userId).ActorId;

        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        await spine.AppendAsync(new SpineEntry
        {
            ReceiptUri = receiptUri,
            SubjectUri = subjectUri,
            TenantId = "test-tenant",
            ActorId = actorId,
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
        Assert.Equal(actorId, root.GetProperty("actorId").GetString());
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
    public async Task GetReceiptByUri_OtherUsersReceipt_Returns404()
    {
        await SignInAsync("receipt-requester@example.com");
        var otherUserId = Guid.NewGuid();
        var receiptUri = $"keon://receipt/other-user-{Guid.NewGuid():N}";

        await AppendReceiptAsync(receiptUri, "protocol:private", ReceiptActor.User(otherUserId).ActorId);

        var response = await _client.GetAsync($"/api/v1/receipts/{Uri.EscapeDataString(receiptUri)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptsBySubject_ReturnsListForKnownSubject()
    {
        var userId = await SignInAsync("receipt-subject-owner@example.com");
        var subjectUri = $"biostack://srb/subject-{Guid.NewGuid():N}";
        var currentActor = ReceiptActor.User(userId).ActorId;
        await AppendReceiptAsync($"keon://receipt/{Guid.NewGuid():N}", subjectUri, currentActor);
        await AppendReceiptAsync($"keon://receipt/{Guid.NewGuid():N}", subjectUri, ReceiptActor.User(Guid.NewGuid()).ActorId);

        var encodedSubject = Uri.EscapeDataString(subjectUri);
        var response = await _client.GetAsync($"/api/v1/receipts?subject={encodedSubject}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var arr = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.Equal(1, arr.GetArrayLength());
        Assert.Equal(currentActor, arr[0].GetProperty("actorId").GetString());
    }

    [Fact]
    public async Task GetReceiptsByActor_OtherActor_Returns403()
    {
        await SignInAsync("receipt-actor-requester@example.com");
        var otherActor = ReceiptActor.User(Guid.NewGuid()).ActorId;

        var response = await _client.GetAsync($"/api/v1/receipts?actor={Uri.EscapeDataString(otherActor)}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReceiptByUri_AdminCanReadSystemReceipt()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(
            _client,
            _factory,
            "receipt-admin@example.com",
            "/governance/receipts");
        var receiptUri = $"keon://receipt/system-{Guid.NewGuid():N}";
        await AppendReceiptAsync(receiptUri, "system:promotion", ReceiptActor.System("knowledge-worker").ActorId);

        var response = await _client.GetAsync($"/api/v1/receipts/{Uri.EscapeDataString(receiptUri)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<Guid> SignInAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/governance/receipts"));

        using var inboxDoc = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = inboxDoc.RootElement
            .EnumerateArray()
            .First(message => string.Equals(
                message.GetProperty("contact").GetString(),
                email,
                StringComparison.OrdinalIgnoreCase))
            .GetProperty("link")
            .GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        return await db.AppUsers
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private async Task AppendReceiptAsync(string receiptUri, string subjectUri, string actorId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var spine = scope.ServiceProvider.GetRequiredService<ISpineRepository>();
        await spine.AppendAsync(new SpineEntry
        {
            ReceiptUri = receiptUri,
            SubjectUri = subjectUri,
            TenantId = ReceiptActor.ConsumerTenant,
            ActorId = actorId,
            TimestampUtc = DateTime.UtcNow,
            Decision = "non-effecting",
            PolicyHashValue = "h",
            PolicyHashVersion = "v1",
            InputHash = "i",
            EffectStatus = "non-effecting",
        });
    }
}
