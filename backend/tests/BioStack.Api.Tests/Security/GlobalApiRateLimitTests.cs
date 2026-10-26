namespace BioStack.Api.Tests.Security;

using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

[Trait("Category", "Integration")]
public sealed class GlobalApiRateLimitTests
{
    [Fact]
    public async Task Api_Budget_Is_Shared_Per_Ip_While_Health_And_Other_Clients_Remain_Available()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"global-api-limit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={Path.Combine(directory, "test.db")};Pooling=False");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret", "global-api-limit-test-secret-at-least-32-chars");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");
            });
            using var client = factory.CreateClient(); // Start the real application pipeline.

            async Task<int> Request(string path, string address = "203.0.113.10", string? forwarded = null)
            {
                var context = await factory.Server.SendAsync(http =>
                {
                    http.Request.Method = "GET";
                    http.Request.Path = path;
                    http.Connection.RemoteIpAddress = IPAddress.Parse(address);
                    if (forwarded is not null) http.Request.Headers["X-Forwarded-For"] = forwarded;
                });
                return context.Response.StatusCode;
            }

            // Two API routes (including an unmapped path) must consume one shared budget.
            for (var index = 0; index < 300; index++)
            {
                var mapped = index % 2 == 0;
                Assert.Equal(mapped ? 200 : 404,
                    await Request(mapped ? "/api/v1/auth/session" : "/api/unmapped-rate-limit-test"));
            }

            Assert.Equal(429, await Request("/api/v1/auth/session"));
            // An untrusted client cannot change partitions by spoofing a forwarded IP.
            Assert.Equal(429, await Request("/api/v1/auth/session", forwarded: "198.51.100.99"));
            Assert.Equal(200, await Request("/api/v1/auth/session", "203.0.113.11"));
            Assert.Equal(200, await Request("/health"));
            Assert.Equal(200, await Request("/health/ready"));
            Assert.Equal(404, await Request("/apiary"));

            // The global budget must not replace the stricter auth-verify policy.
            for (var index = 0; index < 11; index++)
            {
                var body = Encoding.UTF8.GetBytes("{\"token\":\"global-limit-test-bogus-token\"}");
                var verification = await factory.Server.SendAsync(http =>
                {
                    http.Request.Method = "POST";
                    http.Request.Path = "/api/v1/auth/verify";
                    http.Request.ContentType = "application/json";
                    http.Request.ContentLength = body.Length;
                    http.Request.Body = new MemoryStream(body);
                    http.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.12");
                });
                Assert.Equal(index < 10 ? 400 : 429, verification.Response.StatusCode);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
