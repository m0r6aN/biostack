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

    // ── H4a: Connection String Identity Canonicalization ────────────────────────────────────

    [Fact]
    public void H4a_Postgres_connection_strings_with_different_whitespace_derive_same_path()
    {
        // The reviewer's probe: equivalent connection strings differing only in whitespace
        var compactCs = "Host=pg.internal;Port=5432;Database=biostack;Username=u;Password=p";
        var spacedCs = "Host = pg.internal ; Port = 5432 ; Database = biostack ; Username = u ; Password = p";

        using var compact = new NpgsqlConnection(compactCs);
        using var spaced = new NpgsqlConnection(spacedCs);

        var pathCompact = SpineHeadWatermarkStore.ResolveDefaultPath(compact.DataSource, compactCs);
        var pathSpaced = SpineHeadWatermarkStore.ResolveDefaultPath(spaced.DataSource, spacedCs);

        Assert.Equal(pathCompact, pathSpaced);
    }

    [Fact]
    public void H4a_Postgres_connection_strings_with_different_parameter_order_derive_same_path()
    {
        var orderA = "Host=pg.internal;Port=5432;Database=biostack;Username=u;Password=p";
        var orderB = "Database=biostack;Port=5432;Host=pg.internal;Password=p;Username=u";

        using var connA = new NpgsqlConnection(orderA);
        using var connB = new NpgsqlConnection(orderB);

        var pathA = SpineHeadWatermarkStore.ResolveDefaultPath(connA.DataSource, orderA);
        var pathB = SpineHeadWatermarkStore.ResolveDefaultPath(connB.DataSource, orderB);

        Assert.Equal(pathA, pathB);
    }

    [Fact]
    public void H4a_Postgres_connection_strings_with_different_case_derive_same_path()
    {
        var lowerCs = "host=pg.internal;port=5432;database=biostack;username=u;password=p";
        var mixedCs = "Host=pg.internal;Port=5432;Database=biostack;Username=u;Password=p";

        using var lower = new NpgsqlConnection(lowerCs);
        using var mixed = new NpgsqlConnection(mixedCs);

        var pathLower = SpineHeadWatermarkStore.ResolveDefaultPath(lower.DataSource, lowerCs);
        var pathMixed = SpineHeadWatermarkStore.ResolveDefaultPath(mixed.DataSource, mixedCs);

        Assert.Equal(pathLower, pathMixed);
    }

    [Fact]
    public void H4a_Postgres_genuinely_different_databases_still_derive_different_paths()
    {
        // Canonicalization must NOT collapse genuinely different databases
        var csA = "Host=pg.internal;Port=5432;Database=biostack_a;Username=u;Password=p";
        var csB = "Host=pg.internal;Port=5432;Database=biostack_b;Username=u;Password=p";

        using var connA = new NpgsqlConnection(csA);
        using var connB = new NpgsqlConnection(csB);

        var pathA = SpineHeadWatermarkStore.ResolveDefaultPath(connA.DataSource, csA);
        var pathB = SpineHeadWatermarkStore.ResolveDefaultPath(connB.DataSource, csB);

        Assert.NotEqual(pathA, pathB);
    }

    [Fact]
    public void H4a_Postgres_credentials_do_not_affect_identity()
    {
        // Different usernames/passwords for the same database should derive the same path
        // (identity is about the database, not the accessor)
        var csUserA = "Host=pg.internal;Port=5432;Database=biostack;Username=alice;Password=pass1";
        var csUserB = "Host=pg.internal;Port=5432;Database=biostack;Username=bob;Password=pass2";

        using var connA = new NpgsqlConnection(csUserA);
        using var connB = new NpgsqlConnection(csUserB);

        var pathA = SpineHeadWatermarkStore.ResolveDefaultPath(connA.DataSource, csUserA);
        var pathB = SpineHeadWatermarkStore.ResolveDefaultPath(connB.DataSource, csUserB);

        Assert.Equal(pathA, pathB);
    }

    [Fact]
    public void H4a_Sqlite_connection_strings_with_different_whitespace_derive_same_path()
    {
        var compactCs = "Data Source=/tmp/test.db;Mode=ReadWriteCreate";
        var spacedCs = "Data Source = /tmp/test.db ; Mode = ReadWriteCreate";

        var pathCompact = SpineHeadWatermarkStore.ResolveDefaultPath(compactCs, compactCs);
        var pathSpaced = SpineHeadWatermarkStore.ResolveDefaultPath(spacedCs, spacedCs);

        Assert.Equal(pathCompact, pathSpaced);
    }

    // ── H4b: Symlink/TOCTOU Hardening ───────────────────────────────────────────────────────

    [Fact]
    public void H4b_TryRead_rejects_symlink_watermark_file()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"h4b-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var realFile = Path.Combine(tempDir, "real.txt");
            var symlinkFile = Path.Combine(tempDir, "symlink.watermark");
            File.WriteAllText(realFile, "100|abcdef1234567890");

            // Create a symlink pointing to the real file
            if (OperatingSystem.IsWindows())
            {
                // Windows requires admin or developer mode for symlinks; skip if unavailable
                try
                {
                    File.CreateSymbolicLink(symlinkFile, realFile);
                }
                catch (UnauthorizedAccessException)
                {
                    return; // Skip test on Windows without symlink permission
                }
            }
            else
            {
                File.CreateSymbolicLink(symlinkFile, realFile);
            }

            // TryRead should reject the symlink even though it points to valid content
            var result = SpineHeadWatermarkStore.TryRead(symlinkFile);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void H4b_Advance_rejects_symlink_watermark_file()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"h4b-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var realFile = Path.Combine(tempDir, "real.txt");
            var symlinkFile = Path.Combine(tempDir, "symlink.watermark");
            File.WriteAllText(realFile, "50|initialHash");

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    File.CreateSymbolicLink(symlinkFile, realFile);
                }
                catch (UnauthorizedAccessException)
                {
                    return; // Skip on Windows without symlink permission
                }
            }
            else
            {
                File.CreateSymbolicLink(symlinkFile, realFile);
            }

            // Advance should silently refuse to write through the symlink
            SpineHeadWatermarkStore.Advance(symlinkFile, 100, "newHash");

            // The real file should NOT have been updated
            var realContent = File.ReadAllText(realFile);
            Assert.Equal("50|initialHash", realContent);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void H4b_Advance_rejects_symlink_governance_directory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"h4b-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var realDir = Path.Combine(tempDir, "real-governance");
            var symlinkDir = Path.Combine(tempDir, ".biostack-governance");
            Directory.CreateDirectory(realDir);

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    Directory.CreateSymbolicLink(symlinkDir, realDir);
                }
                catch (UnauthorizedAccessException)
                {
                    return; // Skip on Windows without symlink permission
                }
            }
            else
            {
                Directory.CreateSymbolicLink(symlinkDir, realDir);
            }

            var watermarkPath = Path.Combine(symlinkDir, "test.watermark");

            // Advance should silently refuse when the directory is a symlink
            SpineHeadWatermarkStore.Advance(watermarkPath, 100, "testHash");

            // No file should have been created in the real directory
            var realWatermarkPath = Path.Combine(realDir, "test.watermark");
            Assert.False(File.Exists(realWatermarkPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void H4b_normal_watermark_operations_still_work()
    {
        // Ensure the symlink hardening doesn't break legitimate usage
        var tempDir = Path.Combine(Path.GetTempPath(), $"h4b-test-{Guid.NewGuid():N}");
        try
        {
            var watermarkPath = Path.Combine(tempDir, ".biostack-governance", "test.watermark");

            // Normal advance should succeed
            SpineHeadWatermarkStore.Advance(watermarkPath, 100, "hash100");
            var read1 = SpineHeadWatermarkStore.TryRead(watermarkPath);
            Assert.NotNull(read1);
            Assert.Equal(100, read1.Value.SequenceNumber);
            Assert.Equal("hash100", read1.Value.EntryHash);

            // Advancing forward should succeed
            SpineHeadWatermarkStore.Advance(watermarkPath, 200, "hash200");
            var read2 = SpineHeadWatermarkStore.TryRead(watermarkPath);
            Assert.NotNull(read2);
            Assert.Equal(200, read2.Value.SequenceNumber);
            Assert.Equal("hash200", read2.Value.EntryHash);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
