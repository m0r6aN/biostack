using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BioStack.Api;

public static class ProductionMigrationBaselineConfiguration
{
    public static void Configure(DbContextOptionsBuilder options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Migrations in this repository are intentionally hand-written and the central
        // snapshot is intentionally minimal. Keep the mismatch observable in production,
        // but do not let EF's default escalation prevent those migrations from running.
        options.ConfigureWarnings(warnings =>
            warnings.Log(RelationalEventId.PendingModelChangesWarning));
    }
}
