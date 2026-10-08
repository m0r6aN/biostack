namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using BioStack.Api;
using BioStack.Api.Auth;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Regression coverage for BIO-LOCAL-003 finding M1 / BIO-LOCAL-012 remediation: a
/// `.env.example`-style value that is blank or reduced to whitespace (what a copy-pasted,
/// still-unset placeholder looks like once Program.cs trims config values at load) must never
/// be treated as a configured SMTP host, and must never surface a stack trace in Development
/// when the auth flow runs.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SmtpConfigurationTrimRegressionTests : IAsyncLifetime
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t  \t")]
    public async Task BlankOrWhitespaceSmtpHost_UsesInMemoryDeliveryAndDoesNotExposeStackTrace(string smtpHostValue)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"biostack-smtp-trim-{Guid.NewGuid():N}.db");
        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("environment", "Development");
                    builder.ConfigureLogging(logging => logging.ClearProviders());
                    builder.ConfigureAppConfiguration((_, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}",
                            ["FrontendUrl"] = "http://localhost:3043",
                            ["PublicApiUrl"] = "http://localhost:5000",
                            ["Jwt:Secret"] = "test-secret-value-that-is-long-enough-for-hmac",
                            // Simulates the leftover value of a still-unset `.env` line such as
                            // "Smtp__Host=   # leave blank in dev" after the trimming hardening
                            // in Program.cs normalizes it — the bound value must be blank/whitespace,
                            // never the literal comment text.
                            ["Smtp:Host"] = smtpHostValue,
                        });
                    });
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveBioStackDbContext();
                        services.AddDbContext<BioStackDbContext>(options =>
                            options.UseSqlite($"Data Source={dbPath}"));
                    });
                });

            using (var scope = factory.Services.CreateScope())
            {
                var delivery = scope.ServiceProvider.GetRequiredService<IMagicLinkDelivery>();
                Assert.IsType<InMemoryMagicLinkDelivery>(delivery);
            }

            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/start",
                new StartAuthRequest("smtp-trim@example.com", "email", "/mission-control"));

            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("at BioStack.", body, StringComparison.Ordinal);
            Assert.DoesNotContain(".cs:line", body, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
            Assert.DoesNotContain("SmtpException", body, StringComparison.Ordinal);

            using var inboxResponse = await client.GetAsync("/dev/auth/inbox");
            Assert.Equal(HttpStatusCode.OK, inboxResponse.StatusCode);
            var inboxBody = await inboxResponse.Content.ReadAsStringAsync();
            Assert.Contains("smtp-trim@example.com", inboxBody);
        }
        finally
        {
            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
}
