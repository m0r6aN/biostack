namespace BioStack.Infrastructure.Persistence;

public sealed record CriticalPostgresColumn(
    string Table,
    string Column,
    bool IsNullable,
    bool IsLegacyBaselineColumn,
    IReadOnlySet<string> ExpectedUdtNames,
    IReadOnlySet<string> RepairableLegacyUdtNames);

public sealed record PostgresColumnSchema(string UdtName, bool IsNullable);

/// <summary>
/// The PostgreSQL storage contract for authentication- and billing-critical tables.
/// PostgreSQL reports its internal type names through information_schema.columns.udt_name.
/// </summary>
public static class CriticalPostgresSchemaContract
{
    public static IReadOnlyList<CriticalPostgresColumn> Columns { get; } =
    [
        // AppUsers (consent columns were added after the legacy baseline migration).
        Uuid("AppUsers", "Id"),
        Text("AppUsers", "ProviderKey"),
        Text("AppUsers", "Provider"),
        Text("AppUsers", "Email"),
        Text("AppUsers", "DisplayName"),
        Text("AppUsers", "AvatarUrl", nullable: true),
        Text("AppUsers", "StripeCustomerId"),
        Integer("AppUsers", "Role"),
        Timestamp("AppUsers", "CreatedAtUtc"),
        Timestamp("AppUsers", "LastSeenAtUtc"),
        Timestamp("AppUsers", "ConsentAcceptedAtUtc", nullable: true, legacyBaseline: false),
        Text("AppUsers", "ConsentVersion", nullable: true, legacyBaseline: false),
        Timestamp("AppUsers", "ConsentDeclinedAtUtc", nullable: true, legacyBaseline: false),
        Text("AppUsers", "ConsentDeclinedVersion", nullable: true, legacyBaseline: false),

        // AppUsers.Id cannot be changed while the legacy profile-owner FK remains.
        Uuid("PersonProfiles", "OwnerId", nullable: true),

        // AuthIdentities.
        Uuid("AuthIdentities", "Id"),
        Uuid("AuthIdentities", "UserId"),
        Text("AuthIdentities", "Type"),
        Text("AuthIdentities", "ValueNormalized"),
        Boolean("AuthIdentities", "IsVerified"),
        Timestamp("AuthIdentities", "CreatedAtUtc"),
        Timestamp("AuthIdentities", "VerifiedAtUtc", nullable: true),

        // AuthChallenges.
        Uuid("AuthChallenges", "Id"),
        Uuid("AuthChallenges", "IdentityId"),
        Text("AuthChallenges", "Channel"),
        Text("AuthChallenges", "ChallengeType"),
        Text("AuthChallenges", "TokenHash"),
        Timestamp("AuthChallenges", "ExpiresAtUtc"),
        Timestamp("AuthChallenges", "ConsumedAtUtc", nullable: true),
        Timestamp("AuthChallenges", "CreatedAtUtc"),
        Integer("AuthChallenges", "AttemptCount"),
        Text("AuthChallenges", "IpAddress", nullable: true),
        Text("AuthChallenges", "RedirectPath"),

        // Sessions.
        Uuid("Sessions", "Id"),
        Uuid("Sessions", "UserId"),
        Text("Sessions", "TokenHash"),
        Timestamp("Sessions", "CreatedAtUtc"),
        Timestamp("Sessions", "ExpiresAtUtc"),
        Timestamp("Sessions", "RevokedAtUtc", nullable: true),
        Text("Sessions", "IpAddress", nullable: true),
        Text("Sessions", "UserAgent", nullable: true),

        // Subscriptions.
        Uuid("Subscriptions", "Id"),
        Uuid("Subscriptions", "AppUserId"),
        Text("Subscriptions", "ProductCode"),
        Integer("Subscriptions", "Tier"),
        Integer("Subscriptions", "Provider"),
        Text("Subscriptions", "StripeCustomerId"),
        Text("Subscriptions", "StripeSubscriptionId"),
        Text("Subscriptions", "StripePriceId", nullable: true),
        Integer("Subscriptions", "Status"),
        Timestamp("Subscriptions", "CurrentPeriodStartUtc", nullable: true),
        Timestamp("Subscriptions", "CurrentPeriodEndUtc", nullable: true),
        Boolean("Subscriptions", "CancelAtPeriodEnd"),
        Timestamp("Subscriptions", "CreatedAtUtc"),
        Timestamp("Subscriptions", "UpdatedAtUtc"),

        // StripeWebhookEvents (lifecycle columns were added after the legacy baseline).
        Uuid("StripeWebhookEvents", "Id"),
        Text("StripeWebhookEvents", "StripeEventId"),
        Text("StripeWebhookEvents", "EventType"),
        Text("StripeWebhookEvents", "ProcessingStatus", legacyBaseline: false),
        Text("StripeWebhookEvents", "FailureCode", nullable: true, legacyBaseline: false),
        Integer("StripeWebhookEvents", "AttemptCount", legacyBaseline: false),
        Timestamp("StripeWebhookEvents", "LastAttemptAtUtc", legacyBaseline: false),
        Timestamp("StripeWebhookEvents", "ProcessedAtUtc"),
    ];

