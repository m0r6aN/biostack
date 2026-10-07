using System.Data;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BioStack.Api;

public static class ProductionDatabaseSchemaReadiness
{
    public static async Task ValidateAsync(
        BioStackDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        if (!db.Database.IsNpgsql())
        {
            throw new InvalidOperationException(
                "Production database schema readiness validation requires the Npgsql provider.");
        }

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var columns = await ReadColumnSchemasAsync(connection, transaction: null, cancellationToken);
        var problems = CriticalPostgresSchemaContract.FindProblems(columns);
        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Production database schema readiness failed for authentication- or billing-critical columns. " +
                $"Problems: {string.Join(", ", problems)}");
        }

        logger.LogInformation(
            "Validated PostgreSQL types and nullability for {ColumnCount} authentication- and billing-critical columns across {TableCount} tables.",
            CriticalPostgresSchemaContract.Columns.Count,
            CriticalPostgresSchemaContract.Columns.Select(column => column.Table).Distinct(StringComparer.Ordinal).Count());
    }

    internal static async Task<Dictionary<(string Table, string Column), PostgresColumnSchema>> ReadColumnSchemasAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var columns = new Dictionary<(string Table, string Column), PostgresColumnSchema>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT table_name, column_name, udt_name, is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'public';
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns[(reader.GetString(0), reader.GetString(1))] = new PostgresColumnSchema(
                reader.GetString(2),
                string.Equals(reader.GetString(3), "YES", StringComparison.Ordinal));
        }

        return columns;
    }
}
