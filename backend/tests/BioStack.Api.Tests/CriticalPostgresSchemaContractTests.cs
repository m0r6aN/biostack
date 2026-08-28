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
            _ => normalized,
        };
    }
}
