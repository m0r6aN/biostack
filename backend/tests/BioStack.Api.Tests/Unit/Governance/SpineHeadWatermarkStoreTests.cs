namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Infrastructure.Governance;
using Npgsql;
using Xunit;

/// <summary>
/// H2-R2 (AC1, Finding C): direct unit tests of
/// <see cref="SpineHeadWatermarkStore.ResolveDefaultPath"/> against realistic provider
/// <c>DataSource</c>/<c>ConnectionString</c> shapes for BOTH providers this module must support —
/// SQLite (the only shape the pre-H2-R2 test suite covered) and Postgres/Npgsql (the only
/// provider Production is allowed to run, per <c>Program.cs</c>'s Postgres-required-in-Production
/// check — and the provider the shipped H2 code was never tested against).
///
/// The Postgres-shaped assertions use a REAL <see cref="NpgsqlConnection"/> (not a hand-rolled
/// string), so this exercises Npgsql's actual <c>DataSource</c> semantics rather than an assumed
/// approximation of them — this is exactly the gap <c>h2r2_review_2</c>'s NEW-1 probe found: a
/// real <c>NpgsqlConnection.DataSource</c> is a non-path <c>tcp://host:port</c> endpoint
/// descriptor, not a filesystem path, and does not vary by database name.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SpineHeadWatermarkStoreTests
{
    // ── Postgres / Npgsql shapes (H2-R2/Finding C) ──────────────────────────────────────────

    [Fact]
    public void Postgres_two_databases_on_the_same_host_port_derive_distinct_paths()
    {
        // The exact reviewer reproduction (NEW-1): two databases differing only in `Database=`,
        // on the same host:port. Confirmed with a real NpgsqlConnection: both expose the
        // IDENTICAL DataSource ("tcp://host:port") -- the pre-H2-R2 code used DataSource alone
        // and therefore collided here.
        using var tenantA = new NpgsqlConnection(
            "Host=pg.internal;Port=5432;Database=biostack_tenant_a;Username=u;Password=p");
        using var tenantB = new NpgsqlConnection(
            "Host=pg.internal;Port=5432;Database=biostack_tenant_b;Username=u;Password=p");

        // Sanity-check the premise this test exists to guard: Npgsql's DataSource really is
        // identical across these two distinct databases (the exact shape that broke the old code).
        Assert.Equal(tenantA.DataSource, tenantB.DataSource);
        Assert.Equal("tcp://pg.internal:5432", tenantA.DataSource);

        var pathA = SpineHeadWatermarkStore.ResolveDefaultPath(
            tenantA.DataSource, tenantA.ConnectionString);
        var pathB = SpineHeadWatermarkStore.ResolveDefaultPath(
            tenantB.DataSource, tenantB.ConnectionString);

        Assert.NotEqual(pathA, pathB);
    }

    [Fact]
    public void Postgres_derived_path_is_always_rooted()
    {
        // NEW-1's second half: the pre-H2-R2 code concatenated the non-path DataSource
        // ("tcp://host:5432" + ".spine-watermark"), which Path.IsPathRooted reports as false --
        // a CWD-relative path that evaporates on redeploy.
        using var conn = new NpgsqlConnection(
            "Host=pg.internal;Port=5432;Database=biostack;Username=u;Password=p");

        var path = SpineHeadWatermarkStore.ResolveDefaultPath(conn.DataSource, conn.ConnectionString);

        Assert.True(Path.IsPathRooted(path), $"expected a rooted path, got '{path}'");
    }

    [Fact]
    public void Postgres_same_database_derives_a_stable_path_across_calls()
    {
        using var conn = new NpgsqlConnection(
            "Host=pg.internal;Port=5432;Database=biostack;Username=u;Password=p");

        var first = SpineHeadWatermarkStore.ResolveDefaultPath(conn.DataSource, conn.ConnectionString);
        var second = SpineHeadWatermarkStore.ResolveDefaultPath(conn.DataSource, conn.ConnectionString);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Postgres_different_hosts_same_database_name_derive_distinct_paths()
    {
        using var staging = new NpgsqlConnection(
            "Host=pg-staging.internal;Port=5432;Database=biostack;Username=u;Password=p");
        using var prod = new NpgsqlConnection(
            "Host=pg-prod.internal;Port=5432;Database=biostack;Username=u;Password=p");

        var stagingPath = SpineHeadWatermarkStore.ResolveDefaultPath(
            staging.DataSource, staging.ConnectionString);
        var prodPath = SpineHeadWatermarkStore.ResolveDefaultPath(
            prod.DataSource, prod.ConnectionString);

        Assert.NotEqual(stagingPath, prodPath);
    }

    [Fact]
    public void Postgres_default_path_honors_a_configured_base_directory_and_rejects_relative_ones()
    {
        using var conn = new NpgsqlConnection(
            "Host=pg.internal;Port=5432;Database=biostack;Username=u;Password=p");

        var durable = Path.Combine(Path.GetTempPath(), $"biostack-durable-{Guid.NewGuid():N}");
        var path = SpineHeadWatermarkStore.ResolveDefaultPath(
            conn.DataSource, conn.ConnectionString, durable);

        Assert.StartsWith(durable, path);
        Assert.True(Path.IsPathRooted(path));

        Assert.Throws<ArgumentException>(() =>
            SpineHeadWatermarkStore.ResolveDefaultPath(
                conn.DataSource, conn.ConnectionString, "relative/not-absolute"));
    }

    // ── SQLite shapes (file-backed and in-memory) ───────────────────────────────────────────

    [Fact]
    public void Sqlite_file_backed_two_different_database_files_derive_distinct_rooted_paths()
    {
        var dbA = Path.Combine(Path.GetTempPath(), $"spine-a-{Guid.NewGuid():N}.db");
        var dbB = Path.Combine(Path.GetTempPath(), $"spine-b-{Guid.NewGuid():N}.db");

        var pathA = SpineHeadWatermarkStore.ResolveDefaultPath(
            dbA, $"Data Source={dbA}");
        var pathB = SpineHeadWatermarkStore.ResolveDefaultPath(
            dbB, $"Data Source={dbB}");

        Assert.NotEqual(pathA, pathB);
        Assert.True(Path.IsPathRooted(pathA));
        Assert.True(Path.IsPathRooted(pathB));
    }

    [Fact]
    public void Sqlite_file_backed_default_path_is_not_the_self_describing_H2_name()
    {
        // H2-R2/Finding D: the original `{dbFile}.spine-watermark` naming is visible in the same
        // `ls` of the same directory an attacker deleting rows is already touching, with a name
        // that announces exactly what it is. This asserts the H2-R2 replacement no longer uses
        // that literal, self-describing suffix directly beside the database file.
        var db = Path.Combine(Path.GetTempPath(), $"spine-{Guid.NewGuid():N}.db");

        var path = SpineHeadWatermarkStore.ResolveDefaultPath(db, $"Data Source={db}");

        Assert.NotEqual(db + ".spine-watermark", path);
        Assert.DoesNotContain("spine-watermark", Path.GetFileName(path), StringComparison.Ordinal);
        Assert.NotEqual(Path.GetDirectoryName(db), Path.GetDirectoryName(path));
    }

    [Fact]
    public void Sqlite_in_memory_connection_strings_derive_distinct_rooted_paths_per_data_source()
    {
        var csA = $"Data Source=file:spine-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var csB = $"Data Source=file:spine-{Guid.NewGuid():N}?mode=memory&cache=shared";

        var pathA = SpineHeadWatermarkStore.ResolveDefaultPath(csA, csA);
        var pathB = SpineHeadWatermarkStore.ResolveDefaultPath(csB, csB);

        Assert.NotEqual(pathA, pathB);
        Assert.True(Path.IsPathRooted(pathA));
        Assert.True(Path.IsPathRooted(pathB));
    }

    [Fact]
    public void Null_data_source_falls_back_to_the_hashed_connection_string_branch()
    {
        var path = SpineHeadWatermarkStore.ResolveDefaultPath(
            dataSource: null, connectionString: "Data Source=:memory:");

        Assert.True(Path.IsPathRooted(path));
    }
}
