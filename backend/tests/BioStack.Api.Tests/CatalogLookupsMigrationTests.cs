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

/// <summary>The phase 3 migration must stay additive: two new tables, their indexes, and nothing else.</summary>
public sealed class CatalogLookupsMigrationTests
{
    private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";

    private static List<MigrationOperation> Run(string method, string provider)
    {
        var builder = new MigrationBuilder(provider);
        typeof(AddCatalogLookupsAndUserNotices).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(new AddCatalogLookupsAndUserNotices(), [builder]);
        return builder.Operations.ToList();
    }

    [Theory]
    [InlineData(Npgsql)]
    [InlineData(Sqlite)]
    public void Up_OnlyCreatesTwoTablesAndTheirIndexes(string provider)
    {
        var ops = Run("Up", provider);

        Assert.Equal(["CatalogLookups", "UserNotices"], ops.OfType<CreateTableOperation>().Select(o => o.Name).OrderBy(n => n).ToArray());
        Assert.All(ops, o => Assert.True(o is CreateTableOperation or CreateIndexOperation, $"Unexpected operation {o.GetType().Name}"));
        Assert.DoesNotContain(ops, o => o is SqlOperation or DropColumnOperation or DropTableOperation or AlterColumnOperation or AddColumnOperation or UpdateDataOperation or DeleteDataOperation);
        var indexes = ops.OfType<CreateIndexOperation>().ToList();
        Assert.Equal(
            ["IX_CatalogLookups_CompoundRecordId", "IX_CatalogLookups_PersonId_Status", "IX_CatalogLookups_UserId_NameKey", "IX_UserNotices_UserId_CreatedAtUtc", "IX_UserNotices_UserId_DedupeKey"],
            indexes.Select(i => i.Name).OrderBy(n => n).ToArray());
        Assert.Equal(["IX_CatalogLookups_UserId_NameKey", "IX_UserNotices_UserId_DedupeKey"], indexes.Where(i => i.IsUnique).Select(i => i.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void ForeignKeys_FollowTheAccountAndKeepTheLookupWhenAnItemIsDeleted()
    {
        var creates = Run("Up", Npgsql).OfType<CreateTableOperation>().ToDictionary(o => o.Name);

        var lookupFks = creates["CatalogLookups"].ForeignKeys.ToDictionary(f => f.PrincipalTable);
        Assert.Equal(ReferentialAction.Cascade, lookupFks["AppUsers"].OnDelete);
        Assert.Equal(ReferentialAction.Cascade, lookupFks["PersonProfiles"].OnDelete);
        Assert.Equal(ReferentialAction.SetNull, lookupFks["CompoundRecords"].OnDelete);
        var noticeFk = Assert.Single(creates["UserNotices"].ForeignKeys);
        Assert.Equal("AppUsers", noticeFk.PrincipalTable);
        Assert.Equal(ReferentialAction.Cascade, noticeFk.OnDelete);
    }

    [Fact]
    public void Up_GeneratesPostgresDdlWithNoAlterOfExistingTables()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseNpgsql("Host=localhost;Database=biostack_catalog_lookups_sql_generation").Options;
        using var context = new BioStackDbContext(options);
        var sql = string.Join(Environment.NewLine, context.GetService<IMigrationsSqlGenerator>().Generate(Run("Up", Npgsql)).Select(c => c.CommandText));

        Assert.Contains("CREATE TABLE \"CatalogLookups\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"UserNotices\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE UNIQUE INDEX \"IX_CatalogLookups_UserId_NameKey\"", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE SET NULL", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP ", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_DropsOnlyThoseTwoTables()
    {
        var ops = Run("Down", Npgsql);

        Assert.Equal(["CatalogLookups", "UserNotices"], ops.Select(o => Assert.IsType<DropTableOperation>(o).Name).OrderBy(n => n).ToArray());
    }

    [Theory]
    [InlineData(typeof(CatalogLookup), "CatalogLookups")]
    [InlineData(typeof(UserNotice), "UserNotices")]
    public void ModelMapping_MatchesTheMigrationColumnsAndLengths(Type clr, string table)
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new BioStackDbContext(options);
        var entity = context.Model.FindEntityType(clr)!;
        var create = Run("Up", Npgsql).OfType<CreateTableOperation>().Single(o => o.Name == table);

        Assert.Equal(table, entity.GetTableName());
        Assert.Equal(create.Columns.Select(c => c.Name).OrderBy(n => n), entity.GetProperties().Select(p => p.GetColumnName()).OrderBy(n => n));
        foreach (var column in create.Columns)
        {
            var property = entity.GetProperties().Single(p => p.GetColumnName() == column.Name);
            Assert.Equal(property.GetMaxLength(), column.MaxLength);
            Assert.Equal(property.IsNullable, column.IsNullable);
        }
    }
}
