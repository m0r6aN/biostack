namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260713222500_HardenStripeWebhookLifecycle")]
public sealed class HardenStripeWebhookLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AttemptCount",
            table: "StripeWebhookEvents",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "FailureCode",
            table: "StripeWebhookEvents",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastAttemptAtUtc",
            table: "StripeWebhookEvents",
            nullable: false,
            defaultValue: DateTime.UnixEpoch);

        migrationBuilder.AddColumn<string>(
            name: "ProcessingStatus",
            table: "StripeWebhookEvents",
            maxLength: 32,
            nullable: false,
            defaultValue: "processed");

        if (migrationBuilder.ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            // ProcessedAtUtc came from the original SQLite-authored migration and may still
            // be PostgreSQL text. Validate before copying it into the native timestamptz
            // LastAttemptAtUtc column, and interpret offset-less legacy values as UTC.
            migrationBuilder.Sql(
                """
                DO $copy_timestamp$
                DECLARE
                    legacy_value record;
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'StripeWebhookEvents'
                          AND column_name = 'ProcessedAtUtc'
                          AND udt_name <> 'timestamptz'
                    ) THEN
                        FOR legacy_value IN
                            SELECT ctid::text AS row_locator, "ProcessedAtUtc"::text AS value
                            FROM "StripeWebhookEvents"
                        LOOP
                            IF legacy_value.value IS NULL OR btrim(legacy_value.value) = '' THEN
                                RAISE EXCEPTION 'BioStack Stripe migration preflight failed: ProcessedAtUtc is null or blank at row %',
                                    legacy_value.row_locator;
                            END IF;

                            BEGIN
                                IF btrim(legacy_value.value) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$' THEN
                                    PERFORM btrim(legacy_value.value)::timestamp with time zone;
                                ELSE
                                    PERFORM btrim(legacy_value.value)::timestamp without time zone;
                                END IF;
                            EXCEPTION WHEN others THEN
                                RAISE EXCEPTION 'BioStack Stripe migration preflight failed: ProcessedAtUtc is malformed at row %',
                                    legacy_value.row_locator;
                            END;
                        END LOOP;
                    END IF;

                    UPDATE "StripeWebhookEvents"
                    SET "LastAttemptAtUtc" = CASE
                        WHEN btrim("ProcessedAtUtc"::text) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$'
                            THEN btrim("ProcessedAtUtc"::text)::timestamp with time zone
                        ELSE btrim("ProcessedAtUtc"::text)::timestamp without time zone AT TIME ZONE 'UTC'
                    END;
                END
                $copy_timestamp$;
                """);
        }
        else
        {
            migrationBuilder.Sql(
                "UPDATE \"StripeWebhookEvents\" SET \"LastAttemptAtUtc\" = \"ProcessedAtUtc\";");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AttemptCount", table: "StripeWebhookEvents");
        migrationBuilder.DropColumn(name: "FailureCode", table: "StripeWebhookEvents");
        migrationBuilder.DropColumn(name: "LastAttemptAtUtc", table: "StripeWebhookEvents");
        migrationBuilder.DropColumn(name: "ProcessingStatus", table: "StripeWebhookEvents");
    }
}
