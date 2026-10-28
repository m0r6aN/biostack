using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Account export and deletion (MHMDA): strictly additive. One new table, AccountDeletionRecords, that
// holds a minimal NON-identifying record per self-serve deletion (random id, UTC day, row total, the
// day the 35-day backup window clears, procedure version). It deliberately has no user key, so it is
// not a cascade target. No existing table or column is changed, nothing is backfilled, and the previous
// release simply never touches the table. Hand-written per this repository's migration pattern (central
// model snapshot intentionally left untouched; see 20260908000000_AddProviderAccessAuditEntries).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261029120000_AddAccountDeletionRecords")]
public partial class AddAccountDeletionRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountDeletionRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                DeletedOnUtc = table.Column<DateTime>(nullable: false),
                RowsAffected = table.Column<int>(nullable: false),
                BackupsAgeOutByUtc = table.Column<DateTime>(nullable: false),
                ProcedureVersion = table.Column<int>(nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AccountDeletionRecords", item => item.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccountDeletionRecords");
    }
}
