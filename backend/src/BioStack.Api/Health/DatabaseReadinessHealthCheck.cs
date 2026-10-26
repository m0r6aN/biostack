namespace BioStack.Api.Health;

using BioStack.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Readiness (ledger gate G3): the API is ready only when it can reach its database. Used by
/// <see cref="HealthEndpoints.ReadinessPath"/> and the Container Apps readiness probe, so a
/// replica that loses the database stops receiving traffic instead of serving 500s. The
/// response carries no exception text or connection detail.
/// </summary>
public sealed class DatabaseReadinessHealthCheck(BioStackDbContext db) : IHealthCheck
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            return await db.Database.CanConnectAsync(timeout.Token)
                ? HealthCheckResult.Healthy("database reachable")
                : HealthCheckResult.Unhealthy("database unreachable");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("database unreachable");
        }
    }
}

public static class HealthEndpoints
{
    /// <summary>Tag for checks that gate readiness. Liveness (<c>/health</c>) runs none, so a database outage never restarts replicas.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Readiness: the API can serve requests (database reachable).</summary>
    public const string ReadinessPath = "/health/ready";
}
