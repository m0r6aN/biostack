namespace BioStack.Api.Tests;

using System.Reflection;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

/// <summary>
/// The dose-log and structured-frequency migration must stay strictly additive: one new table and six
/// nullable columns, no defaults, no backfill, no drops, so existing rows keep working and the previous
/// release can keep running against the new schema.
/// </summary>
public sealed class DoseLogEntriesMigrationTests
{
    private static readonly string[] ExpectedFrequencyColumns =
    [
        "FrequencyKind",
        "FrequencyIntervalDays",
        "FrequencyWeekdays",
        "FrequencyTimesPerWeek",
        "FrequencyStartDate",
        "FrequencyTimesOfDay",
    ];

    [Fact]
    public void Up_OnlyAddsNullableCompoundColumnsAndOneNewTable()
    {
        var operations = BuildOperations();

        var added = operations.OfType<AddColumnOperation>().ToList();
        Assert.Equal(6, added.Count);
        Assert.All(added, column =>
        {
            Assert.Equal("CompoundRecords", column.Table);
            Assert.True(column.IsNullable, $"{column.Table}.{column.Name} must be nullable");
            Assert.Null(column.DefaultValue);
            Assert.Null(column.DefaultValueSql);
        });
        Assert.Equal(ExpectedFrequencyColumns.OrderBy(n => n), added.Select(c => c.Name).OrderBy(n => n));

        Assert.Equal("DoseLogEntries", Assert.Single(operations.OfType<CreateTableOperation>()).Name);
        Assert.Equal(2, operations.OfType<CreateIndexOperation>().Count());
        Assert.Equal(9, operations.Count);
        Assert.DoesNotContain(operations, o => o is DropColumnOperation or DropTableOperation or AlterColumnOperation or SqlOperation or UpdateDataOperation or InsertDataOperation);
    }

    [Fact]
    public void Up_CreatesTheTableWithCascadingForeignKeysToProfileAndCompound()
    {
        var table = BuildOperations().OfType<CreateTableOperation>().Single();

        Assert.Equal(
            ["Id", "PersonId", "CompoundRecordId", "LogDate", "TakenAtUtc", "Amount", "Unit", "Note", "CreatedAtUtc"],
            table.Columns.Select(c => c.Name));
        Assert.Equal("Note", table.Columns.Single(c => c.IsNullable).Name);
        Assert.Equal(18, table.Columns.Single(c => c.Name == "Amount").Precision);
        Assert.Equal(4, table.Columns.Single(c => c.Name == "Amount").Scale);

        Assert.Equal(2, table.ForeignKeys.Count);
        Assert.All(table.ForeignKeys, fk => Assert.Equal(ReferentialAction.Cascade, fk.OnDelete));
        Assert.Contains(table.ForeignKeys, fk => fk.PrincipalTable == "PersonProfiles" && fk.Columns.SequenceEqual(["PersonId"]));
        Assert.Contains(table.ForeignKeys, fk => fk.PrincipalTable == "CompoundRecords" && fk.Columns.SequenceEqual(["CompoundRecordId"]));
    }

    [Fact]
    public void Up_GeneratesAdditivePostgresDdl()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_dose_log_migration_sql_generation")
            .Options;
        using var context = new BioStackDbContext(options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var sql = string.Join(
            Environment.NewLine,
            generator.Generate(BuildOperations()).Select(command => command.CommandText));

        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"FrequencyKind\" character varying(16);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"FrequencyIntervalDays\" integer;", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"FrequencyWeekdays\" character varying(20);", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"FrequencyTimesPerWeek\" integer;", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"FrequencyStartDate\" timestamp with time zone;", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE \"CompoundRecords\" ADD \"FrequencyTimesOfDay\" character varying(60);", sql, StringComparison.Ordinal);

        Assert.Contains("CREATE TABLE \"DoseLogEntries\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"Amount\" numeric(18,4) NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"Unit\" character varying(16) NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"Note\" character varying(500)", sql, StringComparison.Ordinal);
        Assert.Contains("REFERENCES \"PersonProfiles\" (\"Id\") ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("REFERENCES \"CompoundRecords\" (\"Id\") ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX \"IX_DoseLogEntries_PersonId_LogDate\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX \"IX_DoseLogEntries_CompoundRecordId\"", sql, StringComparison.Ordinal);

        Assert.DoesNotContain("DEFAULT", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER COLUMN", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_DropsOnlyWhatUpAdded()
    {
        var migration = new AddDoseLogEntriesAndStructuredFrequency();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = typeof(AddDoseLogEntriesAndStructuredFrequency).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(down);
        down.Invoke(migration, [builder]);

        Assert.Equal("DoseLogEntries", Assert.Single(builder.Operations.OfType<DropTableOperation>()).Name);
        Assert.Equal(6, builder.Operations.OfType<DropColumnOperation>().Count());
        Assert.Equal(7, builder.Operations.Count);
    }

    [Fact]
    public void Migration_IsDiscoverableAfterThePreviousDoseMigration()
    {
        var attribute = typeof(AddDoseLogEntriesAndStructuredFrequency).GetCustomAttribute<MigrationAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("20261028170000_AddDoseLogEntriesAndStructuredFrequency", attribute.Id);
        Assert.True(string.CompareOrdinal(attribute.Id, "20261028150000_AddUserEnteredDoseFields") > 0);
        Assert.NotNull(typeof(AddDoseLogEntriesAndStructuredFrequency).GetCustomAttribute<DbContextAttribute>());
    }

    [Fact]
    public void ModelMapping_MatchesTheMigration()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_dose_log_model_check")
            .Options;
        using var context = new BioStackDbContext(options);

        var compound = context.Model.FindEntityType(typeof(CompoundRecord))!;
        foreach (var column in ExpectedFrequencyColumns)
        {
            Assert.True(compound.FindProperty(column)!.IsNullable, column);
        }

        Assert.Equal(16, compound.FindProperty("FrequencyKind")!.GetMaxLength());
        Assert.Equal(20, compound.FindProperty("FrequencyWeekdays")!.GetMaxLength());
        Assert.Equal(60, compound.FindProperty("FrequencyTimesOfDay")!.GetMaxLength());

        var entry = context.Model.FindEntityType(typeof(DoseLogEntry))!;
        Assert.Equal("DoseLogEntries", entry.GetTableName());
        Assert.Equal(18, entry.FindProperty("Amount")!.GetPrecision());
        Assert.Equal(4, entry.FindProperty("Amount")!.GetScale());
        Assert.False(entry.FindProperty("Amount")!.IsNullable);
        Assert.Equal(16, entry.FindProperty("Unit")!.GetMaxLength());
        Assert.Equal(500, entry.FindProperty("Note")!.GetMaxLength());
        Assert.True(entry.FindProperty("Note")!.IsNullable);
        Assert.All(entry.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior));
        Assert.Equal(2, entry.GetForeignKeys().Count());
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations()
    {
        var migration = new AddDoseLogEntriesAndStructuredFrequency();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var up = typeof(AddDoseLogEntriesAndStructuredFrequency).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);
        up.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
