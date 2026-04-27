namespace BioStack.Cognition.CollectiveApi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

/// <summary>
/// Thin HTTP wrapper for the Keon Control /api/collective/* endpoints.
///
/// Integration boundary (per collective-api-integration.md):
///   BioStack server → Keon Control /api/collective/* → Collective Host
///
/// Trust model: Tenant and Actor are derived from trusted server state;
/// body fields are kept for compatibility only.
/// </summary>
internal sealed class CollectiveApiClient
{
    private readonly HttpClient _http;
    private readonly CollectiveApiOptions _options;

    public CollectiveApiClient(HttpClient http, CollectiveApiOptions options)
    {
        _http    = http;
        _options = options;
    }

    /// <summary>
    /// POST /api/collective/live-runs
    /// Submits a new deliberation run and returns the full live-run response.
    /// Returns null on non-success status.
    /// </summary>
    public async Task<CollectiveLiveRunResponse?> SubmitAsync(
        CollectiveSubmitRequest request,
        CancellationToken ct)
    {
        using var req = BuildRequest(HttpMethod.Post, "/api/collective/live-runs", request);
        req.Headers.Add("X-Keon-Tenant-Id",  request.TenantId);
        req.Headers.Add("X-Keon-Actor-Id",   request.ActorId);
        req.Headers.Add("X-Correlation-Id",  request.CorrelationId);

        using var resp = await _http.SendAsync(req, ct);

        // 202 Accepted = host accepted but not yet ready; caller should poll GET
        if (resp.StatusCode == HttpStatusCode.Accepted)
            return null;

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<CollectiveLiveRunResponse>(ct);
    }

    /// <summary>
    /// GET /api/collective/live-runs/{intentId}
    /// Returns null when the host responds 202 (still processing) or on error.
    /// </summary>
    public async Task<CollectiveLiveRunResponse?> GetRunAsync(
        string intentId,
        string tenantId,
        string actorId,
        string correlationId,
        CancellationToken ct)
    {
        using var req = BuildRequest<object?>(HttpMethod.Get,
            $"/api/collective/live-runs/{Uri.EscapeDataString(intentId)}", body: null);
        req.Headers.Add("X-Keon-Tenant-Id", tenantId);
        req.Headers.Add("X-Keon-Actor-Id",  actorId);
        req.Headers.Add("X-Correlation-Id", correlationId);

        using var resp = await _http.SendAsync(req, ct);

        // 202 means still processing — tell the caller to retry
        if (resp.StatusCode == HttpStatusCode.Accepted)
            return null;

        if (!resp.IsSuccessStatusCode)
            return null;

        return await resp.Content.ReadFromJsonAsync<CollectiveLiveRunResponse>(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a base request with Authorization and Accept headers.
    ///
    /// Responsibility split: BuildRequest handles static auth + content-negotiation
    /// headers that are identical for every call. Callers (SubmitAsync / GetRunAsync)
    /// add identity headers (X-Keon-Tenant-Id, X-Keon-Actor-Id, X-Correlation-Id)
    /// because those vary per intent and must be sourced from the call site's context.
    ///
    /// TODO: Extract ICollectiveApiClient if independent stub-ability is needed later.
    /// </summary>
    private HttpRequestMessage BuildRequest<T>(HttpMethod method, string path, T? body)
    {
        var req = new HttpRequestMessage(method, path);

        // Authorization
        if (!string.IsNullOrWhiteSpace(_options.AuthorizationHeader))
            req.Headers.TryAddWithoutValidation("Authorization", _options.AuthorizationHeader);
        else if (!string.IsNullOrWhiteSpace(_options.BearerToken))
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.BearerToken}");

        // Content negotiation — JsonContent.Create sets Content-Type but not Accept
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (body is not null)
            req.Content = JsonContent.Create(body);

        return req;
    }
}
