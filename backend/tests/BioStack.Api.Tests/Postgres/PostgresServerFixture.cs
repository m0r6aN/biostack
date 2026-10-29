namespace BioStack.Api.Tests.Postgres;

using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// One throwaway Postgres 18 server shared by the Postgres tests. Resolution order:
/// 1. <c>BIOSTACK_TEST_POSTGRES_ADMIN</c> (a connection string to a server we may CREATE DATABASE on),
/// 2. a Testcontainers <c>postgres:18</c> container when Docker is reachable (GitHub-hosted runners have it).
/// Without either, tests are skipped locally, but fail in CI (GITHUB_ACTIONS / CI set) so the coverage cannot
/// silently disappear.
/// </summary>
public sealed class PostgresServerFixture : IAsyncLifetime
{
    public const string AdminConnectionStringVariable = "BIOSTACK_TEST_POSTGRES_ADMIN";
    private PostgreSqlContainer? _container;

    public string AdminConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(AdminConnectionStringVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            AdminConnectionString = configured;
            return;
        }

        if (!PostgresAvailability.DockerReachable)
        {
            return; // Tests are skipped (or fail in CI) by PostgresFactAttribute before they use this.
        }

        _container = new PostgreSqlBuilder("postgres:18").Build();
        await _container.StartAsync();
        AdminConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>Creates an empty database and returns its connection string. Drop it with <see cref="DropDatabaseAsync"/>.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = "biostack_" + Guid.NewGuid().ToString("N")[..12];
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
        await command.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = name, Pooling = false }.ConnectionString;
    }

    public async Task DropDatabaseAsync(string connectionString)
    {
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresServerFixture>
{
    public const string Name = "Postgres server";
}

internal static class PostgresAvailability
{
    public static bool InCi =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    public static bool Configured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PostgresServerFixture.AdminConnectionStringVariable));

    public static bool DockerReachable =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST"))
        || File.Exists("/var/run/docker.sock")
        || OperatingSystem.IsWindows();
}

/// <summary>A fact that needs a real Postgres: skipped locally when none is available, never skipped in CI.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!PostgresAvailability.Configured && !PostgresAvailability.DockerReachable && !PostgresAvailability.InCi)
        {
            Skip = $"No Postgres available: set {PostgresServerFixture.AdminConnectionStringVariable} or start Docker.";
        }
    }
}
