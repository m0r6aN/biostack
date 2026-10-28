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
/// The structured-dose migration must stay strictly additive and nullable so existing rows keep
/// working and the previous release can keep running against the new schema.
/// </summary>
public sealed class DoseFieldsMigrationTests
{
    private static readonly string[] ExpectedCompoundRecordColumns = ["DoseAmount", "DoseUnit", "DoseFrequency", "DoseTime"];

    private static readonly string[] ExpectedProtocolItemColumns =
    [
        "CompoundDoseAmountSnapshot",
        "CompoundDoseUnitSnapshot",
        "CompoundDoseFrequencySnapshot",
        "CompoundDoseTimeSnapshot",
    ];

    [Fact]
    public void Up_OnlyAddsNullableColumnsWithNoDefaultsOrBackfill()
    {
        var operations = BuildOperations();

        Assert.All(operations, operation => Assert.IsType<AddColumnOperation>(operation));
        var added = operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(8, added.Count);
        Assert.All(added, column =>
        {
            Assert.True(column.IsNullable, $"{column.Table}.{column.Name} must be nullable");
            Assert.Null(column.DefaultValue);
            Assert.Null(column.DefaultValueSql);
        });
        Assert.Equal(
            ExpectedCompoundRecordColumns.OrderBy(n => n),
            added.Where(c => c.Table == "CompoundRecords").Select(c => c.Name).OrderBy(n => n));
        Assert.Equal(
            ExpectedProtocolItemColumns.OrderBy(n => n),
            added.Where(c => c.Table == "ProtocolItems").Select(c => c.Name).OrderBy(n => n));
    }

    [Fact]
    public void Up_GeneratesAdditivePostgresDdl_WithExactNumericPrecisionAndLengths()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_dose_migration_sql_generation")
            .Options;
        using var context = new BioStackDbContext(options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var sql = string.Join(
            Environment.NewLine,
            generator.Generate(BuildOperations()).Select(command => command.CommandText));

        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"DoseAmount\" numeric(18,4);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"DoseUnit\" character varying(16);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"DoseFrequency\" character varying(100);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"DoseTime\" character varying(100);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"ProtocolItems\" ADD \"CompoundDoseAmountSnapshot\" numeric(18,4);", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("NOT;", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DEFAULT", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER COLUMN", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_DropsOnlyTheEightAddedColumns()
    {
        var migration = new AddUserEnteredDoseFields();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = typeof(AddUserEnteredDoseFields).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(down);
        down.Invoke(migration, [builder]);

        Assert.Equal(8, builder.Operations.OfType<DropColumnOperation>().Count());
        Assert.Equal(8, builder.Operations.Count);
    }

    [Fact]
    public void ModelMapping_MatchesTheMigrationColumns()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_dose_model_check")
            .Options;
        using var context = new BioStackDbContext(options);

        var compound = context.Model.FindEntityType(typeof(BioStack.Domain.Entities.CompoundRecord))!;
        foreach (var column in ExpectedCompoundRecordColumns)
        {
            Assert.True(compound.FindProperty(column)!.IsNullable, column);
        }

        var item = context.Model.FindEntityType(typeof(BioStack.Domain.Entities.ProtocolItem))!;
        foreach (var column in ExpectedProtocolItemColumns)
        {
            Assert.True(item.FindProperty(column)!.IsNullable, column);
        }

        Assert.Equal(18, compound.FindProperty("DoseAmount")!.GetPrecision());
        Assert.Equal(4, compound.FindProperty("DoseAmount")!.GetScale());
        Assert.Equal(16, compound.FindProperty("DoseUnit")!.GetMaxLength());
        Assert.Equal(100, compound.FindProperty("DoseFrequency")!.GetMaxLength());
        Assert.Equal(100, compound.FindProperty("DoseTime")!.GetMaxLength());
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations()
    {
        var migration = new AddUserEnteredDoseFields();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var up = typeof(AddUserEnteredDoseFields).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);
        up.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
