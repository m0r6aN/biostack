namespace BioStack.Api.Tests;

using System.Reflection;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

public sealed class PasskeyAuthenticationMigrationTests
{
    [Fact]
    public void Up_AddsOnlyIsolatedPasskeyTablesAndIndexes()
    {
        var operations = BuildOperations();

        Assert.Equal(2, operations.OfType<CreateTableOperation>().Count());
        Assert.Equal(5, operations.OfType<CreateIndexOperation>().Count());
        Assert.Empty(operations.OfType<AlterColumnOperation>());
        Assert.Empty(operations.OfType<SqlOperation>());
        Assert.Contains(operations.OfType<CreateTableOperation>(), operation => operation.Name == "PasskeyCredentials");
        Assert.Contains(operations.OfType<CreateTableOperation>(), operation => operation.Name == "PasskeyOperationChallenges");
    }

    [Fact]
    public void Up_GeneratesNativePostgresTypesWithoutExistingTextDriftRepair()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseNpgsql("Host=localhost;Database=biostack_passkey_migration_sql_generation")
            .Options;
        using var context = new BioStackDbContext(options);
        var generator = context.GetService<IMigrationsSqlGenerator>();
        var sql = string.Join(
            Environment.NewLine,
            generator.Generate(BuildOperations()).Select(command => command.CommandText));

        Assert.Contains("CREATE TABLE \"PasskeyCredentials\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"CredentialId\" bytea", sql, StringComparison.Ordinal);
        Assert.Contains("\"OptionsJson\" text", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER COLUMN", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("CompoundGraph", sql, StringComparison.Ordinal);
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations()
    {
        var migration = new AddPasskeyAuthentication();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var up = typeof(AddPasskeyAuthentication).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);
        up.Invoke(migration, [builder]);
        return builder.Operations;
    }
}
