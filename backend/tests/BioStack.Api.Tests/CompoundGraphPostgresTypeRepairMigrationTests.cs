namespace BioStack.Api.Tests;

using System.Reflection;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

public sealed class CompoundGraphPostgresTypeRepairMigrationTests
{
    [Fact]
    public void Up_EmitsForwardRepairForNpgsql()
    {
        var operations = BuildOperations("Npgsql.EntityFrameworkCore.PostgreSQL");
        var sql = GenerateNpgsqlSql(operations);

        Assert.Contains("""ALTER COLUMN "Id" TYPE uuid""", sql, StringComparison.Ordinal);
        Assert.Contains("""ALTER COLUMN "IsActive" TYPE boolean""", sql, StringComparison.Ordinal);
        Assert.Contains("""ALTER COLUMN "NeedsReview" TYPE boolean""", sql, StringComparison.Ordinal);
        Assert.Contains("TYPE timestamp with time zone", sql, StringComparison.Ordinal);
        Assert.Contains("""AT TIME ZONE 'UTC'""", sql, StringComparison.Ordinal);
        Assert.Contains(""":?[0-9]{2})?)$""", sql, StringComparison.Ordinal);
        Assert.Contains(
            "contains an unrecognized boolean value",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            """FK_CompoundGraphRelationships_CompoundGraphArtifacts_GraphArtifactId""",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            """FK_CompoundGraphFindings_CompoundGraphArtifacts_GraphArtifactId""",
            sql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Up_IsNoOpForNonPostgresProviders()
    {
        Assert.Empty(BuildOperations("Microsoft.EntityFrameworkCore.Sqlite"));
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
    {
        var migration = new RepairCompoundGraphPostgresTypes();
        var builder = new MigrationBuilder(provider);
        var up = typeof(RepairCompoundGraphPostgresTypes).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(up);
        up.Invoke(migration, new object[] { builder });
        return builder.Operations;
    }
}
