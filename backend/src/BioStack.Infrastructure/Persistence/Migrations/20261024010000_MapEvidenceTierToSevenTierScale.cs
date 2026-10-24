using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Data-only: KnowledgeEntries.EvidenceTier moves from the legacy 4-step scale
// (1 Limited, 2 Moderate, 3 Strong, 4 Mechanistic) to the 7-tier methodology scale
// (10 Established ... 16 Hype Exceeds Evidence). Rows are mapped by strength only;
// the next Refresh replaces every value with the claim-level regrade from the seed
// (tools/regrade/regrade_7_tier.py). Hand-written; snapshot untouched per repo pattern.
[DbContext(typeof(BioStackDbContext))]
[Migration("20261024010000_MapEvidenceTierToSevenTierScale")]
public partial class MapEvidenceTierToSevenTierScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "KnowledgeEntries" SET "EvidenceTier" = CASE "EvidenceTier"
                WHEN 3 THEN 10
                WHEN 2 THEN 12
                WHEN 1 THEN 11
                WHEN 4 THEN 11
                ELSE "EvidenceTier" END
            WHERE "EvidenceTier" BETWEEN 1 AND 4;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "KnowledgeEntries" SET "EvidenceTier" = CASE "EvidenceTier"
                WHEN 10 THEN 3
                WHEN 12 THEN 2
                WHEN 11 THEN 1
                WHEN 13 THEN 1
                ELSE 0 END
            WHERE "EvidenceTier" >= 10;
            """);
    }
}
