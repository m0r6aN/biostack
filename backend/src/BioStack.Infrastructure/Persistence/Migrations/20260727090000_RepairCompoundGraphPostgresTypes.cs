namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260727090000_RepairCompoundGraphPostgresTypes")]
public sealed class RepairCompoundGraphPostgresTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        // AddCompoundGraphArtifacts was hand-authored with explicit SQLite type names.
        // PostgreSQL accepted those names, but EF/Npgsql queries expect native uuid, boolean,
        // and timestamptz values. Repair only mismatched columns so fresh or manually repaired
        // databases remain safe, and keep the conversion transactional.
        migrationBuilder.Sql(
            """
            DO $repair$
            BEGIN
                ALTER TABLE "CompoundGraphRelationships"
                    DROP CONSTRAINT IF EXISTS "FK_CompoundGraphRelationships_CompoundGraphArtifacts_GraphArtifactId";
                ALTER TABLE "CompoundGraphFindings"
                    DROP CONSTRAINT IF EXISTS "FK_CompoundGraphFindings_CompoundGraphArtifacts_GraphArtifactId";

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphArtifacts'
                      AND column_name = 'Id'
                      AND udt_name <> 'uuid'
                ) THEN
                    ALTER TABLE "CompoundGraphArtifacts"
                        ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphArtifacts'
                      AND column_name = 'GeneratedAtUtc'
                      AND udt_name <> 'timestamptz'
                ) THEN
                    ALTER TABLE "CompoundGraphArtifacts"
                        ALTER COLUMN "GeneratedAtUtc" TYPE timestamp with time zone
                        USING CASE
                            WHEN trim("GeneratedAtUtc"::text) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$'
                                THEN trim("GeneratedAtUtc"::text)::timestamp with time zone
                            ELSE trim("GeneratedAtUtc"::text)::timestamp without time zone AT TIME ZONE 'UTC'
                        END;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphArtifacts'
                      AND column_name = 'IsActive'
                      AND udt_name <> 'bool'
                ) THEN
                    IF EXISTS (
                        SELECT 1 FROM "CompoundGraphArtifacts"
                        WHERE lower(trim("IsActive"::text)) NOT IN ('0', '1', 'false', 'true', 'f', 't')
                    ) THEN
                        RAISE EXCEPTION 'CompoundGraphArtifacts.IsActive contains an unrecognized boolean value';
                    END IF;

                    ALTER TABLE "CompoundGraphArtifacts"
                        ALTER COLUMN "IsActive" TYPE boolean
                        USING CASE
                            WHEN lower(trim("IsActive"::text)) IN ('1', 'true', 't') THEN TRUE
                            WHEN lower(trim("IsActive"::text)) IN ('0', 'false', 'f') THEN FALSE
                        END;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphArtifacts'
                      AND column_name = 'CreatedAtUtc'
                      AND udt_name <> 'timestamptz'
                ) THEN
                    ALTER TABLE "CompoundGraphArtifacts"
                        ALTER COLUMN "CreatedAtUtc" TYPE timestamp with time zone
                        USING CASE
                            WHEN trim("CreatedAtUtc"::text) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$'
                                THEN trim("CreatedAtUtc"::text)::timestamp with time zone
                            ELSE trim("CreatedAtUtc"::text)::timestamp without time zone AT TIME ZONE 'UTC'
                        END;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphRelationships'
                      AND column_name = 'Id'
                      AND udt_name <> 'uuid'
                ) THEN
                    ALTER TABLE "CompoundGraphRelationships"
                        ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphRelationships'
                      AND column_name = 'GraphArtifactId'
                      AND udt_name <> 'uuid'
                ) THEN
                    ALTER TABLE "CompoundGraphRelationships"
                        ALTER COLUMN "GraphArtifactId" TYPE uuid USING "GraphArtifactId"::uuid;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphRelationships'
                      AND column_name = 'NeedsReview'
                      AND udt_name <> 'bool'
                ) THEN
                    IF EXISTS (
                        SELECT 1 FROM "CompoundGraphRelationships"
                        WHERE lower(trim("NeedsReview"::text)) NOT IN ('0', '1', 'false', 'true', 'f', 't')
                    ) THEN
                        RAISE EXCEPTION 'CompoundGraphRelationships.NeedsReview contains an unrecognized boolean value';
                    END IF;

                    ALTER TABLE "CompoundGraphRelationships"
                        ALTER COLUMN "NeedsReview" TYPE boolean
                        USING CASE
                            WHEN lower(trim("NeedsReview"::text)) IN ('1', 'true', 't') THEN TRUE
                            WHEN lower(trim("NeedsReview"::text)) IN ('0', 'false', 'f') THEN FALSE
                        END;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphRelationships'
                      AND column_name = 'CreatedAtUtc'
                      AND udt_name <> 'timestamptz'
                ) THEN
                    ALTER TABLE "CompoundGraphRelationships"
                        ALTER COLUMN "CreatedAtUtc" TYPE timestamp with time zone
                        USING CASE
                            WHEN trim("CreatedAtUtc"::text) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$'
                                THEN trim("CreatedAtUtc"::text)::timestamp with time zone
                            ELSE trim("CreatedAtUtc"::text)::timestamp without time zone AT TIME ZONE 'UTC'
                        END;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphFindings'
                      AND column_name = 'Id'
                      AND udt_name <> 'uuid'
                ) THEN
                    ALTER TABLE "CompoundGraphFindings"
                        ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphFindings'
                      AND column_name = 'GraphArtifactId'
                      AND udt_name <> 'uuid'
                ) THEN
                    ALTER TABLE "CompoundGraphFindings"
                        ALTER COLUMN "GraphArtifactId" TYPE uuid USING "GraphArtifactId"::uuid;
                END IF;

                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name = 'CompoundGraphFindings'
                      AND column_name = 'CreatedAtUtc'
                      AND udt_name <> 'timestamptz'
                ) THEN
                    ALTER TABLE "CompoundGraphFindings"
                        ALTER COLUMN "CreatedAtUtc" TYPE timestamp with time zone
                        USING CASE
                            WHEN trim("CreatedAtUtc"::text) ~* '(Z|[+-][0-9]{2}(:?[0-9]{2})?)$'
                                THEN trim("CreatedAtUtc"::text)::timestamp with time zone
                            ELSE trim("CreatedAtUtc"::text)::timestamp without time zone AT TIME ZONE 'UTC'
                        END;
                END IF;

                ALTER TABLE "CompoundGraphRelationships"
                    ADD CONSTRAINT "FK_CompoundGraphRelationships_CompoundGraphArtifacts_GraphArtifactId"
                    FOREIGN KEY ("GraphArtifactId")
                    REFERENCES "CompoundGraphArtifacts" ("Id")
                    ON DELETE CASCADE;

                ALTER TABLE "CompoundGraphFindings"
                    ADD CONSTRAINT "FK_CompoundGraphFindings_CompoundGraphArtifacts_GraphArtifactId"
                    FOREIGN KEY ("GraphArtifactId")
                    REFERENCES "CompoundGraphArtifacts" ("Id")
                    ON DELETE CASCADE;
            END
            $repair$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Forward-only production repair. Restoring the SQLite-specific PostgreSQL types
        // would reintroduce the outage, so rollback intentionally leaves the corrected schema.
    }
}
