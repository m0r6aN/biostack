namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

// BIO-PAIRWISE-004 / PW-004-A1: adds the provenance surface to CompoundInteractionHint. Both
// columns are additive and nullable/defaulted so the fourteen existing rows remain valid without
// backfill -- IsSourced defaults to false (unsourced), matching the 2026-09-17 owner ratification
// that those rows carry no citation and must be quarantined from public rendering and evidence
// labeling while continuing to serve the internal score. Hand-written per this repository's
// established migration pattern (central model snapshot intentionally left untouched; see
// ProductionMigrationBaselineConfiguration). IsSourced is left without an explicit provider type
// so EF maps it to each provider's native boolean representation (precedent:
// 20260828090000_AddPasskeyAuthentication's IsBackupEligible/IsBackedUp columns); SourceReference
// specifies type: "TEXT" explicitly so SQLite and PostgreSQL round-trip identically, matching the
// precedent set by 20260626000000_AddReceiptClassToSpine.ReceiptClass.
[DbContext(typeof(BioStackDbContext))]
[Migration("20260830000000_AddCompoundInteractionHintProvenance")]
public sealed class AddCompoundInteractionHintProvenance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsSourced",
            table: "CompoundInteractionHints",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "SourceReference",
            table: "CompoundInteractionHints",
            type: "TEXT",
            maxLength: 2048,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SourceReference",
            table: "CompoundInteractionHints");

        migrationBuilder.DropColumn(
            name: "IsSourced",
            table: "CompoundInteractionHints");
    }
}
