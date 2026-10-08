namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
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

    [Fact]
    public async Task AdminQueue_RejectsAnonymousAndOrdinaryUsers()
    {
        var requestId = Guid.NewGuid();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/v1/admin/provider-access/requests/")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.PatchAsJsonAsync($"/api/v1/admin/provider-access/requests/{requestId}", new
            {
                Status = "contacted",
                Owner = "owner",
            })).StatusCode);

        await SignInAsync("ordinary-provider-queue@example.com");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.GetAsync("/api/v1/admin/provider-access/requests/")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.PatchAsJsonAsync($"/api/v1/admin/provider-access/requests/{requestId}", new
            {
                Status = "contacted",
                Owner = "owner",
            })).StatusCode);
    }

    [Fact]
    public async Task AdminDemotion_TakesEffectOnTheActiveCookie()
    {
        const string email = "demoted-provider-admin@example.com";
        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, email);

        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
            userId = await db.AppUsers
                .Where(user => user.Email == email)
                .Select(user => user.Id)
                .SingleAsync();
        }

        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.GetAsync("/api/v1/admin/provider-access/requests/")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.PostAsync($"/api/v1/admin/users/{userId}/demote", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.GetAsync("/api/v1/admin/provider-access/requests/")).StatusCode);
    }

    [Fact]
    public async Task CreateRequest_SixthRequestInWindow_Returns429AndEmitsRejectionSignalExactlyOnce()
    {
        using var logSink = new CapturingLoggerProvider();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logSink));
        });
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/v1/provider-access/requests", new
            {
                Email = $"rate-limit-{i}@example.com",
                Name = "Jordan Provider",
                Organization = "Example Practice",
                Role = "Owner",
                Consent = true,
            });
            Assert.Equal(HttpStatusCode.Accepted, ok.StatusCode);
        }

        var sixth = await client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            Email = "rate-limit-6@example.com",
            Name = "Jordan Provider",
            Organization = "Example Practice",
            Role = "Owner",
            Consent = true,
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        var rejectionLogCount = logSink.Messages.Count(message => message.Contains("ProviderAccessRateLimitRejected", StringComparison.Ordinal));
        Assert.Equal(1, rejectionLogCount);
    }

    [Fact]
    public async Task RetentionSweep_AnonymizesOnlyEligibleClosedRequestsAndIsAdminOnly()
    {
        Guid eligibleId, insideWindowId, nonClosedId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

            var eligible = new BioStack.Domain.Entities.ProviderAccessRequest
            {
                Id = Guid.NewGuid(),
                Email = "eligible-retention@example.com",
                Name = "Eligible Provider",
                Organization = "Eligible Org",
                Role = "Owner",
                Status = "closed",
                Owner = "commercial-owner",
                ConsentVersion = "provider-access-v1",
                ConsentRecordedAtUtc = DateTime.UtcNow.AddDays(-400),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-400),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-400),
            };
            var insideWindow = new BioStack.Domain.Entities.ProviderAccessRequest
            {
                Id = Guid.NewGuid(),
                Email = "inside-window-retention@example.com",
                Name = "Inside Window Provider",
                Organization = "Inside Window Org",
                Role = "Owner",
                Status = "closed",
                Owner = "commercial-owner",
                ConsentVersion = "provider-access-v1",
                ConsentRecordedAtUtc = DateTime.UtcNow.AddDays(-30),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-30),
            };
            var nonClosed = new BioStack.Domain.Entities.ProviderAccessRequest
            {
                Id = Guid.NewGuid(),
                Email = "non-closed-retention@example.com",
                Name = "Non Closed Provider",
                Organization = "Non Closed Org",
                Role = "Owner",
                Status = "pending",
                ConsentVersion = "provider-access-v1",
                ConsentRecordedAtUtc = DateTime.UtcNow.AddDays(-400),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-400),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-400),
            };

            db.ProviderAccessRequests.AddRange(eligible, insideWindow, nonClosed);
            await db.SaveChangesAsync();
            eligibleId = eligible.Id;
            insideWindowId = insideWindow.Id;
            nonClosedId = nonClosed.Id;
        }

        var anonymousAttempt = await _client.PostAsync("/api/v1/admin/provider-access/requests/retention-sweep", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousAttempt.StatusCode);

        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-retention-sweep@example.com");
        var sweepResponse = await _client.PostAsync("/api/v1/admin/provider-access/requests/retention-sweep", null);
        Assert.Equal(HttpStatusCode.OK, sweepResponse.StatusCode);
        var sweep = await sweepResponse.Content.ReadFromJsonAsync<ProviderAccessRetentionSweepResponse>();
        Assert.NotNull(sweep);
        Assert.Equal(1, sweep!.AnonymizedCount);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BioStackDbContext>();

        var anonymized = await verifyDb.ProviderAccessRequests.SingleAsync(item => item.Id == eligibleId);
        Assert.EndsWith("@redacted.invalid", anonymized.Email, StringComparison.Ordinal);
        Assert.Equal("[redacted]", anonymized.Name);
        Assert.Equal("[redacted]", anonymized.Organization);
        Assert.Equal("[redacted]", anonymized.Role);
        Assert.Equal("closed", anonymized.Status);
        Assert.Equal("commercial-owner", anonymized.Owner);
        Assert.Equal("provider-access-v1", anonymized.ConsentVersion);

        var untouchedInsideWindow = await verifyDb.ProviderAccessRequests.SingleAsync(item => item.Id == insideWindowId);
        Assert.Equal("inside-window-retention@example.com", untouchedInsideWindow.Email);

        var untouchedNonClosed = await verifyDb.ProviderAccessRequests.SingleAsync(item => item.Id == nonClosedId);
        Assert.Equal("non-closed-retention@example.com", untouchedNonClosed.Email);
    }

    [Fact]
    public async Task AdminQueue_ComputesSlaStalenessAgainstTheConfiguredThreshold()
    {
        Guid underId, atBoundaryId, overId, nonPendingOverId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

            BioStack.Domain.Entities.ProviderAccessRequest Make(string email, string status, int daysOld)
                => new()
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    Name = "SLA Provider",
                    Organization = "SLA Org",
                    Role = "Owner",
                    Status = status,
                    ConsentVersion = "provider-access-v1",
                    ConsentRecordedAtUtc = DateTime.UtcNow.AddDays(-daysOld),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-daysOld),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-daysOld),
                };

            var under = Make("sla-under@example.com", "pending", 2);
            var atBoundary = Make("sla-at-boundary@example.com", "pending", 5);
            var over = Make("sla-over@example.com", "pending", 9);
            var nonPendingOver = Make("sla-non-pending-over@example.com", "contacted", 30);

            db.ProviderAccessRequests.AddRange(under, atBoundary, over, nonPendingOver);
            await db.SaveChangesAsync();
            underId = under.Id;
            atBoundaryId = atBoundary.Id;
            overId = over.Id;
            nonPendingOverId = nonPendingOver.Id;
        }

        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-sla-staleness@example.com");
        var queue = await _client.GetFromJsonAsync<List<ProviderAccessReviewResponse>>(
            "/api/v1/admin/provider-access/requests/");
        Assert.NotNull(queue);

        var underResult = queue!.Single(item => item.RequestId == underId);
        Assert.False(underResult.IsOverdue);

        var atBoundaryResult = queue!.Single(item => item.RequestId == atBoundaryId);
        Assert.False(atBoundaryResult.IsOverdue);

        var overResult = queue!.Single(item => item.RequestId == overId);
        Assert.True(overResult.IsOverdue);
        Assert.True(overResult.DaysOpen > 5);

        var nonPendingOverResult = queue!.Single(item => item.RequestId == nonPendingOverId);
        Assert.False(nonPendingOverResult.IsOverdue);
    }

    [Fact]
    public async Task AdminQueue_PatchCreatesExactlyOneAuditRowPerChangeAndNoneOnNoOp()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/provider-access/requests", new
        {
            Email = "audit-trail@example.com",
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

        await AdminAuthTestHelper.SignInAsAdminAsync(_client, _factory, "admin-audit-trail@example.com");

        var firstUpdate = await _client.PatchAsJsonAsync(
            $"/api/v1/admin/provider-access/requests/{persistedRequestId}",
            new { Status = "contacted", Owner = "commercial-owner" });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        // No-op PATCH: identical status and owner should not create a second audit row.
        var noOpUpdate = await _client.PatchAsJsonAsync(
            $"/api/v1/admin/provider-access/requests/{persistedRequestId}",
            new { Status = "contacted", Owner = "commercial-owner" });
        Assert.Equal(HttpStatusCode.OK, noOpUpdate.StatusCode);

        using var scopeVerify = _factory.Services.CreateScope();
        var verifyDb = scopeVerify.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var auditRows = await verifyDb.ProviderAccessAuditEntries
            .Where(item => item.RequestId == persistedRequestId)
            .ToListAsync();

        var row = Assert.Single(auditRows);
        Assert.Equal("pending", row.FromStatus);
        Assert.Equal("contacted", row.ToStatus);
        Assert.Null(row.FromOwner);
        Assert.Equal("commercial-owner", row.ToOwner);
        Assert.NotEqual(Guid.Empty, row.ActorId);

        var adminUserId = await verifyDb.AppUsers
            .Where(user => user.Email == "admin-audit-trail@example.com")
            .Select(user => user.Id)
            .SingleAsync();
        Assert.Equal(adminUserId, row.ActorId);
    }

    [Fact]
    public async Task AuditTrail_IsNotExposedOnAnyPublicEndpoint()
    {
        var response = await _client.GetAsync("/api/v1/provider-access/audit");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// PR-PROV-001 deployment-config review (Required Tests, "Deployment-config review" item):
    /// a deterministic local/test-mode check that docker-compose.yml declares all four
    /// KeonRuntime__* pass-through keys for biostack-api with defaults that preserve current
    /// stub-mode behavior, and that .env.example documents the three new KEON_RUNTIME_* keys as
    /// blank. This does not deploy or touch any live value.
    /// </summary>
    [Fact]
    public void DeploymentConfigReview_ComposeDeclaresKeonRuntimePassThroughWithStubPreservingDefaults()
    {
        var repoRoot = FindRepoRoot();
        var compose = File.ReadAllText(Path.Combine(repoRoot, "docker-compose.yml"));
        var envExample = File.ReadAllText(Path.Combine(repoRoot, ".env.example"));

        Assert.Contains("KeonRuntime__LiveMode=false", compose, StringComparison.Ordinal);
        Assert.Contains("KeonRuntime__BaseUrl=${KEON_RUNTIME_BASE_URL:-}", compose, StringComparison.Ordinal);
        Assert.Contains("KeonRuntime__BearerToken=${KEON_RUNTIME_BEARER_TOKEN:-}", compose, StringComparison.Ordinal);
        Assert.Contains("KeonRuntime__TimeoutMs=${KEON_RUNTIME_TIMEOUT_MS:-5000}", compose, StringComparison.Ordinal);

        Assert.Contains("KEON_RUNTIME_BASE_URL=", envExample, StringComparison.Ordinal);
        Assert.Contains("KEON_RUNTIME_BEARER_TOKEN=", envExample, StringComparison.Ordinal);
        Assert.Contains("KEON_RUNTIME_TIMEOUT_MS=5000", envExample, StringComparison.Ordinal);

        // The newly-documented keys default to blank/safe values — asserting the literal
        // "KEY=" (no trailing value) form for BaseUrl/BearerToken confirms no live value leaked
        // into this parcel's artifacts.
        Assert.Contains("KEON_RUNTIME_BASE_URL=\n", envExample, StringComparison.Ordinal);
        Assert.Contains("KEON_RUNTIME_BEARER_TOKEN=\n", envExample, StringComparison.Ordinal);
    }

    /// <summary>
    /// PR-PROV-001 deployment-config review: confirms (via a local ASPNETCORE_ENVIRONMENT
    /// simulation, not a deployment) that the existing fail-closed Production boot check in
    /// KeonRuntimeDependencyInjection.cs is unchanged. This calls the exact same public
    /// extension method Program.cs calls (<c>AddKeonRuntime(configuration, isProduction)</c>)
    /// directly against a minimal in-memory configuration, which exercises the real check
    /// without requiring every unrelated production-only precondition elsewhere in Program.cs
    /// (Stripe/Azure Data Protection/etc.) to also be satisfied first -- those are already each
    /// covered by their own dedicated validators and are out of this parcel's Allowed Files.
    /// </summary>
    [Fact]
    public void DeploymentConfigReview_ProductionBootStillFailsClosedWithoutLiveKeonConfiguration()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddKeonRuntime(emptyConfiguration, isProduction: true));
        Assert.Contains("KeonRuntime is not in live mode", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Companion to the fail-closed check above: the same call with <c>isProduction: false</c>
    /// (Development/Testing posture) must not throw and must register the fail-closed stub
    /// client, not a live client, matching current behavior.
    /// </summary>
    [Fact]
    public void DeploymentConfigReview_NonProductionBootStartsInStubModeWithoutLiveKeonConfiguration()
    {
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddKeonRuntime(emptyConfiguration, isProduction: false);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IKeonRuntimeClient>();
        Assert.IsType<KeonRuntimeClientStub>(client);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate repository root containing docker-compose.yml.");
        }

        return directory.FullName;
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly System.Collections.Concurrent.ConcurrentBag<string> _messages;

            public CapturingLogger(System.Collections.Concurrent.ConcurrentBag<string> messages) => _messages = messages;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _messages.Add(formatter(state, exception));
            }
        }
    }

    private async Task SignInAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/start",
            new StartAuthRequest(email, "email", "/providers"));
        using var inbox = await JsonDocument.ParseAsync(await _client.GetStreamAsync("/dev/auth/inbox"));
        var link = inbox.RootElement
            .EnumerateArray()
            .First(message => string.Equals(
                message.GetProperty("contact").GetString(),
                email,
                StringComparison.OrdinalIgnoreCase))
            .GetProperty("link")
            .GetString()!;
        var uri = new Uri(link);
        await _client.GetAsync($"{uri.AbsolutePath}{uri.Query}");
    }
}
