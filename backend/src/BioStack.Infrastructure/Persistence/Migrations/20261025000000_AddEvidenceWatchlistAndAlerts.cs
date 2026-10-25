using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Growth Phase 6: strictly additive. Three new tables for the evidence watchlist, alert
// preferences (with one-click unsubscribe tokens) and the alert log / in-app notices. No
// existing table or column changes. Hand-written per this repository's migration pattern
// (central model snapshot intentionally left untouched; see ProductionMigrationBaselineConfiguration).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261025000000_AddEvidenceWatchlistAndAlerts")]
public partial class AddEvidenceWatchlistAndAlerts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EvidenceWatches",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                TargetType = table.Column<string>(maxLength: 16, nullable: false),
                TargetId = table.Column<string>(maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_EvidenceWatches", x => x.Id));
        migrationBuilder.CreateIndex(
            name: "IX_EvidenceWatches_UserId_TargetType_TargetId",
            table: "EvidenceWatches",
            columns: new[] { "UserId", "TargetType", "TargetId" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_EvidenceWatches_TargetType_TargetId",
            table: "EvidenceWatches",
            columns: new[] { "TargetType", "TargetId" });

        migrationBuilder.CreateTable(
            name: "EvidenceAlertPreferences",
            columns: table => new
            {
                UserId = table.Column<Guid>(nullable: false),
                Frequency = table.Column<string>(maxLength: 16, nullable: false),
                EmailEnabled = table.Column<bool>(nullable: false),
                UnsubscribeToken = table.Column<string>(maxLength: 64, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_EvidenceAlertPreferences", x => x.UserId));
        migrationBuilder.CreateIndex(
            name: "IX_EvidenceAlertPreferences_UnsubscribeToken",
            table: "EvidenceAlertPreferences",
            column: "UnsubscribeToken",
            unique: true);

        migrationBuilder.CreateTable(
            name: "EvidenceAlertLog",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                AlertType = table.Column<string>(maxLength: 32, nullable: false),
                CompoundSlug = table.Column<string>(maxLength: 200, nullable: false),
                ClaimId = table.Column<string>(maxLength: 200, nullable: false),
                Version = table.Column<int>(nullable: false),
                OldVerdict = table.Column<string>(maxLength: 32, nullable: false),
                NewVerdict = table.Column<string>(maxLength: 32, nullable: false),
                Reason = table.Column<string>(maxLength: 500, nullable: false),
                Delivery = table.Column<string>(maxLength: 16, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                EmailedAtUtc = table.Column<DateTime>(nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_EvidenceAlertLog", x => x.Id));
        migrationBuilder.CreateIndex(
            name: "IX_EvidenceAlertLog_UserId_ClaimId_Version_AlertType",
            table: "EvidenceAlertLog",
            columns: new[] { "UserId", "ClaimId", "Version", "AlertType" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_EvidenceAlertLog_UserId_CreatedAtUtc",
            table: "EvidenceAlertLog",
            columns: new[] { "UserId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EvidenceAlertLog");
        migrationBuilder.DropTable(name: "EvidenceAlertPreferences");
        migrationBuilder.DropTable(name: "EvidenceWatches");
    }
}
