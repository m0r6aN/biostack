using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Growth Phase 7: strictly additive. Hashed claim-checker query log and "Submit a claim you saw"
// submissions (sanitized text only; no URL, creator, photo or dose columns). Hand-written per
// this repository's migration pattern (central model snapshot intentionally left untouched).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261025010000_AddClaimCheckerAndSubmissions")]
public partial class AddClaimCheckerAndSubmissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ClaimCheckQueries",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                QueryHash = table.Column<string>(maxLength: 64, nullable: false),
                MatchedClaimId = table.Column<string>(maxLength: 200, nullable: true),
                DoseIntent = table.Column<bool>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_ClaimCheckQueries", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_ClaimCheckQueries_CreatedAtUtc", table: "ClaimCheckQueries", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_ClaimCheckQueries_QueryHash", table: "ClaimCheckQueries", column: "QueryHash");

        migrationBuilder.CreateTable(
            name: "ClaimSubmissions",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                CompoundSlug = table.Column<string>(maxLength: 100, nullable: false),
                ClaimText = table.Column<string>(maxLength: 280, nullable: false),
                SeenOn = table.Column<string>(maxLength: 16, nullable: false),
                Experience = table.Column<string>(maxLength: 16, nullable: true),
                ThemeText = table.Column<string>(maxLength: 140, nullable: true),
                Redacted = table.Column<bool>(nullable: false),
                Emergency = table.Column<bool>(nullable: false),
                SubmitterKey = table.Column<string>(maxLength: 64, nullable: false),
                Status = table.Column<string>(maxLength: 16, nullable: false),
                Theme = table.Column<string>(maxLength: 64, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                ModeratedAtUtc = table.Column<DateTime>(nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_ClaimSubmissions", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_ClaimSubmissions_Status_CreatedAtUtc", table: "ClaimSubmissions", columns: new[] { "Status", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_ClaimSubmissions_CompoundSlug_Theme_Status", table: "ClaimSubmissions", columns: new[] { "CompoundSlug", "Theme", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ClaimSubmissions");
        migrationBuilder.DropTable(name: "ClaimCheckQueries");
    }
}