    public static IReadOnlyList<string> FindProblems(
        IReadOnlyDictionary<(string Table, string Column), PostgresColumnSchema> actualColumns,
        bool legacyBaselineMode = false)
    {
        ArgumentNullException.ThrowIfNull(actualColumns);

        var problems = new List<string>();
        var requiredColumns = legacyBaselineMode
            ? Columns.Where(column => column.IsLegacyBaselineColumn)
            : Columns;

        foreach (var expected in requiredColumns)
        {
            var key = (expected.Table, expected.Column);
            if (!actualColumns.TryGetValue(key, out var actual))
            {
                problems.Add($"column:{expected.Table}.{expected.Column}");
                continue;
            }

            var acceptedTypes = legacyBaselineMode
                ? expected.ExpectedUdtNames.Concat(expected.RepairableLegacyUdtNames).ToHashSet(StringComparer.Ordinal)
                : expected.ExpectedUdtNames;

            if (!acceptedTypes.Contains(actual.UdtName))
            {
                problems.Add(
                    $"type:{expected.Table}.{expected.Column}={actual.UdtName};expected={string.Join('|', acceptedTypes.Order(StringComparer.Ordinal))}");
            }

            if (actual.IsNullable != expected.IsNullable)
            {
                problems.Add(
                    $"nullability:{expected.Table}.{expected.Column}={(actual.IsNullable ? "nullable" : "not-null")};expected={(expected.IsNullable ? "nullable" : "not-null")}");
            }
        }

        return problems;
    }

    private static CriticalPostgresColumn Uuid(
        string table,
        string column,
        bool nullable = false,
        bool legacyBaseline = true) =>
        Column(table, column, nullable, legacyBaseline, ["uuid"], ["text", "varchar"]);

    private static CriticalPostgresColumn Timestamp(
        string table,
        string column,
        bool nullable = false,
        bool legacyBaseline = true) =>
        Column(table, column, nullable, legacyBaseline, ["timestamptz"], ["text", "varchar", "timestamp"]);

    private static CriticalPostgresColumn Boolean(
        string table,
        string column,
        bool nullable = false,
        bool legacyBaseline = true) =>
        Column(table, column, nullable, legacyBaseline, ["bool"], ["int2", "int4", "int8", "text", "varchar"]);

    private static CriticalPostgresColumn Integer(
        string table,
        string column,
        bool nullable = false,
        bool legacyBaseline = true) =>
        Column(table, column, nullable, legacyBaseline, ["int4"], []);

    private static CriticalPostgresColumn Text(
        string table,
        string column,
        bool nullable = false,
        bool legacyBaseline = true) =>
        Column(table, column, nullable, legacyBaseline, ["text", "varchar"], []);

    private static CriticalPostgresColumn Column(
        string table,
        string column,
        bool nullable,
        bool legacyBaseline,
        string[] expectedTypes,
        string[] repairableLegacyTypes) =>
        new(
            table,
            column,
            nullable,
            legacyBaseline,
            expectedTypes.ToHashSet(StringComparer.Ordinal),
            repairableLegacyTypes.ToHashSet(StringComparer.Ordinal));
}
