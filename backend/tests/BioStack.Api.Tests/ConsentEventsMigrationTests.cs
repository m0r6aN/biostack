namespace BioStack.Api.Tests;

using System.Reflection;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

/// <summary>
/// The consent-history migration must stay additive: one new table, a read-only backfill into it (PostgreSQL
/// only), and nothing in an existing table changed.
/// </summary>
public sealed class ConsentEventsMigrationTests
{
    private static List<MigrationOperation> Up(string provider)
    {
        var builder = new MigrationBuilder(provider);
        typeof(AddConsentEvents).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(new AddConsentEvents(), [builder]);
        return builder.Operations.ToList();
    }

    [Fact]
    public void Up_CreatesTheTableAndIndex_AndBackfillsOnlyOnPostgres()
    {
        var postgres = Up("Npgsql.EntityFrameworkCore.PostgreSQL");
        var create = Assert.IsType<CreateTableOperation>(postgres[0]);
        Assert.Equal("ConsentEvents", create.Name);
        Assert.Equal(["EventType", "Id", "OccurredAtUtc", "UserId", "Version"], create.Columns.Select(c => c.Name).OrderBy(n => n).ToArray());
        var fk = Assert.Single(create.ForeignKeys);
        Assert.Equal("AppUsers", fk.PrincipalTable);
        Assert.Equal(ReferentialAction.Cascade, fk.OnDelete);
        Assert.IsType<CreateIndexOperation>(postgres[1]);
        Assert.Equal(2, postgres.OfType<SqlOperation>().Count());
        Assert.Equal(4, postgres.Count);
        Assert.DoesNotContain(postgres, o => o is DropColumnOperation or DropTableOperation or AlterColumnOperation or UpdateDataOperation or DeleteDataOperation);

        var sqlite = Up("Microsoft.EntityFrameworkCore.Sqlite");
        Assert.Empty(sqlite.OfType<SqlOperation>());
        Assert.Equal(2, sqlite.Count);
    }

    [Fact]
    public void Backfill_IsAnInsertSelectIntoTheNewTable_AndTouchesNoExistingRow()
    {
        foreach (var sql in new[] { AddConsentEvents.BackfillAcceptedSql, AddConsentEvents.BackfillDeclinedSql })
        {
            Assert.StartsWith("INSERT INTO \"ConsentEvents\"", sql, StringComparison.Ordinal);
            Assert.Contains("FROM \"AppUsers\"", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("UPDATE ", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("DELETE ", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("DROP ", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("gen_random_uuid", sql, StringComparison.Ordinal);   // version-independent id generation
        }

        Assert.Contains("'accepted'", AddConsentEvents.BackfillAcceptedSql, StringComparison.Ordinal);
        Assert.Contains("\"ConsentAcceptedAtUtc\" IS NOT NULL", AddConsentEvents.BackfillAcceptedSql, StringComparison.Ordinal);
        Assert.Contains("'declined'", AddConsentEvents.BackfillDeclinedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void Up_GeneratesPostgresDdl()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseNpgsql("Host=localhost;Database=biostack_consent_events_sql_generation").Options;
        using var context = new BioStackDbContext(options);
        var sql = string.Join(Environment.NewLine, context.GetService<IMigrationsSqlGenerator>().Generate(Up("Npgsql.EntityFrameworkCore.PostgreSQL")).Select(c => c.CommandText));

        Assert.Contains("CREATE TABLE \"ConsentEvents\"", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX \"IX_ConsentEvents_UserId_OccurredAtUtc\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_DropsOnlyThatTable()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddConsentEvents).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(new AddConsentEvents(), [builder]);
        Assert.Equal("ConsentEvents", Assert.IsType<DropTableOperation>(Assert.Single(builder.Operations)).Name);
    }

    [Fact]
    public void ModelMapping_MatchesTheMigrationColumns()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new BioStackDbContext(options);
        var entity = context.Model.FindEntityType(typeof(ConsentEvent))!;
        var create = (CreateTableOperation)Up("Npgsql.EntityFrameworkCore.PostgreSQL")[0];

        Assert.Equal("ConsentEvents", entity.GetTableName());
        Assert.Equal(create.Columns.Select(c => c.Name).OrderBy(n => n), entity.GetProperties().Select(p => p.GetColumnName()).OrderBy(n => n));
        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(entity.GetForeignKeys()).DeleteBehavior);
    }
}
