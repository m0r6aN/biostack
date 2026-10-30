using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Strictly additive: one boolean column, KnowledgeEntries.IsPublic (NOT NULL, default TRUE), so every existing
// row stays public exactly as before. Owner ruling t101u (2026-10-30): an auto-discovered draft is hidden from
// every public read until the two-reviewer AI council approves it. Refresh sets the column for every record on
// each run from the record and the council decision; this migration only hides the drafts that already exist so
// the change takes effect on deploy instead of waiting for the next Refresh.
//
// Data step (PostgreSQL only, one UPDATE, reversible with SET "IsPublic" = TRUE; nothing is deleted or rewritten
// apart from that flag): a row is hidden only when ALL of these hold: it is not retired; its stored Details say it
// was auto-discovered (compoundFamily "unknown (auto-discovered: ...)", which only the discovery draft builder
// writes); and its review status is not "reviewed" or "ai-council-reviewed". Curated records, including ER-100
// (curated, pending review), are untouched. The match is on the serialized Details JSON, so a stored row that
// does not match simply stays public (fails open to today's behaviour).
// Hand-written per this repository's migration pattern (central model snapshot intentionally left untouched;
// see 20261025020000_AddKnowledgeEntryRetirement).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261030120000_AddKnowledgeEntryIsPublic")]
public partial class AddKnowledgeEntryIsPublic : Migration
{
    internal const string HideUnreviewedDraftsSql = """
        UPDATE "KnowledgeEntries"
        SET "IsPublic" = FALSE
        WHERE "RetiredAtUtc" IS NULL
          AND "Details" IS NOT NULL
          AND "Details" LIKE '%"CompoundFamily":"unknown (auto-discovered:%'
          AND "Details" NOT LIKE '%"ReviewStatus":"reviewed"%'
          AND "Details" NOT LIKE '%"ReviewStatus":"ai-council-reviewed"%';
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPublic",
            table: "KnowledgeEntries",
            nullable: false,
            defaultValue: true);

        if (migrationBuilder.ActiveProvider != null && migrationBuilder.ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql(HideUnreviewedDraftsSql);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsPublic", table: "KnowledgeEntries");
    }
}
