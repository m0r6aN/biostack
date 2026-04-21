namespace BioStack.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

public static class InteractionSchemaBootstrapper
{
    public static async Task EnsureCompoundInteractionHintsTableAsync(BioStackDbContext db, CancellationToken cancellationToken = default)
    {
        var provider = db.Database.ProviderName ?? string.Empty;

        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "CompoundInteractionHints" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_CompoundInteractionHints" PRIMARY KEY,
                    "CompoundA" TEXT NOT NULL,
                    "CompoundB" TEXT NOT NULL,
                    "InteractionType" INTEGER NOT NULL,
                    "Strength" NUMERIC NOT NULL,
                    "MechanismOverlap" TEXT NULL,
                    "Notes" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_CompoundInteractionHints_CompoundA_CompoundB"
                    ON "CompoundInteractionHints" ("CompoundA", "CompoundB");
                """,
                cancellationToken);
            return;
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "CompoundInteractionHints" (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CompoundA" text NOT NULL,
                "CompoundB" text NOT NULL,
                "InteractionType" integer NOT NULL,
                "Strength" numeric(3,2) NOT NULL,
                "MechanismOverlap" text NULL,
                "Notes" text NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_CompoundInteractionHints_CompoundA_CompoundB"
                ON "CompoundInteractionHints" ("CompoundA", "CompoundB");
            """,
            cancellationToken);
    }
}
