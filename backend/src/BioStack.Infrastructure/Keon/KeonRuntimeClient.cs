namespace BioStack.Infrastructure.Keon;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Live IKeonRuntimeClient that calls the Keon Runtime REST API.
/// Uses IHttpClientFactory — never constructs HttpClient directly.
/// Fail-closed: any exception on PolicyCheck/CheckEvidenceGate returns Blocked.
/// Any exception on IssueReceipt throws KeonRuntimeUnavailableException.
/// </summary>
internal sealed class KeonRuntimeClient(
    IHttpClientFactory httpClientFactory,
    KeonRuntimeOptions options,
    ILogger<KeonRuntimeClient> logger) : IKeonRuntimeClient
{
    internal const string HttpClientName = "keon-runtime";

    private static readonly PolicyHash FailedHash = new("ERROR", "0.0.0");

    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(options.TimeoutMs);

    public async Task<KeonHealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            using var http = httpClientFactory.CreateClient(HttpClientName);
            http.Timeout = _timeout;
            var response = await http.GetAsync("health", ct);
            return response.IsSuccessStatusCode
                ? new KeonHealthStatus(true, KeonRuntimeMode.Live, null)
                : new KeonHealthStatus(false, KeonRuntimeMode.Degraded,
                    $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[KeonRuntime] Health check failed");
            return new KeonHealthStatus(false, KeonRuntimeMode.Offline, ex.Message);
        }
    }

    public async Task<PolicyGateResult> PolicyCheckAsync(PolicyGateRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = httpClientFactory.CreateClient(HttpClientName);
            http.Timeout = _timeout;
            var response = await http.PostAsJsonAsync("api/v1/policy/check", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PolicyGateResult>(ct);
            return result ?? FailClosed("null response from policy check");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[KeonRuntime] PolicyCheck fail-closed for text length {Len}", request.Text.Length);
            return FailClosed(ex.Message);
        }
    }

    public async Task<DecisionReceipt> IssueReceiptAsync(ReceiptRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = httpClientFactory.CreateClient(HttpClientName);
            http.Timeout = _timeout;
            var response = await http.PostAsJsonAsync("api/v1/receipts", request, ct);
            response.EnsureSuccessStatusCode();
            var receipt = await response.Content.ReadFromJsonAsync<DecisionReceipt>(ct);
            return receipt ?? throw new KeonRuntimeUnavailableException("Keon Runtime returned null receipt");
        }
        catch (KeonRuntimeUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[KeonRuntime] IssueReceipt failed for subject {Subject}", request.SubjectUri);
            throw new KeonRuntimeUnavailableException(
                $"Keon Runtime unavailable — cannot issue receipt for {request.SubjectUri}: {ex.Message}");
        }
    }

    public async Task<DecisionReceipt?> GetReceiptAsync(string receiptUri, CancellationToken ct = default)
    {
        try
        {
            using var http = httpClientFactory.CreateClient(HttpClientName);
            http.Timeout = _timeout;
            var encoded = Uri.EscapeDataString(receiptUri);
            var response = await http.GetAsync($"api/v1/receipts/{encoded}", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<DecisionReceipt>(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[KeonRuntime] GetReceipt failed for URI {Uri}", receiptUri);
            return null;
        }
    }

    public async Task<EvidenceGateResult> CheckEvidenceGateAsync(EvidenceGateRequest request, CancellationToken ct = default)
    {
        try
        {
            using var http = httpClientFactory.CreateClient(HttpClientName);
            http.Timeout = _timeout;
            var response = await http.PostAsJsonAsync("api/v1/evidence-gate/check", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EvidenceGateResult>(ct);
            return result ?? EvidenceFailClosed("null response");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[KeonRuntime] EvidenceGate fail-closed for compound {Slug}", request.CompoundSlug);
            return EvidenceFailClosed(ex.Message);
        }
    }

    private static PolicyGateResult FailClosed(string reason) =>
        new(PolicyDecision.Blocked, null, null, $"keon-offline: {reason}", FailedHash);

    private static EvidenceGateResult EvidenceFailClosed(string reason) =>
        new(EvidenceVisibilityTier.Blocked, $"keon-offline: {reason}", FailedHash);
}
