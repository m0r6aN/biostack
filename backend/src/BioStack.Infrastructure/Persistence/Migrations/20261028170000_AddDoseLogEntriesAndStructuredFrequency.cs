using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Strictly additive, follow-up to 20261028150000_AddUserEnteredDoseFields:
//  1. DoseLogEntries (new table): per-compound amounts the user says they actually took. Foreign keys
//     cascade from both the profile and the compound, so deleting either removes the logged amounts.
//  2. Six nullable columns on CompoundRecords for the user's structured frequency (kind, every-N-days
//     interval, weekdays, times per week, start date, times of day). Existing rows keep all six NULL,
//     which the application reads as "daily, labelled with the free-text frequency" exactly as before.
// No default, no backfill, no parsing of free text. The previous release ignores the new columns and
// table, so this is backward-compatible for one release. Hand-written per this repository's migration
// pattern (central model snapshot intentionally left untouched; see 20261024000000 and 20261025020000).
// Columns carry no explicit provider type so EF maps native types on PostgreSQL and SQLite alike.
[DbContext(typeof(BioStackDbContext))]
[Migration("20261028170000_AddDoseLogEntriesAndStructuredFrequency")]
public partial class AddDoseLogEntriesAndStructuredFrequency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "FrequencyKind", table: "CompoundRecords", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<int>(name: "FrequencyIntervalDays", table: "CompoundRecords", nullable: true);
        migrationBuilder.AddColumn<string>(name: "FrequencyWeekdays", table: "CompoundRecords", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<int>(name: "FrequencyTimesPerWeek", table: "CompoundRecords", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "FrequencyStartDate", table: "CompoundRecords", nullable: true);
        migrationBuilder.AddColumn<string>(name: "FrequencyTimesOfDay", table: "CompoundRecords", maxLength: 60, nullable: true);

        migrationBuilder.CreateTable(
            name: "DoseLogEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                PersonId = table.Column<Guid>(nullable: false),
                CompoundRecordId = table.Column<Guid>(nullable: false),
                LogDate = table.Column<DateTime>(nullable: false),
                TakenAtUtc = table.Column<DateTime>(nullable: false),
                Amount = table.Column<decimal>(precision: 18, scale: 4, nullable: false),
                Unit = table.Column<string>(maxLength: 16, nullable: false),
                Note = table.Column<string>(maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DoseLogEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_DoseLogEntries_PersonProfiles_PersonId",
                    column: x => x.PersonId,
                    principalTable: "PersonProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DoseLogEntries_CompoundRecords_CompoundRecordId",
                    column: x => x.CompoundRecordId,
                    principalTable: "CompoundRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_DoseLogEntries_PersonId_LogDate", table: "DoseLogEntries", columns: new[] { "PersonId", "LogDate" });
        migrationBuilder.CreateIndex(name: "IX_DoseLogEntries_CompoundRecordId", table: "DoseLogEntries", column: "CompoundRecordId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DoseLogEntries");
        migrationBuilder.DropColumn(name: "FrequencyTimesOfDay", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "FrequencyStartDate", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "FrequencyTimesPerWeek", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "FrequencyWeekdays", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "FrequencyIntervalDays", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "FrequencyKind", table: "CompoundRecords");
    }
}
