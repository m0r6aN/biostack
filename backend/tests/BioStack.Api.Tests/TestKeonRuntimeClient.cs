namespace BioStack.Api.Tests;

using BioStack.Infrastructure.Keon;
using BioStack.Infrastructure.Governance;
using BioStack.Domain.Governance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Test-assembly-only Keon client for integration tests that verify successful
/// receipt-producing flows without claiming Keon receipt authority.
/// </summary>
internal sealed class TestKeonRuntimeClient : IKeonRuntimeClient
{
    private static readonly PolicyHash TestPolicyHash = new("test-policy", "test");

    public Task<KeonHealthStatus> CheckHealthAsync(CancellationToken ct = default)
        => Task.FromResult(new KeonHealthStatus(
            false,
            KeonRuntimeMode.Offline,
            "Test-only Keon client; no live Runtime is configured."));

    public Task<PolicyGateResult> PolicyCheckAsync(
        PolicyGateRequest request,
        CancellationToken ct = default)
        => Task.FromResult(new PolicyGateResult(
            PolicyDecision.Allowed,
            null,
            null,
            null,
            TestPolicyHash));

    public Task<DecisionReceipt> IssueReceiptAsync(
        ReceiptRequest request,
        CancellationToken ct = default)
        => Task.FromResult(new DecisionReceipt(
            ReceiptUri: $"urn:biostack:test-receipt:{Guid.NewGuid():N}",
            SubjectUri: request.SubjectUri,
            TenantId: request.TenantId,
            ActorId: request.ActorId,
            TimestampUtc: DateTime.UtcNow,
            Decision: request.Decision,
            PolicyHash: TestPolicyHash,
            InputHash: request.InputHash,
            EvidenceRefs: request.EvidenceRefs,
            EffectStatus: request.EffectStatus,
            ReceiptClass: request.ReceiptClass));

    public Task<DecisionReceipt?> GetReceiptAsync(
        string receiptUri,
        CancellationToken ct = default)
        => Task.FromResult<DecisionReceipt?>(null);

    public Task<EvidenceGateResult> CheckEvidenceGateAsync(
        EvidenceGateRequest request,
        CancellationToken ct = default)
        => Task.FromResult(new EvidenceGateResult(
            EvidenceVisibilityTier.UserFacing,
            null,
            TestPolicyHash));
}

internal static class TestKeonRuntimeClientServiceCollectionExtensions
{
    public static IServiceCollection UseTestKeonRuntimeClient(this IServiceCollection services)
    {
        services.RemoveAll<IKeonRuntimeClient>();
        services.AddSingleton<IKeonRuntimeClient, TestKeonRuntimeClient>();
        return services;
    }

    public static IServiceCollection UseThrowingTestKeonRuntimeClient(
        this IServiceCollection services,
        int failOnReceiptAttempt = 1)
    {
        services.RemoveAll<IKeonRuntimeClient>();
        services.AddSingleton<IKeonRuntimeClient>(
            new ThrowingTestKeonRuntimeClient(failOnReceiptAttempt));
        return services;
    }

    public static IServiceCollection UseThrowingTestSpineRepository(this IServiceCollection services)
    {
        services.RemoveAll<ISpineRepository>();
        services.AddScoped<ISpineRepository, ThrowingTestSpineRepository>();
        return services;
    }
}

internal sealed class ThrowingTestKeonRuntimeClient(int failOnReceiptAttempt) : IKeonRuntimeClient
{
    private readonly TestKeonRuntimeClient _inner = new();
    private int _receiptAttempts;

    public Task<KeonHealthStatus> CheckHealthAsync(CancellationToken ct = default)
        => _inner.CheckHealthAsync(ct);

    public Task<PolicyGateResult> PolicyCheckAsync(
        PolicyGateRequest request,
        CancellationToken ct = default)
        => _inner.PolicyCheckAsync(request, ct);

    public Task<DecisionReceipt> IssueReceiptAsync(
        ReceiptRequest request,
        CancellationToken ct = default)
    {
        if (Interlocked.Increment(ref _receiptAttempts) == failOnReceiptAttempt)
        {
            return Task.FromException<DecisionReceipt>(
                new KeonRuntimeUnavailableException("Test-only receipt issuance failure."));
        }

        return _inner.IssueReceiptAsync(request, ct);
    }

    public Task<DecisionReceipt?> GetReceiptAsync(
        string receiptUri,
        CancellationToken ct = default)
        => _inner.GetReceiptAsync(receiptUri, ct);

    public Task<EvidenceGateResult> CheckEvidenceGateAsync(
        EvidenceGateRequest request,
        CancellationToken ct = default)
        => _inner.CheckEvidenceGateAsync(request, ct);
}

internal sealed class ThrowingTestSpineRepository : ISpineRepository
{
    public Task<SpineEntry> AppendAsync(SpineEntry entry, CancellationToken ct = default)
        => Task.FromException<SpineEntry>(
            new InvalidOperationException("Test-only Spine append failure."));

    public Task<SpineEntry?> GetByReceiptUriAsync(string receiptUri, CancellationToken ct = default)
        => Task.FromResult<SpineEntry?>(null);

    public Task<IReadOnlyList<SpineEntry>> GetBySubjectAsync(
        string subjectUri,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SpineEntry>>([]);

    public Task<IReadOnlyList<SpineEntry>> GetByActorAsync(
        string actorId,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SpineEntry>>([]);
}
