namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using BioStack.Api;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class ProviderAccessEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-provider-access-{Guid.NewGuid():N}.db");
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

    [Fact]
    public async Task CreateRequest_PersistsNormalizedContactAndConsentWithoutHealthFields()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            Email = "  Provider@Example.com ",
            Name = "Jordan Provider",
            Organization = "Example Practice",
            Role = "Owner",
            Consent = true,
            ProtocolDetails = "ignored and not persisted",
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var confirmation = await response.Content.ReadFromJsonAsync<ProviderAccessConfirmationResponse>();
        Assert.NotNull(confirmation);
        Assert.Equal("pending", confirmation.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var stored = await db.ProviderAccessRequests.SingleAsync();
        Assert.NotEqual(stored.Id, confirmation.RequestId);
        Assert.Equal("provider@example.com", stored.Email);
        Assert.Equal("provider-access-v1", stored.ConsentVersion);
        Assert.NotEqual(default, stored.ConsentRecordedAtUtc);
        Assert.Equal("pending", stored.Status);
        Assert.Null(stored.Owner);
    }

    [Fact]
    public async Task CreateRequest_IsIdempotentForAnOpenEmailAndRequiresConsent()
    {
        var payload = new
        {
            Email = "provider@example.com",
            Name = "Jordan Provider",
            Organization = "Example Practice",
            Role = "Owner",
            Consent = true,
        };

        var created = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", payload);
        var duplicate = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", payload);
        var rejected = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            payload.Email, payload.Name, payload.Organization, payload.Role, Consent = false,
        });

        Assert.Equal(HttpStatusCode.Accepted, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var createdAcknowledgement = await created.Content.ReadFromJsonAsync<ProviderAccessConfirmationResponse>();
        var duplicateAcknowledgement = await duplicate.Content.ReadFromJsonAsync<ProviderAccessConfirmationResponse>();
        Assert.NotNull(createdAcknowledgement);
        Assert.NotNull(duplicateAcknowledgement);
        Assert.NotEqual(createdAcknowledgement.RequestId, duplicateAcknowledgement.RequestId);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        Assert.Equal(1, await db.ProviderAccessRequests.CountAsync());
        var stored = await db.ProviderAccessRequests.SingleAsync();
        Assert.NotEqual(stored.Id, createdAcknowledgement.RequestId);
        Assert.NotEqual(stored.Id, duplicateAcknowledgement.RequestId);
    }

    [Fact]
    public async Task CreateRequest_DuplicateClosedEmail_DoesNotReopenOrOverwrite()
    {
        const string email = "closed-provider@example.com";
        await _client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            Email = email,
            Name = "Original Provider",
            Organization = "Original Practice",
            Role = "Owner",
            Consent = true,
        });

        Guid storedId;
        DateTime consentRecordedAt;
        DateTime updatedAt;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            var stored = await db.ProviderAccessRequests.SingleAsync();
            stored.Status = "closed";
            stored.Owner = "commercial-owner";
            stored.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            storedId = stored.Id;
            consentRecordedAt = stored.ConsentRecordedAtUtc;
            updatedAt = stored.UpdatedAtUtc;
        }

        var duplicate = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            Email = email,
            Name = "Attacker Replacement",
            Organization = "Replacement Organization",
            Role = "Replacement Role",
            Consent = true,
        });

        Assert.Equal(HttpStatusCode.Accepted, duplicate.StatusCode);
        var acknowledgement = await duplicate.Content.ReadFromJsonAsync<ProviderAccessConfirmationResponse>();
        Assert.NotNull(acknowledgement);
        Assert.Equal("pending", acknowledgement.Status);
        Assert.NotEqual(storedId, acknowledgement.RequestId);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var unchanged = await verificationDb.ProviderAccessRequests.SingleAsync();
        Assert.Equal("Original Provider", unchanged.Name);
        Assert.Equal("Original Practice", unchanged.Organization);
        Assert.Equal("Owner", unchanged.Role);
        Assert.Equal("closed", unchanged.Status);
        Assert.Equal("commercial-owner", unchanged.Owner);
        Assert.Equal(consentRecordedAt, unchanged.ConsentRecordedAtUtc);
        Assert.Equal(updatedAt, unchanged.UpdatedAtUtc);
    }

    [Fact]
    public async Task AdminQueue_ListsAndUpdatesStatusAndOwner()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            Email = "provider@example.com",
            Name = "Jordan Provider",
            Organization = "Example Practice",
            Role = "Owner",
            Consent = true,
        });
        Assert.Equal(HttpStatusCode.Accepted, created.StatusCode);
        Guid persistedRequestId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            persistedRequestId = await db.ProviderAccessRequests.Select(item => item.Id).SingleAsync();
        }

        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-provider-queue@example.com");
        var update = await _client.PatchAsJsonAsync($"/api/v1/admin/provider-access/requests/{persistedRequestId}", new
        {
            Status = "contacted",
            Owner = "commercial-owner",
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var queue = await _client.GetFromJsonAsync<List<ProviderAccessReviewResponse>>(
            "/api/v1/admin/provider-access/requests/?status=contacted&owner=commercial-owner");
        var item = Assert.Single(queue!);
        Assert.Equal("contacted", item.Status);
        Assert.Equal("commercial-owner", item.Owner);
    }
}
