using BioStack.Api;
using Xunit;

namespace BioStack.Api.Tests;

public sealed class ProductionMigrationHistoryBaselineTests
{
    [Fact]
    public void FindMissingSchemaObjects_AcceptsCompleteApprovedBaseline()
    {
        var tables = ReadRequiredValues("RequiredTables");
        var indexes = ReadRequiredValues("RequiredIndexes");
        var columns = ReadRequiredColumns();

        var missing = ProductionMigrationHistoryBaseline.FindMissingSchemaObjects(tables, columns, indexes);

        Assert.Empty(missing);
    }

    [Fact]
    public void FindMissingSchemaObjects_FailsClosedWithPreciseMissingObjects()
    {
        var missing = ProductionMigrationHistoryBaseline.FindMissingSchemaObjects(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<(string Table, string Column)>(),
            new HashSet<string>(StringComparer.Ordinal));

        Assert.Contains("table:AppUsers", missing);
        Assert.Contains("index:IX_AppUsers_Email", missing);
    }

    [Fact]
    public void Baseline_StopsBeforeStripeAndConsentMigrations()
    {
        var migrationIds = ReadRequiredValues("BaselineMigrationIds");

        Assert.Equal(
            "20260422125251_RecoverBillingTierEnforcement",
            migrationIds.Order(StringComparer.Ordinal).Last());
        Assert.DoesNotContain(
            migrationIds,
            migration => migration.Contains("Stripe", StringComparison.Ordinal));
        Assert.DoesNotContain(
            migrationIds,
            migration => migration.Contains("ConsentDecline", StringComparison.Ordinal));
    }

    [Fact]
    public void BaselineMigrationIds_CanBeAddedWithoutReplacingLegacyHistory()
    {
        var migrationIds = ReadRequiredValues("BaselineMigrationIds");
        var applied = new HashSet<string>(StringComparer.Ordinal)
        {
            "20260401000000_LegacyEnsureCreatedBaseline",
        };

        var missing = migrationIds.Where(id => !applied.Contains(id)).ToArray();

        Assert.Single(missing);
        Assert.Contains("20260401000000_LegacyEnsureCreatedBaseline", applied);
        Assert.Contains(migrationIds.First(), missing);
    }

    private static HashSet<string> ReadRequiredValues(string fieldName)
    {
        var field = typeof(ProductionMigrationHistoryBaseline).GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return ((string[])field!.GetValue(null)!).ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<(string Table, string Column)> ReadRequiredColumns()
    {
        var field = typeof(ProductionMigrationHistoryBaseline).GetField(
            "RequiredColumns",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return ((ValueTuple<string, string>[])field!.GetValue(null)!).ToHashSet();
    }
}
