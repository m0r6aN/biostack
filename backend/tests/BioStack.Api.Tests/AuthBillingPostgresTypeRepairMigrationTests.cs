namespace BioStack.Api.Tests;

using System.Reflection;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

public sealed class AuthBillingPostgresTypeRepairMigrationTests
{
    [Fact]
    public void Up_EmitsValidatedForwardRepairForNpgsql()
    {
        var operations = BuildOperations("Npgsql.EntityFrameworkCore.PostgreSQL");
        var sql = GenerateNpgsqlSql(operations);

        Assert.Contains("Validate every legacy value before", sql, StringComparison.Ordinal);
        Assert.Contains("schema repair preflight failed", sql, StringComparison.Ordinal);
        Assert.Contains("contains NULL", sql, StringComparison.Ordinal);
        Assert.Contains("contains a blank value", sql, StringComparison.Ordinal);
        Assert.Contains("contains a malformed", sql, StringComparison.Ordinal);
        Assert.Contains("TYPE uuid", sql, StringComparison.Ordinal);
        Assert.Contains("TYPE boolean", sql, StringComparison.Ordinal);
        Assert.Contains("TYPE timestamp with time zone", sql, StringComparison.Ordinal);
        Assert.Contains("AT TIME ZONE 'UTC'", sql, StringComparison.Ordinal);

        foreach (var table in new[]
                 {
                     "AppUsers", "AuthIdentities", "AuthChallenges", "Sessions",
                     "Subscriptions", "StripeWebhookEvents",
                 })
        {
            Assert.Contains($"'{table}'", sql, StringComparison.Ordinal);
        }

        foreach (var foreignKey in new[]
                 {
                     "FK_PersonProfiles_AppUsers_OwnerId",
                     "FK_AuthIdentities_AppUsers_UserId",
                     "FK_AuthChallenges_AuthIdentities_IdentityId",
                     "FK_Sessions_AppUsers_UserId",
                     "FK_Subscriptions_AppUsers_AppUserId",
                 })
        {
            Assert.Contains(foreignKey, sql, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Up_CoversEveryCriticalUuidTimestampAndBooleanColumn()
    {
        var sql = GenerateNpgsqlSql(BuildOperations("Npgsql.EntityFrameworkCore.PostgreSQL"));
        var repairedColumns = CriticalPostgresSchemaContract.Columns.Where(column =>
            column.ExpectedUdtNames.SetEquals(["uuid"])
            || column.ExpectedUdtNames.SetEquals(["timestamptz"])
            || column.ExpectedUdtNames.SetEquals(["bool"]));

        foreach (var column in repairedColumns)
        {
            Assert.Contains($"'{column.Table}', '{column.Column}'", sql, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Up_IsNoOpForSqlite()
    {
        Assert.Empty(BuildOperations("Microsoft.EntityFrameworkCore.Sqlite"));
    }

    [Fact]
    public void StripeLifecycleMigration_CastsLegacyProcessedTimestampForNpgsql()
    {
        var operations = BuildOperations<HardenStripeWebhookLifecycle>(
            "Npgsql.EntityFrameworkCore.PostgreSQL");
        var sql = GenerateNpgsqlSql(operations);

        Assert.Contains("Stripe migration preflight failed", sql, StringComparison.Ordinal);
        Assert.Contains("ProcessedAtUtc\"::text", sql, StringComparison.Ordinal);
        Assert.Contains("AT TIME ZONE 'UTC'", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void StripeLifecycleMigration_PreservesSqliteCopy()
    {
        var operations = BuildOperations<HardenStripeWebhookLifecycle>(
            "Microsoft.EntityFrameworkCore.Sqlite");
        var sqlOperations = operations.OfType<SqlOperation>().ToArray();

        Assert.Single(sqlOperations);
        Assert.Equal(
            "UPDATE \"StripeWebhookEvents\" SET \"LastAttemptAtUtc\" = \"ProcessedAtUtc\";",
            sqlOperations[0].Sql);
    }

    private static string GenerateNpgsqlSql(IReadOnlyList<MigrationOperation> operations)
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_migration_sql_generation")
            .Options;
        using var context = new BioStackDbContext(options);
        var generator = context.GetService<IMigrationsSqlGenerator>();

        return string.Join(
            Environment.NewLine,
            generator.Generate(operations).Select(command => command.CommandText));
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations(string provider)
        => BuildOperations<RepairAuthBillingPostgresTypes>(provider);

    private static IReadOnlyList<MigrationOperation> BuildOperations<TMigration>(string provider)
        where TMigration : Migration, new()
    {
        var migration = new TMigration();
        var builder = new MigrationBuilder(provider);
        var up = typeof(TMigration).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(up);
        up.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
