namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Config;
using Microsoft.Extensions.Configuration;
using Xunit;

public class ProductionSafetyGuardTests
{
    // ── EnforcePostgresOnly ─────────────────────────────────────────────────────

    [Fact]
    public void EnforcePostgresOnly_Throws_When_ConnectionString_Is_Missing()
    {
        var cfg = BuildConfig(conn: null);

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProductionSafetyGuard.EnforcePostgresOnly(cfg, isProduction: true));

        Assert.Contains("DefaultConnection", ex.Message);
    }

    [Fact]
    public void EnforcePostgresOnly_Throws_When_Connection_Is_Sqlite()
    {
        var cfg = BuildConfig(conn: "Data Source=biostack.db");

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProductionSafetyGuard.EnforcePostgresOnly(cfg, isProduction: true));

        Assert.Contains("SQLite", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnforcePostgresOnly_Throws_When_Host_Is_Missing()
    {
        var cfg = BuildConfig(conn: "Database=biostack;Username=app;Password=x");

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProductionSafetyGuard.EnforcePostgresOnly(cfg, isProduction: false));

        Assert.Contains("Host", ex.Message);
    }

    [Fact]
    public void EnforcePostgresOnly_Throws_When_Database_Is_Missing()
    {
        var cfg = BuildConfig(conn: "Host=db.internal;Username=app;Password=x");

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProductionSafetyGuard.EnforcePostgresOnly(cfg, isProduction: false));

        Assert.Contains("Database", ex.Message);
    }

    [Fact]
    public void EnforcePostgresOnly_Returns_Canonicalized_String_For_Valid_Postgres_Connection()
    {
        var cfg = BuildConfig(conn: "Host=db.internal;Database=biostack;Username=app;Password=secret");

        var result = ProductionSafetyGuard.EnforcePostgresOnly(cfg, isProduction: true);

        Assert.Contains("Host=db.internal", result);
        Assert.Contains("Database=biostack", result);
    }

    // ── ResolveRunMode ──────────────────────────────────────────────────────────

    [Fact]
    public void ResolveRunMode_Returns_Explicit_RunMode_When_Set()
    {
        var opts = new WorkerOptions { RunMode = RunMode.Refresh };

        Assert.Equal(RunMode.Refresh, ProductionSafetyGuard.ResolveRunMode(opts, isProduction: true));
    }

    [Fact]
    public void ResolveRunMode_Throws_In_Production_When_RunMode_Unset()
    {
        var opts = new WorkerOptions { RunMode = null };

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProductionSafetyGuard.ResolveRunMode(opts, isProduction: true));

        Assert.Contains("Worker:RunMode", ex.Message);
    }

    [Fact]
    public void ResolveRunMode_Falls_Back_To_Seed_In_Non_Production_When_SeedOnStartup_True()
    {
        var opts = new WorkerOptions { RunMode = null, SeedOnStartup = true };

        Assert.Equal(RunMode.Seed, ProductionSafetyGuard.ResolveRunMode(opts, isProduction: false));
    }

    [Fact]
    public void ResolveRunMode_Falls_Back_To_Refresh_In_Non_Production_When_SeedOnStartup_False()
    {
        var opts = new WorkerOptions { RunMode = null, SeedOnStartup = false };

        Assert.Equal(RunMode.Refresh, ProductionSafetyGuard.ResolveRunMode(opts, isProduction: false));
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(string? conn)
    {
        var dict = new Dictionary<string, string?>();
        if (conn is not null)
        {
            dict["ConnectionStrings:DefaultConnection"] = conn;
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }
}
