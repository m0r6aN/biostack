namespace BioStack.Api.Tests;

using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

public sealed class CriticalPostgresSchemaContractTests
{
    [Fact]
    public void FindProblems_AcceptsCompleteProviderNativeSchema()
    {
        var actual = BuildSchema(useLegacyTypes: false);

        Assert.Empty(CriticalPostgresSchemaContract.FindProblems(actual));
    }

    [Fact]
    public void FindProblems_ReportsTypeNullabilityAndMissingDriftPrecisely()
    {
        var actual = BuildSchema(useLegacyTypes: false);
        actual[("AppUsers", "CreatedAtUtc")] = new PostgresColumnSchema("text", false);
        actual[("AuthIdentities", "IsVerified")] = new PostgresColumnSchema("bool", true);
        actual.Remove(("Subscriptions", "AppUserId"));

        var problems = CriticalPostgresSchemaContract.FindProblems(actual);

        Assert.Contains(
            problems,
            problem => problem.StartsWith("type:AppUsers.CreatedAtUtc=text", StringComparison.Ordinal));
        Assert.Contains(
            "nullability:AuthIdentities.IsVerified=nullable;expected=not-null",
            problems);
        Assert.Contains("column:Subscriptions.AppUserId", problems);
    }

    [Fact]
    public void LegacyBaselineMode_AcceptsOnlyKnownRepairableProviderDrift()
    {
        var actual = BuildSchema(useLegacyTypes: true, legacyBaselineOnly: true);
        Assert.Empty(CriticalPostgresSchemaContract.FindProblems(actual, legacyBaselineMode: true));

        actual[("AppUsers", "Id")] = new PostgresColumnSchema("int4", false);
        var problems = CriticalPostgresSchemaContract.FindProblems(actual, legacyBaselineMode: true);

        Assert.Contains(
            problems,
            problem => problem.StartsWith("type:AppUsers.Id=int4", StringComparison.Ordinal));
    }

    [Fact]
    public void Contract_IncludesPasskeyCredentialsWithMigrationColumnTypes()
    {
        var expected = new Dictionary<string, (IReadOnlySet<string> UdtNames, bool IsNullable)>(StringComparer.Ordinal)
        {
            ["Id"] = (new HashSet<string> { "uuid" }, false),
            ["IdentityId"] = (new HashSet<string> { "uuid" }, false),
            ["CredentialId"] = (new HashSet<string> { "bytea" }, false),
            ["PublicKey"] = (new HashSet<string> { "bytea" }, false),
            ["UserHandle"] = (new HashSet<string> { "bytea" }, false),
            ["CredentialType"] = (new HashSet<string> { "text", "varchar" }, false),
            ["SignatureCounter"] = (new HashSet<string> { "int8" }, false),
            ["Transports"] = (new HashSet<string> { "text", "varchar" }, false),
            ["AaGuid"] = (new HashSet<string> { "uuid" }, false),
            ["IsBackupEligible"] = (new HashSet<string> { "bool" }, false),
            ["IsBackedUp"] = (new HashSet<string> { "bool" }, false),
            ["DisplayName"] = (new HashSet<string> { "text", "varchar" }, false),
            ["CreatedAtUtc"] = (new HashSet<string> { "timestamptz" }, false),
            ["LastUsedAtUtc"] = (new HashSet<string> { "timestamptz" }, true),
        };

        AssertTableMatchesContract("PasskeyCredentials", expected);
    }

