namespace BioStack.Application.Tests.Cognition;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Cognition.CollectiveApi;
using Keon.Collective;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

public sealed class CollectiveOutboundBoundaryInvestigationTests
{
    private const string CompoundSentinel = "Q01_SYNTHETIC_COMPOUND_SENTINEL";
    private const string UserGoalSentinel = "Q01_SYNTHETIC_USER_GOAL_SENTINEL";
    private const string TenantSentinel = "q01-synthetic-tenant";
    private const string ActorSentinel = "q01-synthetic-actor";
    private const string CorrelationSentinel = "q01-synthetic-correlation";
    private const string IntentSentinel = "q01-synthetic-intent";

    private readonly ITestOutputHelper _output;

    public CollectiveOutboundBoundaryInvestigationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RunAsync_without_current_user_authorization_must_not_emit_collective_payload()
    {
        var options = new CollectiveApiOptions
        {
            LiveMode = true,
            ControlBaseUrl = "https://q01-collective.invalid",
            AuthorizationHeader = null,
            BearerToken = null,
            PollDelayMs = 0,
        };

        var intent = new CollectiveIntent(
            new IntentId(IntentSentinel),
            $"Evaluate {CompoundSentinel} for {UserGoalSentinel}",
            "{\"synthetic\":true}",
            new TenantContext(TenantSentinel),
            new ActorContext(ActorSentinel, "SyntheticActor"),
            new CorrelationContext(CorrelationSentinel));

        var handler = new RecordingHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.ControlBaseUrl + "/"),
        };
        var factory = new NamedClientFactory(http);
        var orchestrator = new CollectiveLiveOrchestrator(
            factory,
            options,
            new FixedAuthorizationGate(false),
            NullLogger<CollectiveLiveOrchestrator>.Instance);

        var envelope = await orchestrator.RunAsync(intent);

        _output.WriteLine($"Q01_REQUEST_COUNT={handler.Requests.Count}");
        _output.WriteLine($"Q01_FACTORY_CLIENT_NAME={factory.RequestedName ?? "<none>"}");
        _output.WriteLine($"Q01_SYNTHETIC_RESPONSE_MODEL={envelope.ConfidenceProfile.Model}");

        if (handler.Requests.Count == 1)
        {
            var request = handler.Requests[0];
            using var document = JsonDocument.Parse(request.Body);
            var root = document.RootElement;
            var fields = root.EnumerateObject()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            _output.WriteLine($"Q01_METHOD={request.Method}");
            _output.WriteLine($"Q01_RESOLVED_URI={request.ResolvedUri}");
            _output.WriteLine($"Q01_PATH={request.Path}");
            _output.WriteLine($"Q01_FIELDS={string.Join(",", fields)}");
            _output.WriteLine($"Q01_COMPOUND_SENTINEL_CROSSED={Contains(root, "objective", CompoundSentinel)}");
            _output.WriteLine($"Q01_USER_GOAL_SENTINEL_CROSSED={Contains(root, "objective", UserGoalSentinel)}");
            _output.WriteLine($"Q01_TENANT_SENTINEL_CROSSED={Equals(root, "tenantId", TenantSentinel)}");
            _output.WriteLine($"Q01_ACTOR_SENTINEL_CROSSED={Equals(root, "actorId", ActorSentinel)}");
            _output.WriteLine($"Q01_CORRELATION_SENTINEL_CROSSED={Equals(root, "correlationId", CorrelationSentinel)}");
            _output.WriteLine($"Q01_INTENT_SENTINEL_CROSSED={Equals(root, "intentId", IntentSentinel)}");
            _output.WriteLine($"Q01_CONTEXT_IS_NULL={root.TryGetProperty("context", out var context) && context.ValueKind == JsonValueKind.Null}");
            _output.WriteLine($"Q01_AUTHORIZATION_HEADER_PRESENT={request.AuthorizationHeaderPresent}");
            _output.WriteLine($"Q01_TENANT_HEADER={request.TenantHeader ?? "<absent>"}");
            _output.WriteLine($"Q01_ACTOR_HEADER={request.ActorHeader ?? "<absent>"}");
            _output.WriteLine($"Q01_CORRELATION_HEADER={request.CorrelationHeader ?? "<absent>"}");
        }

        Assert.True(
            handler.Requests.Count == 0 && handler.Requests.All(request => request.Body.Length == 0),
            "Without a current-user authorization or consent decision, no Collective request or body may cross the outbound handler boundary.");
        Assert.Equal("COLLECTIVE_UNAVAILABLE", envelope.ConfidenceProfile.Model);
        Assert.Null(factory.RequestedName);
    }

    [Fact]
    public async Task RunAsync_when_authorization_gate_fails_must_not_emit_collective_payload()
    {
        var options = CreateOptions();
        var handler = new RecordingHandler();
        var factory = new NamedClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri(options.ControlBaseUrl + "/"),
        });
        var orchestrator = new CollectiveLiveOrchestrator(
            factory,
            options,
            new ThrowingAuthorizationGate(),
            NullLogger<CollectiveLiveOrchestrator>.Instance);

        var envelope = await orchestrator.RunAsync(CreateIntent());

        Assert.Equal("COLLECTIVE_UNAVAILABLE", envelope.ConfidenceProfile.Model);
        Assert.Empty(handler.Requests);
        Assert.Null(factory.RequestedName);
    }

    [Fact]
    public async Task RunAsync_with_current_authorization_emits_one_synthetic_collective_submit()
    {
        var options = CreateOptions();
        var handler = new RecordingHandler();
        var factory = new NamedClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri(options.ControlBaseUrl + "/"),
        });
        var orchestrator = new CollectiveLiveOrchestrator(
            factory,
            options,
            new FixedAuthorizationGate(true),
            NullLogger<CollectiveLiveOrchestrator>.Instance);

        var envelope = await orchestrator.RunAsync(CreateIntent());

        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method);
        Assert.Equal("/api/collective/live-runs", request.Path);
        Assert.Equal(CollectiveLiveOrchestrator.HttpClientName, factory.RequestedName);
        Assert.Equal("Q01_SYNTHETIC_MODEL", envelope.ConfidenceProfile.Model);

        using var document = JsonDocument.Parse(request.Body);
        var root = document.RootElement;
        Assert.True(Contains(root, "objective", CompoundSentinel));
        Assert.True(Contains(root, "objective", UserGoalSentinel));
        Assert.True(Equals(root, "tenantId", TenantSentinel));
        Assert.True(Equals(root, "actorId", ActorSentinel));
        Assert.True(Equals(root, "correlationId", CorrelationSentinel));
        Assert.True(Equals(root, "intentId", IntentSentinel));
    }

    private static CollectiveApiOptions CreateOptions() => new()
    {
        LiveMode = true,
        ControlBaseUrl = "https://q01-collective.invalid",
        AuthorizationHeader = null,
        BearerToken = null,
        PollDelayMs = 0,
    };

    private static CollectiveIntent CreateIntent() => new(
        new IntentId(IntentSentinel),
        $"Evaluate {CompoundSentinel} for {UserGoalSentinel}",
        "{\"synthetic\":true}",
        new TenantContext(TenantSentinel),
        new ActorContext(ActorSentinel, "SyntheticActor"),
        new CorrelationContext(CorrelationSentinel));

    private static bool Contains(JsonElement root, string propertyName, string sentinel) =>
        root.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString()!.Contains(sentinel, StringComparison.Ordinal);

    private static bool Equals(JsonElement root, string propertyName, string sentinel) =>
        root.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
        && string.Equals(value.GetString(), sentinel, StringComparison.Ordinal);

    private sealed class FixedAuthorizationGate : ICollectiveOutboundAuthorizationGate
    {
        private readonly bool _authorized;

        public FixedAuthorizationGate(bool authorized)
        {
            _authorized = authorized;
        }

        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_authorized);
    }

    private sealed class ThrowingAuthorizationGate : ICollectiveOutboundAuthorizationGate
    {
        public Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic current-user authorization failure.");
    }

    private sealed class NamedClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public NamedClientFactory(HttpClient client)
        {
            _client = client;
        }

        public string? RequestedName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            RequestedName = name;
            if (!string.Equals(name, CollectiveLiveOrchestrator.HttpClientName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unexpected client name: {name}");
            }

            return _client;
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new CapturedRequest(
                request.Method.Method,
                request.RequestUri?.AbsoluteUri ?? string.Empty,
                request.RequestUri?.AbsolutePath ?? string.Empty,
                body,
                request.Headers.Contains("Authorization"),
                Header(request, "X-Keon-Tenant-Id"),
                Header(request, "X-Keon-Actor-Id"),
                Header(request, "X-Correlation-Id")));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    dataMode = "SYNTHETIC",
                    retrievalMode = "LOCAL_FAKE",
                    run = new
                    {
                        intentId = IntentSentinel,
                        correlationId = CorrelationSentinel,
                        cognitionSurfaces = new
                        {
                            perspectiveReviews = new Dictionary<string, object>(),
                            contradictionReview = (object?)null,
                            confidenceProfile = new
                            {
                                model = "Q01_SYNTHETIC_MODEL",
                                epistemic = "synthetic",
                                evidenceSupport = "synthetic",
                                contradictionDensity = "synthetic",
                                calibrationVersion = "synthetic",
                            },
                            reasoningGraphRef = new
                            {
                                graphId = "q01-synthetic-graph",
                                nodeCount = 0,
                                edgeCount = 0,
                            },
                            witnessSignature = "q01-synthetic-signature",
                        },
                    },
                    operatorMessages = Array.Empty<object>(),
                }),
            };
        }

        private static string? Header(HttpRequestMessage request, string name) =>
            request.Headers.TryGetValues(name, out var values) ? values.SingleOrDefault() : null;
    }

    private sealed record CapturedRequest(
        string Method,
        string ResolvedUri,
        string Path,
        string Body,
        bool AuthorizationHeaderPresent,
        string? TenantHeader,
        string? ActorHeader,
        string? CorrelationHeader);
}
