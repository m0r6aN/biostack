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
/// The account-deletion migration must stay strictly additive (one new table, no existing table touched, nothing
/// backfilled) and the table must stay free of any column that could identify a person.
/// </summary>
public sealed class AccountDeletionRecordsMigrationTests
{
    private static List<MigrationOperation> BuildOperations()
    {
        var migration = new AddAccountDeletionRecords();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddAccountDeletionRecords).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(migration, [builder]);
        return builder.Operations.ToList();
    }

    [Fact]
    public void Up_OnlyCreatesTheDeletionRecordsTable()
    {
        var operations = BuildOperations();
        var create = Assert.IsType<CreateTableOperation>(Assert.Single(operations));
        Assert.Equal("AccountDeletionRecords", create.Name);
        Assert.Equal(
            ["BackupsAgeOutByUtc", "DeletedOnUtc", "Id", "ProcedureVersion", "RowsAffected"],
            create.Columns.Select(c => c.Name).OrderBy(n => n).ToArray());
        Assert.Empty(create.ForeignKeys);
        Assert.DoesNotContain(operations, o => o is DropColumnOperation or DropTableOperation or AlterColumnOperation or SqlOperation or UpdateDataOperation or InsertDataOperation);
    }

    [Fact]
    public void Up_GeneratesAdditivePostgresDdl()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseNpgsql("Host=localhost;Database=biostack_account_deletion_sql_generation").Options;
        using var context = new BioStackDbContext(options);
        var sql = string.Join(Environment.NewLine, context.GetService<IMigrationsSqlGenerator>().Generate(BuildOperations()).Select(c => c.CommandText));

        Assert.Contains("CREATE TABLE \"AccountDeletionRecords\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER TABLE", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Down_DropsOnlyThatTable()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(AddAccountDeletionRecords).GetMethod("Down", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(new AddAccountDeletionRecords(), [builder]);
        Assert.Equal("AccountDeletionRecords", Assert.IsType<DropTableOperation>(Assert.Single(builder.Operations)).Name);
    }

    [Fact]
    public void ModelMapping_MatchesTheMigrationColumns_AndHasNoPersonKey()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new BioStackDbContext(options);
        var entity = context.Model.FindEntityType(typeof(AccountDeletionRecord))!;
        var create = (CreateTableOperation)BuildOperations().Single();

        Assert.Equal("AccountDeletionRecords", entity.GetTableName());
        Assert.Equal(create.Columns.Select(c => c.Name).OrderBy(n => n), entity.GetProperties().Select(p => p.GetColumnName()).OrderBy(n => n));
        Assert.Empty(entity.GetForeignKeys());
    }
}