    [Fact]
    public void Contract_IncludesPasskeyOperationChallengesWithMigrationColumnTypes()
    {
        var expected = new Dictionary<string, (IReadOnlySet<string> UdtNames, bool IsNullable)>(StringComparer.Ordinal)
        {
            ["Id"] = (new HashSet<string> { "uuid" }, false),
            ["UserId"] = (new HashSet<string> { "uuid" }, true),
            ["Operation"] = (new HashSet<string> { "text", "varchar" }, false),
            ["RequestIdHash"] = (new HashSet<string> { "text", "varchar" }, false),
            ["OptionsJson"] = (new HashSet<string> { "text", "varchar" }, false),
            ["RedirectPath"] = (new HashSet<string> { "text", "varchar" }, false),
            ["CreatedAtUtc"] = (new HashSet<string> { "timestamptz" }, false),
            ["ExpiresAtUtc"] = (new HashSet<string> { "timestamptz" }, false),
            ["ConsumedAtUtc"] = (new HashSet<string> { "timestamptz" }, true),
            ["AttemptCount"] = (new HashSet<string> { "int4" }, false),
            ["IpAddress"] = (new HashSet<string> { "text", "varchar" }, true),
        };

        AssertTableMatchesContract("PasskeyOperationChallenges", expected);
    }

    private static void AssertTableMatchesContract(
        string table,
        IReadOnlyDictionary<string, (IReadOnlySet<string> UdtNames, bool IsNullable)> expectedColumns)
    {
        var actualColumns = CriticalPostgresSchemaContract.Columns
            .Where(column => column.Table == table)
            .ToDictionary(column => column.Column, StringComparer.Ordinal);

        Assert.Equal(expectedColumns.Keys.OrderBy(k => k, StringComparer.Ordinal), actualColumns.Keys.OrderBy(k => k, StringComparer.Ordinal));

        foreach (var (columnName, expected) in expectedColumns)
        {
            var actual = actualColumns[columnName];
            Assert.False(actual.IsLegacyBaselineColumn, $"{table}.{columnName} was added by the passkey migration and must not be a legacy-baseline column.");
            Assert.Equal(expected.IsNullable, actual.IsNullable);
            Assert.True(
                expected.UdtNames.SetEquals(actual.ExpectedUdtNames),
                $"{table}.{columnName} expected udt names [{string.Join(',', expected.UdtNames)}] but contract has [{string.Join(',', actual.ExpectedUdtNames)}]");
        }
    }

    [Fact]
    public void Contract_MatchesCurrentNpgsqlModelMappings()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_model_contract")
            .Options;
        using var context = new BioStackDbContext(options);
        var actual = new Dictionary<(string Table, string Column), PostgresColumnSchema>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            var table = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(table);
                if (columnName is null)
                {
                    continue;
                }

                var storeType = property.GetColumnType(table)
                    ?? property.GetRelationalTypeMapping().StoreType;
                actual[(tableName, columnName)] = new PostgresColumnSchema(
                    ToUdtName(storeType),
                    property.IsColumnNullable(table));
            }
        }

        Assert.Empty(CriticalPostgresSchemaContract.FindProblems(actual));
    }

    private static Dictionary<(string Table, string Column), PostgresColumnSchema> BuildSchema(
        bool useLegacyTypes,
        bool legacyBaselineOnly = false)
    {
        var columns = legacyBaselineOnly
            ? CriticalPostgresSchemaContract.Columns.Where(column => column.IsLegacyBaselineColumn)
            : CriticalPostgresSchemaContract.Columns;

        return columns.ToDictionary(
            column => (column.Table, column.Column),
            column => new PostgresColumnSchema(
                useLegacyTypes && column.RepairableLegacyUdtNames.Count > 0
                    ? column.RepairableLegacyUdtNames.Order(StringComparer.Ordinal).First()
                    : column.ExpectedUdtNames.Order(StringComparer.Ordinal).First(),
                column.IsNullable));
    }

    private static string ToUdtName(string storeType)
    {
        var normalized = storeType.ToLowerInvariant();
        if (normalized.StartsWith("character varying", StringComparison.Ordinal))
        {
            return "varchar";
        }

        return normalized switch
        {
            "timestamp with time zone" => "timestamptz",
            "timestamp without time zone" => "timestamp",
            "boolean" => "bool",
            "integer" => "int4",
            "bigint" => "int8",
            _ => normalized,
        };
    }
}
