namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using BioStack.Api.Endpoints;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

[Trait("Category", "Integration")]
public sealed class KompressEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private string _dbPath = string.Empty;
    private string _kompressPath = string.Empty;

    public Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-kompress-api-{Guid.NewGuid():N}.db");
        _kompressPath = Path.Combine(Path.GetTempPath(), $"biostack-kompress-store-{Guid.NewGuid():N}.db");
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
                        ["Kompress:StorePath"] = _kompressPath,
                        ["Kompress:TenantId"] = "biostack-tests",
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

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        DeleteSqliteFiles(_dbPath);
        DeleteSqliteFiles(_kompressPath);
    }

    [Fact]
    public async Task Compress_RequiresAdminAuthorization()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/admin/kompress/compress",
            new KompressContentRequest("some content"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompressAndRetrieve_RoundTripsOriginalContent()
    {
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-kompress@example.com");
        var original = string.Join('\n', Enumerable.Repeat(
            "2026-07-10T12:00:00Z ERROR retry failed attempt=3 operation=collective-live-run", 400));

        var compressResponse = await _client.PostAsJsonAsync(
            "/api/v1/admin/kompress/compress",
            new KompressContentRequest(original, "log", "integration-test"));

        Assert.Equal(HttpStatusCode.OK, compressResponse.StatusCode);
        var compressed = await compressResponse.Content.ReadFromJsonAsync<KompressContentResponse>();
        Assert.NotNull(compressed);
        Assert.True(compressed!.TokensAfter < compressed.TokensBefore);
        Assert.True(compressed.Retrievable);
        var hash = Assert.Single(compressed.RetrievalHashes);

        var retrieveResponse = await _client.PostAsJsonAsync(
            "/api/v1/admin/kompress/retrieve",
            new RetrieveKompressedContentRequest(hash));

        Assert.Equal(HttpStatusCode.OK, retrieveResponse.StatusCode);
        var retrieved = await retrieveResponse.Content.ReadFromJsonAsync<RetrieveKompressedContentResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(original, retrieved!.Content);
    }

    private static void DeleteSqliteFiles(string path)
    {
        foreach (var candidate in new[] { path, $"{path}-wal", $"{path}-shm" })
        {
            try
            {
                if (File.Exists(candidate))
                    File.Delete(candidate);
            }
            catch (IOException) { }
        }
    }
}
