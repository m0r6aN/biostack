namespace BioStack.KnowledgeWorker.Config;

using Microsoft.Extensions.Configuration;
using Npgsql;

/// <summary>
/// Fails closed at startup when running in Production unless the KnowledgeWorker
/// is correctly pointed at a Postgres database reachable from this process.
///
/// Locked-in production policy (signed, non-reinterpretable):
///  * KnowledgeWorker is <b>Npgsql-only</b> in Production.
///  * The API may retain multi-provider support for local / dev convenience, but
///    in Production the API and this worker MUST point at the same Postgres
///    connection string. There is no valid production topology in which the
///    worker writes to Postgres while the API reads from SQLite (or vice-versa).
///  * The guard parses <c>ConnectionStrings:DefaultConnection</c> through
///    <see cref="NpgsqlConnectionStringBuilder"/>. A connection string that is
///    empty, unparseable, missing <c>Host</c>, or missing <c>Database</c> causes
///    startup to abort.
/// </summary>
public static class ProductionSafetyGuard
{
    /// <summary>
    /// Resolves the effective <see cref="RunMode"/> for this invocation.
    /// Precedence: explicit <c>Worker:RunMode</c> > legacy <c>Worker:SeedOnStartup</c>.
    /// Throws when neither is set in Production.
    /// </summary>
    public static RunMode ResolveRunMode(WorkerOptions options, bool isProduction)
    {
        if (options.RunMode.HasValue)
        {
            return options.RunMode.Value;
        }

        if (isProduction)
        {
            throw new InvalidOperationException(
                "Worker:RunMode must be set to 'Seed' or 'Refresh' in Production. " +
                "The worker runs one-shot under Azure Container App Jobs and does not " +
                "accept implicit defaults in Production.");
        }

        return options.SeedOnStartup ? RunMode.Seed : RunMode.Refresh;
    }

    /// <summary>
    /// Validates provider policy + connection string shape.
    /// In Production, a malformed or non-Postgres connection string throws.
    /// Returns the canonicalized Npgsql-parsed connection string.
    /// </summary>
    public static string EnforcePostgresOnly(IConfiguration config, bool isProduction)
    {
        var raw = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required. " +
                "Set it via environment variable: ConnectionStrings__DefaultConnection");
        }

        if (LooksLikeSqlite(raw))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection resolves to a SQLite data source. " +
                "The KnowledgeWorker is Npgsql-only. In Production, the API and worker " +
                "must point at the same Postgres database. SQLite is not permitted here.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not a valid Npgsql connection string. " +
                "The KnowledgeWorker is Npgsql-only.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing 'Host='. " +
                "A Postgres Host is required.");
        }

        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing 'Database='. " +
                "A Postgres database name is required.");
        }

        if (isProduction && IsLocalHost(builder.Host))
        {
            // Not fatal, but flagged. Production-environment config pointing at localhost
            // is almost always a misconfiguration and should be caught early by ops.
            Console.Error.WriteLine(
                "[ProductionSafetyGuard] WARNING: Production environment is pointed at a localhost " +
                $"Postgres ({builder.Host}). Verify the API and worker share the same durable store.");
        }

        return builder.ToString();
    }

    private static bool LooksLikeSqlite(string conn)
    {
        // SQLite connection strings are typically "Data Source=..." or contain ".db" paths.
        // Npgsql strings use "Host=" and "Database=". This is a conservative sniff.
        var lower = conn.ToLowerInvariant();
        if (lower.Contains("data source=") && !lower.Contains("host="))
        {
            return true;
        }
        if (lower.Contains(".db") && !lower.Contains("host="))
        {
            return true;
        }
        return false;
    }

    private static bool IsLocalHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal);
    }
}
