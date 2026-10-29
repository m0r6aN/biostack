namespace BioStack.Api.Tests.Postgres;

using System.Reflection;
using BioStack.Api;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Xunit;

/// <summary>
/// Applies the whole migration chain to an EMPTY Postgres and compares the resulting schema to the one the
/// model produces. EF's own pending-model-changes check is not usable here: the model snapshot is intentionally
/// minimal (see <see cref="ProductionMigrationBaselineConfiguration"/>), so the comparison is done on the live schema.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PostgresMigrationChainTests
{
    /// <summary>
    /// Tables that exist in the model but that NO migration creates. Remove an entry when a migration adds the table;
    /// the test fails if an entry is stale, and fails if a new table gets this problem.
    /// </summary>
    private static readonly string[] TablesMissingFromMigrationChain = ["KnowledgeSourceIntakeRequests"];

    /// <summary>
    /// Tables whose migrated column storage types still differ from the model (their migrations were scaffolded
    /// against SQLite). Existing databases already have these migrations applied, so they are recorded here rather
    /// than rewritten. The test fails if a table drifts that is not listed, and if a listed table no longer drifts.
    /// </summary>
    private static readonly string[] TablesWithKnownColumnTypeDrift =
    [
        "SpineChainCheckpoints", "SpineEntries", "StagedTranscriptCandidateReviews",
    ];

    /// <summary>Indexes the migrations create (raw SQL) that the model does not describe.</summary>
    private static readonly string[] IndexesOnlyInMigrations =
    [
        "IX_CompoundInteractionHints_CompoundA_CompoundB", "UX_KnowledgeEntries_CanonicalName_Lower",
    ];

    private readonly PostgresServerFixture _server;

    public PostgresMigrationChainTests(PostgresServerFixture server) => _server = server;

    [PostgresFact]
    public async Task FullMigrationChain_AppliesToAnEmptyDatabase()
    {
        var connectionString = await _server.CreateDatabaseAsync();
        try
        {
            await using var db = CreateContext(connectionString);
            await db.Database.MigrateAsync();

            var expected = db.Database.GetMigrations().ToArray();
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.Equal(expected, applied);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Contains("20260828000000_AddProfileGoals", expected);
            Assert.Contains("20260422125251_RecoverBillingTierEnforcement", expected);

            // A second run is a no-op.
            await db.Database.MigrateAsync();
            Assert.Equal(expected, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        }
        finally
        {
            await _server.DropDatabaseAsync(connectionString);
        }
    }

    [PostgresFact]
    public async Task FullMigrationChain_CreatesUuidKeysSoProfileGoalsCanReferencePersonProfiles()
    {
        var connectionString = await _server.CreateDatabaseAsync();
        try
        {
            await using var db = CreateContext(connectionString);
            await db.Database.MigrateAsync();

            var schema = await SchemaSnapshot.ReadAsync(connectionString);
            Assert.Contains("uuid", schema.ColumnTypes["PersonProfiles.Id"]);
            Assert.Contains("uuid", schema.ColumnTypes["ProfileGoals.ProfileId"]);
            Assert.Contains(schema.ForeignKeys, fk => fk.Contains("FK_ProfileGoals_PersonProfiles_ProfileId", StringComparison.Ordinal));
        }
        finally
        {
            await _server.DropDatabaseAsync(connectionString);
        }
    }

    [PostgresFact]
    public async Task MigratedSchema_MatchesTheModel()
    {
        var migratedConnection = await _server.CreateDatabaseAsync();
        var modelConnection = await _server.CreateDatabaseAsync();
        try
        {
            await using (var migrated = CreateContext(migratedConnection))
            {
                await migrated.Database.MigrateAsync();
            }

            await using (var model = CreateContext(modelConnection))
            {
                await model.Database.EnsureCreatedAsync();
            }

            var fromMigrations = await SchemaSnapshot.ReadAsync(migratedConnection);
            var fromModel = await SchemaSnapshot.ReadAsync(modelConnection);

            // Tables.
            var missingTables = fromModel.Tables.Except(fromMigrations.Tables).OrderBy(x => x).ToArray();
            Assert.Equal(TablesMissingFromMigrationChain.OrderBy(x => x), missingTables);
            Assert.Empty(fromMigrations.Tables.Except(fromModel.Tables));

            var comparableTables = fromModel.Tables.Intersect(fromMigrations.Tables).ToHashSet();

            // Columns: same set and same nullability.
            Assert.Empty(Diff("columns", fromModel.Columns, fromMigrations.Columns, comparableTables));

            // Foreign keys and indexes: same names (Postgres truncates long names, so compare a safe prefix).
            Assert.Empty(Diff("foreign keys", fromModel.ForeignKeys, fromMigrations.ForeignKeys, comparableTables, ignore: []));
            Assert.Empty(Diff("indexes", fromModel.Indexes, fromMigrations.Indexes, comparableTables, ignore: IndexesOnlyInMigrations));
            Assert.Empty(Diff("check constraints", fromModel.CheckConstraints, fromMigrations.CheckConstraints, comparableTables));

            // Column storage types (text and varchar(n) are the same kind: the model's max lengths are not enforced by older migrations).
            var drift = fromModel.ColumnTypes
                .Where(pair => fromMigrations.ColumnTypes.TryGetValue(pair.Key, out var migratedType) && migratedType != pair.Value)
                .Select(pair => pair.Key.Split('.')[0])
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            Assert.Equal(TablesWithKnownColumnTypeDrift.OrderBy(x => x), drift);
        }
        finally
        {
            await _server.DropDatabaseAsync(migratedConnection);
            await _server.DropDatabaseAsync(modelConnection);
        }
    }

    [Fact]
    public void BaselineMigration_KeepsItsScaffoldedSqliteTypesAndUsesNativeTypesOnPostgres()
    {
        var sqlite = BuildOperations<RecoverBillingTierEnforcement>("Microsoft.EntityFrameworkCore.Sqlite");
        var sqliteColumns = sqlite.OfType<CreateTableOperation>().SelectMany(t => t.Columns).ToArray();
        Assert.NotEmpty(sqliteColumns);
        Assert.All(sqliteColumns, c => Assert.True(c.ColumnType is "TEXT" or "INTEGER", $"{c.Table}.{c.Name} was {c.ColumnType}"));

        var postgres = BuildOperations<RecoverBillingTierEnforcement>("Npgsql.EntityFrameworkCore.PostgreSQL");
        var postgresColumns = postgres.OfType<CreateTableOperation>().SelectMany(t => t.Columns).ToArray();
        Assert.Equal(sqliteColumns.Length, postgresColumns.Length);
        Assert.All(postgresColumns, c => Assert.DoesNotContain(c.ColumnType, new[] { "TEXT", "INTEGER" }));

        string TypeOf(string table, string column) =>
            postgresColumns.Single(c => c.Table == table && c.Name == column).ColumnType!;
        Assert.Equal("uuid", TypeOf("PersonProfiles", "Id"));
        Assert.Equal("uuid", TypeOf("PersonProfiles", "OwnerId"));
        Assert.Equal("timestamp with time zone", TypeOf("AppUsers", "CreatedAtUtc"));
        Assert.Equal("character varying(255)", TypeOf("AppUsers", "Email"));
        Assert.StartsWith("numeric(", TypeOf("CompoundInteractionHints", "Strength"));

        // Tables, indexes and keys are identical on both providers; only column storage types differ.
        Assert.Equal(sqlite.Count, postgres.Count);
        Assert.Equal(
            sqlite.Select(o => o.GetType().Name),
            postgres.Select(o => o.GetType().Name));
    }

    private static IReadOnlyList<string> Diff(
        string kind,
        IReadOnlySet<string> expected,
        IReadOnlySet<string> actual,
        IReadOnlySet<string> comparableTables,
        IReadOnlyCollection<string>? ignore = null)
    {
        ignore ??= [];
        bool Relevant(string item)
        {
            var table = item.Split(' ')[0].Split('.')[0];
            return comparableTables.Contains(table) && !ignore.Any(item.Contains);
        }

        return expected.Where(Relevant).Except(actual).Select(x => $"missing from migrations ({kind}): {x}")
            .Concat(actual.Where(Relevant).Except(expected).Select(x => $"only in migrations ({kind}): {x}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static BioStackDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseNpgsql(connectionString);
        ProductionMigrationBaselineConfiguration.Configure(options);
        return new BioStackDbContext(options.Options);
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations<TMigration>(string provider)
        where TMigration : Migration, new()
    {
        var builder = new MigrationBuilder(provider);
        var up = typeof(TMigration).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);
        up.Invoke(new TMigration(), [builder]);
        return builder.Operations;
    }

    private sealed record SchemaSnapshot(
        IReadOnlySet<string> Tables,
        IReadOnlySet<string> Columns,
        IReadOnlyDictionary<string, string> ColumnTypes,
        IReadOnlySet<string> ForeignKeys,
        IReadOnlySet<string> Indexes,
        IReadOnlySet<string> CheckConstraints)
    {
        public static async Task<SchemaSnapshot> ReadAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var tables = await Strings(connection,
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name <> '__EFMigrationsHistory'");

            var columns = new HashSet<string>(StringComparer.Ordinal);
            var types = new Dictionary<string, string>(StringComparer.Ordinal);
            await using (var command = new NpgsqlCommand(
                "SELECT table_name, column_name, udt_name, is_nullable, numeric_precision, numeric_scale FROM information_schema.columns WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'",
                connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var key = $"{reader.GetString(0)}.{reader.GetString(1)}";
                    columns.Add($"{key} nullable={reader.GetString(3)}");
                    var udt = reader.GetString(2);
                    types[key] = udt is "text" or "varchar" ? "string" : udt;
                }
            }

            // Postgres truncates identifiers to 63 bytes, and EF truncates differently, so compare a stable prefix.
            string Name(string value) => value.Length > 55 ? value[..55] : value;
            var foreignKeys = (await Strings(connection,
                "SELECT conrelid::regclass::text || ' ' || conname FROM pg_constraint WHERE contype = 'f' AND connamespace = 'public'::regnamespace"))
                .Select(x => Strip(x)).Select(Name).ToHashSet(StringComparer.Ordinal);
            var checks = (await Strings(connection,
                "SELECT conrelid::regclass::text || ' ' || conname FROM pg_constraint WHERE contype = 'c' AND connamespace = 'public'::regnamespace"))
                .Select(x => Strip(x)).ToHashSet(StringComparer.Ordinal);
            var indexes = await Strings(connection,
                "SELECT tablename || ' ' || indexname FROM pg_indexes WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'");

            return new SchemaSnapshot(tables, columns, types, foreignKeys, indexes, checks);

            static string Strip(string value) => value.Replace("\"", string.Empty, StringComparison.Ordinal);
        }

        private static async Task<HashSet<string>> Strings(NpgsqlConnection connection, string sql)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }
    }
}
