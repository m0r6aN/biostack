using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// PR-PROV-001: strictly additive — new append-only audit table for admin provider-access queue
// mutations only. No existing table or column is changed. Hand-written per this repository's
// established migration pattern (central model snapshot intentionally left untouched; see
// 20260830000000_AddCompoundInteractionHintProvenance's precedent comment and
// ProductionMigrationBaselineConfiguration).
[DbContext(typeof(BioStackDbContext))]
[Migration("20260908000000_AddProviderAccessAuditEntries")]
public partial class AddProviderAccessAuditEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProviderAccessAuditEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RequestId = table.Column<Guid>(nullable: false),
                ActorId = table.Column<Guid>(nullable: false),
                FromStatus = table.Column<string>(maxLength: 32, nullable: false),
                ToStatus = table.Column<string>(maxLength: 32, nullable: false),
                FromOwner = table.Column<string>(maxLength: 160, nullable: true),
                ToOwner = table.Column<string>(maxLength: 160, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProviderAccessAuditEntries", item => item.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProviderAccessAuditEntries_RequestId",
            table: "ProviderAccessAuditEntries",
            column: "RequestId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProviderAccessAuditEntries");
    }
}
