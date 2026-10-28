namespace BioStack.Api.Tests;

using System.Reflection;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

/// <summary>
/// The catalog-link migration must stay strictly additive and backward-compatible: three nullable
/// columns with no default or backfill (legacy rows read as unverified and keep counting toward the
/// cap) and one NOT NULL column that carries a database default, so the previous release (which does
/// not know the column) keeps inserting valid rows.
/// </summary>
public sealed class CompoundCatalogLinkMigrationTests
{
    private static readonly string[] NullableColumns = ["CatalogSlug", "VerificationState", "CategorySource"];

    [Fact]
    public void Up_OnlyAddsFourColumns_ThreeNullableAndOneDefaultedBoolean()
    {
        var operations = BuildOperations();

        Assert.Equal(4, operations.Count);
        Assert.All(operations, operation => Assert.IsType<AddColumnOperation>(operation));
        var added = operations.OfType<AddColumnOperation>().ToList();
        Assert.All(added, column => Assert.Equal("CompoundRecords", column.Table));

        foreach (var name in NullableColumns)
        {
            var column = added.Single(c => c.Name == name);
            Assert.True(column.IsNullable, name);
            Assert.Null(column.DefaultValue);
            Assert.Null(column.DefaultValueSql);
        }

        var toggle = added.Single(c => c.Name == "IncludeInProviderSummary");
        Assert.False(toggle.IsNullable);
        Assert.Equal(true, toggle.DefaultValue);
        Assert.DoesNotContain(operations, o => o is DropColumnOperation or DropTableOperation or AlterColumnOperation or SqlOperation or UpdateDataOperation or InsertDataOperation);
    }

    [Fact]
    public void Up_GeneratesAdditivePostgresDdl_WithNoBackfill()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_catalog_link_migration_sql_generation")
            .Options;
        using var context = new BioStackDbContext(options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var sql = string.Join(Environment.NewLine, generator.Generate(BuildOperations()).Select(command => command.CommandText));

        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"CatalogSlug\" character varying(160);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"VerificationState\" character varying(16);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"CategorySource\" character varying(16);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"IncludeInProviderSummary\" boolean NOT NULL DEFAULT TRUE;", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER COLUMN", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_DropsOnlyTheFourAddedColumns()
    {
        var migration = new AddCompoundCatalogLinkAndVerification();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = typeof(AddCompoundCatalogLinkAndVerification).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(down);
        down.Invoke(migration, [builder]);

        Assert.Equal(4, builder.Operations.OfType<DropColumnOperation>().Count());
        Assert.Equal(4, builder.Operations.Count);
    }

    [Fact]
    public void ModelMapping_MatchesTheMigrationColumns()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_catalog_link_model_check")
            .Options;
        using var context = new BioStackDbContext(options);
        var compound = context.Model.FindEntityType(typeof(BioStack.Domain.Entities.CompoundRecord))!;

        Assert.Equal(160, compound.FindProperty("CatalogSlug")!.GetMaxLength());
        Assert.Equal(16, compound.FindProperty("VerificationState")!.GetMaxLength());
        Assert.Equal(16, compound.FindProperty("CategorySource")!.GetMaxLength());
        foreach (var name in NullableColumns)
            Assert.True(compound.FindProperty(name)!.IsNullable, name);
        Assert.False(compound.FindProperty("IncludeInProviderSummary")!.IsNullable);
    }

    [Fact]
    public void MigrationIdSortsAfterThePreviousDoseMigrations()
    {
        var id = typeof(AddCompoundCatalogLinkAndVerification).GetCustomAttribute<MigrationAttribute>()!.Id;
        Assert.True(string.CompareOrdinal(id, "20261028170000_AddDoseLogEntriesAndStructuredFrequency") > 0);
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations()
    {
        var migration = new AddCompoundCatalogLinkAndVerification();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var up = typeof(AddCompoundCatalogLinkAndVerification).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);
        up.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
