namespace BioStack.Api.Tests;

using BioStack.Api;
using Xunit;

public class DatabaseProviderResolverTests
{
    [Theory]
    [InlineData("postgres")]
    [InlineData("postgresql")]
    [InlineData("npgsql")]
    public void IsPostgres_ReturnsTrue_ForExplicitProvider(string provider)
    {
        Assert.True(DatabaseProviderResolver.IsPostgres(provider, "Data Source=./data/biostack.db"));
    }

    [Fact]
    public void IsPostgres_ReturnsFalse_ForExplicitSqliteProvider()
    {
        Assert.False(DatabaseProviderResolver.IsPostgres("sqlite", "Host=localhost;Database=biostack;Username=postgres;Password=secret"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Data Source=./data/biostack.db")]
    [InlineData("Filename=:memory:")]
    public void IsPostgres_ReturnsFalse_ForNonPostgresConnectionStrings(string? connectionString)
    {
        Assert.False(DatabaseProviderResolver.IsPostgres(connectionString));
        Assert.False(DatabaseProviderResolver.IsPostgres(null, connectionString));
    }

    [Theory]
    [InlineData("Host=localhost;Database=biostack;Username=postgres;Password=secret")]
    [InlineData("Host=myserver.postgres.database.azure.com;Database=biostack;Username=biostackadmin;Password=secret;Ssl Mode=Require")]
    public void IsPostgres_ReturnsTrue_ForPostgresConnectionStrings(string connectionString)
    {
        Assert.True(DatabaseProviderResolver.IsPostgres(connectionString));
        Assert.True(DatabaseProviderResolver.IsPostgres(null, connectionString));
    }
}