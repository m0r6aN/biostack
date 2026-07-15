using System.Data;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BioStack.Api;

public static class ProductionMigrationHistoryBaseline
{
    internal static readonly string[] BaselineMigrationIds =
    [
        "20260422125251_RecoverBillingTierEnforcement",
    ];

    private static readonly string[] RequiredTables =
    [
        "AppUsers", "AuthChallenges", "AuthIdentities", "CheckIns",
        "CompoundInteractionHints", "CompoundRecords", "InteractionFlags", "KnowledgeEntries",
        "LeadCaptures", "PersonProfiles", "ProtocolComputationRecords", "ProtocolItems",
        "ProtocolPhases", "ProtocolReviewCompletedEvents", "ProtocolRuns", "Protocols",
        "Sessions", "StripeWebhookEvents", "Subscriptions", "TimelineEvents",
    ];

    private static readonly (string Table, string Column)[] RequiredColumns =
    [];

    private static readonly string[] RequiredIndexes =
    [
        "IX_AppUsers_Email",
        "IX_AppUsers_Provider_ProviderKey",
        "IX_AppUsers_StripeCustomerId",
        "IX_AuthChallenges_ExpiresAtUtc",
        "IX_AuthChallenges_IdentityId",
        "IX_AuthChallenges_TokenHash",
        "IX_AuthIdentities_Type_ValueNormalized",
        "IX_AuthIdentities_UserId",
        "IX_CheckIns_Date",
        "IX_CheckIns_PersonId",
        "IX_CheckIns_ProtocolRunId",
        "IX_CompoundInteractionHints_CompoundA_CompoundB",
        "IX_CompoundRecords_PersonId",
        "IX_LeadCaptures_Email_Source",
        "IX_PersonProfiles_OwnerId",
        "IX_ProtocolComputationRecords_ProtocolId",
        "IX_ProtocolComputationRecords_ProtocolRunId",
        "IX_ProtocolComputationRecords_TimestampUtc",
        "IX_ProtocolItems_CompoundRecordId",
        "IX_ProtocolItems_ProtocolId",
        "IX_ProtocolPhases_PersonId",
        "IX_ProtocolReviewCompletedEvents_CompletedAtUtc",
        "IX_ProtocolReviewCompletedEvents_ProtocolId",
        "IX_ProtocolReviewCompletedEvents_ProtocolRunId",
        "IX_ProtocolRuns_PersonId",
        "IX_ProtocolRuns_PersonId_Status",
        "IX_ProtocolRuns_ProtocolId",
        "IX_Protocols_EvolvedFromRunId",
        "IX_Protocols_OriginProtocolId",
        "IX_Protocols_ParentProtocolId",
        "IX_Protocols_PersonId",
        "IX_Protocols_PersonId_OriginProtocolId_Version",
        "IX_Sessions_ExpiresAtUtc",
        "IX_Sessions_TokenHash",
        "IX_Sessions_UserId",
        "IX_StripeWebhookEvents_StripeEventId",
        "IX_Subscriptions_AppUserId",
        "IX_Subscriptions_StripeCustomerId",
        "IX_Subscriptions_StripeSubscriptionId",
        "IX_TimelineEvents_OccurredAtUtc",
        "IX_TimelineEvents_PersonId",
    ];

    public static async Task ReconcileAsync(
        BioStackDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var createHistory = connection.CreateCommand())
        {
            createHistory.Transaction = transaction;
            createHistory.CommandText = """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """;
            await createHistory.ExecuteNonQueryAsync(cancellationToken);
        }

        var appliedMigrations = await ReadSingleColumnAsync(
            connection,
            transaction,
            "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";",
            cancellationToken);
        var missingBaselineMigrations = BaselineMigrationIds
            .Where(migrationId => !appliedMigrations.Contains(migrationId))
            .ToArray();
        if (missingBaselineMigrations.Length == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var tables = await ReadSingleColumnAsync(
            connection,
            transaction,
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';",
            cancellationToken);
        var columns = await ReadPairsAsync(
            connection,
            transaction,
            "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema = 'public';",
            cancellationToken);
        var indexes = await ReadSingleColumnAsync(
            connection,
            transaction,
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public';",
            cancellationToken);

        var missing = FindMissingSchemaObjects(tables, columns, indexes);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Production migration history is missing approved baseline rows, but the legacy schema does not match that baseline. " +
                $"Missing: {string.Join(", ", missing)}");
        }

        foreach (var migrationId in missingBaselineMigrations)
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES (@migrationId, '10.0.0')
                ON CONFLICT ("MigrationId") DO NOTHING;
                """;
            var parameter = insert.CreateParameter();
            parameter.ParameterName = "migrationId";
            parameter.Value = migrationId;
            insert.Parameters.Add(parameter);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogWarning(
            "Reconciled {MigrationCount} missing production migration-history row after validating {TableCount} original tables and {IndexCount} original indexes. Existing history rows were preserved; baseline ends at {MigrationId}; every later migration remains pending.",
            missingBaselineMigrations.Length,
            RequiredTables.Length,
            RequiredIndexes.Length,
            BaselineMigrationIds[^1]);
    }

    public static IReadOnlyList<string> FindMissingSchemaObjects(
        IReadOnlySet<string> tables,
        IReadOnlySet<(string Table, string Column)> columns,
        IReadOnlySet<string> indexes)
    {
        var missing = new List<string>();
        missing.AddRange(RequiredTables.Where(table => !tables.Contains(table)).Select(table => $"table:{table}"));
        missing.AddRange(RequiredColumns.Where(column => !columns.Contains(column)).Select(column => $"column:{column.Table}.{column.Column}"));
        missing.AddRange(RequiredIndexes.Where(index => !indexes.Contains(index)).Select(index => $"index:{index}"));
        return missing;
    }

    private static async Task<HashSet<string>> ReadSingleColumnAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<HashSet<(string Table, string Column)>> ReadPairsAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        var values = new HashSet<(string Table, string Column)>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add((reader.GetString(0), reader.GetString(1)));
        }

        return values;
    }
}
