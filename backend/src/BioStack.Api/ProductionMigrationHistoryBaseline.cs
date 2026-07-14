using System.Data;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BioStack.Api;

public static class ProductionMigrationHistoryBaseline
{
    internal static readonly string[] BaselineMigrationIds =
    [
        "20260422125251_RecoverBillingTierEnforcement",
        "20260511000000_AddGovernedSpine",
        "20260517000000_AddUserConsent",
        "20260530000000_PR8_AddStagedTranscriptCandidateReviewPersistence",
        "20260607000000_PR14A_AddPromotionTargetToStagedTranscriptCandidateReviews",
        "20260626000000_AddReceiptClassToSpine",
        "20260626100000_AddCompoundGraphArtifacts",
        "20260705000000_PR172_AddSourceLaneLaunchGuardrails",
        "20260711183500_AddProviderAccessRequests",
    ];

    private static readonly string[] RequiredTables =
    [
        "AppUsers", "AuthChallenges", "AuthIdentities", "CheckIns",
        "CompoundGraphArtifacts", "CompoundGraphFindings", "CompoundGraphRelationships",
        "CompoundInteractionHints", "CompoundRecords", "InteractionFlags", "KnowledgeEntries",
        "LeadCaptures", "PersonProfiles", "ProtocolComputationRecords", "ProtocolItems",
        "ProtocolPhases", "ProtocolReviewCompletedEvents", "ProtocolRuns", "Protocols",
        "ProviderAccessRequests", "Sessions", "SpineEntries", "StagedTranscriptCandidateReviews",
        "StripeWebhookEvents", "Subscriptions", "TimelineEvents",
    ];

    private static readonly (string Table, string Column)[] RequiredColumns =
    [
        ("AppUsers", "ConsentAcceptedAtUtc"),
        ("AppUsers", "ConsentVersion"),
        ("SpineEntries", "ReceiptClass"),
        ("StagedTranscriptCandidateReviews", "TargetCanonicalName"),
        ("StagedTranscriptCandidateReviews", "PromotedKnowledgeEntryId"),
        ("StagedTranscriptCandidateReviews", "PromotedAtUtc"),
        ("StagedTranscriptCandidateReviews", "IntakeRequestId"),
    ];

    private static readonly string[] RequiredIndexes =
    [
        "IX_SpineEntries_ActorId",
        "IX_SpineEntries_ReceiptUri",
        "IX_SpineEntries_SubjectUri",
        "IX_StagedTranscriptCandidateReviews_ReviewState",
        "IX_StagedTranscriptCandidateReviews_IntakeRequestId",
        "IX_CompoundGraphArtifacts_ArtifactHash",
        "IX_CompoundGraphArtifacts_IsActive",
        "IX_CompoundGraphRelationships_GraphArtifactId_SubjectSlug",
        "IX_CompoundGraphRelationships_GraphArtifactId_ObjectSlug",
        "IX_CompoundGraphFindings_GraphArtifactId",
        "IX_ProviderAccessRequests_Email",
        "IX_ProviderAccessRequests_Status_Owner_CreatedAtUtc",
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
            "Reconciled {MigrationCount} missing production migration-history rows after validating {TableCount} tables, {ColumnCount} columns, and {IndexCount} indexes. Existing history rows were preserved; baseline ends at {MigrationId}; later migrations remain pending.",
            missingBaselineMigrations.Length,
            RequiredTables.Length,
            RequiredColumns.Length,
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
