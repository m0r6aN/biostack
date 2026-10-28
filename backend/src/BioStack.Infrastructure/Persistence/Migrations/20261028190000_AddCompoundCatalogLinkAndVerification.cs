using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Strictly additive (Everything I'm Taking, phase 1): four columns on CompoundRecords.
//  - CatalogSlug, VerificationState, CategorySource: nullable, no default, no backfill. Existing rows
//    keep all three NULL, which the application reads as "not linked to the catalog, unverified,
//    category entered by the user" (so they keep counting toward the Observer cap exactly as before).
//    BioStack never auto-links a legacy row; only a user-confirmed pick sets CatalogSlug.
//  - IncludeInProviderSummary: NOT NULL DEFAULT TRUE (the per-item provider-summary toggle). The
//    database default means the previous release, which does not know the column, keeps inserting rows
//    without it and they land as included, so this stays backward-compatible for one release.
// No data is rewritten and nothing is dropped. Hand-written per this repository's migration pattern
// (central model snapshot intentionally left untouched; see 20261024000000, 20261025020000 and
// 20261028150000). Lengths match BioStackDbContext.
[DbContext(typeof(BioStackDbContext))]
[Migration("20261028190000_AddCompoundCatalogLinkAndVerification")]
public partial class AddCompoundCatalogLinkAndVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "CatalogSlug", table: "CompoundRecords", maxLength: 160, nullable: true);
        migrationBuilder.AddColumn<string>(name: "VerificationState", table: "CompoundRecords", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CategorySource", table: "CompoundRecords", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IncludeInProviderSummary", table: "CompoundRecords", nullable: false, defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IncludeInProviderSummary", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "CategorySource", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "VerificationState", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "CatalogSlug", table: "CompoundRecords");
    }
}
