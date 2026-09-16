namespace BioStack.KnowledgeWorker.Config;

using BioStack.KnowledgeWorker.Pipeline;
using Npgsql;

/// <summary>
/// Startup-time construction of the <see cref="IPromotionGate"/> used by
/// <see cref="RunMode.Refresh"/>. Called from <c>Program.cs</c> inside
/// <c>ConfigureServices</c> — i.e. during <c>host.Build()</c>, strictly before the
/// post-build Postgres connectivity check, schema bootstrap, and interaction-hint
/// seeding. A thrown exception here therefore aborts the whole process before any
/// connection to, or write against, the database is attempted — the "fail closed" the
/// promotion gate is required to provide.
/// </summary>
public static class RefreshPromotionGateStartup
{
    public static IPromotionGate ValidateAndLoad(WorkerOptions options, string connectionString, string environmentName = "Production")
    {
        if (options.AllowUnpromoted)
        {
            ValidateOverrideSafety(connectionString, environmentName);

            Console.Error.WriteLine(
                "[RefreshPromotionGateStartup] WARNING: Worker:AllowUnpromoted=true — the promotion "
                + "gate is DISABLED for this Refresh run. Every schema-valid seed record will be "
                + "upserted regardless of review-decision status. This override is for dev/local use "
                + "only and must never be set for a production Refresh.");

            return AllowAllPromotionGate.Instance;
        }

        IResearchArtifactValidator validator;
        try
        {
            var schemaDir = Path.Combine(AppContext.BaseDirectory, "Schemas");
            validator = ResearchArtifactValidator.LoadFromDirectory(schemaDir);
        }
        catch (Exception ex)
        {
            throw new PromotionGateLoadException(
                $"Refresh's promotion gate could not load the review-decision schema: {ex.Message}", ex);
        }

        var index = PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, validator);
        return new ReviewDecisionPromotionGate(index);
    }

    /// <summary>Only Development against a loopback database may bypass the gate.</summary>
    private static void ValidateOverrideSafety(string connectionString, string environmentName)
    {
        if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Worker:AllowUnpromoted=true requires the Development hosting environment.");

        string host;
        try
        {
            host = new NpgsqlConnectionStringBuilder(connectionString).Host ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Worker:AllowUnpromoted=true is refused: the connection string host could not be "
                + $"determined to confirm it is local ({ex.Message}).", ex);
        }

        if (!ProductionSafetyGuard.IsLocalHost(host))
        {
            throw new InvalidOperationException(
                $"Worker:AllowUnpromoted=true is refused: the connection string host '{host}' is not "
                + "a loopback host. This override is restricted to local Development databases.");
        }
    }
}
