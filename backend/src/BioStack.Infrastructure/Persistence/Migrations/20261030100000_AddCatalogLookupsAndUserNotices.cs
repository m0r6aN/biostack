using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Everything I'm Taking, phase 3: strictly additive. Two new, empty tables and nothing else.
//   CatalogLookups: one internal catalog lookup per user per normalized name (user id and profile id cascade with
//                   the account; the link to the user's item is SET NULL so the lookup outlives a deleted item).
//   UserNotices:    the minimal in-app inbox (user id cascades with the account).
// No existing table or column is touched, so the previous release keeps working and ignores both tables. Hand-written
// per this repository's migration pattern (central model snapshot intentionally left untouched; see
// 20261029130000_AddConsentEvents).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261030100000_AddCatalogLookupsAndUserNotices")]
public partial class AddCatalogLookupsAndUserNotices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CatalogLookups",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                PersonId = table.Column<Guid>(nullable: false),
                CompoundRecordId = table.Column<Guid>(nullable: true),
                NameKey = table.Column<string>(maxLength: 120, nullable: false),
                QueryName = table.Column<string>(maxLength: 60, nullable: false),
                Status = table.Column<string>(maxLength: 32, nullable: false),
                MatchSlug = table.Column<string>(maxLength: 255, nullable: true),
                MatchKind = table.Column<string>(maxLength: 32, nullable: true),
                MatchLabel = table.Column<string>(maxLength: 16, nullable: true),
                MatchScore = table.Column<float>(nullable: true),
                CandidatesJson = table.Column<string>(nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                CompletedAtUtc = table.Column<DateTime>(nullable: true),
                LastCheckedAtUtc = table.Column<DateTime>(nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CatalogLookups", item => item.Id);
                table.ForeignKey(
                    name: "FK_CatalogLookups_AppUsers_UserId",
                    column: item => item.UserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CatalogLookups_PersonProfiles_PersonId",
                    column: item => item.PersonId,
                    principalTable: "PersonProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CatalogLookups_CompoundRecords_CompoundRecordId",
                    column: item => item.CompoundRecordId,
                    principalTable: "CompoundRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CatalogLookups_UserId_NameKey",
            table: "CatalogLookups",
            columns: new[] { "UserId", "NameKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CatalogLookups_PersonId_Status",
            table: "CatalogLookups",
            columns: new[] { "PersonId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_CatalogLookups_CompoundRecordId",
            table: "CatalogLookups",
            column: "CompoundRecordId");

        migrationBuilder.CreateTable(
            name: "UserNotices",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                Kind = table.Column<string>(maxLength: 48, nullable: false),
                TitleKey = table.Column<string>(maxLength: 96, nullable: false),
                ParamsJson = table.Column<string>(nullable: false),
                ActionPath = table.Column<string>(maxLength: 200, nullable: true),
                RelatedId = table.Column<Guid>(nullable: true),
                DedupeKey = table.Column<string>(maxLength: 160, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false),
                ReadAtUtc = table.Column<DateTime>(nullable: true),
                DismissedAtUtc = table.Column<DateTime>(nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserNotices", item => item.Id);
                table.ForeignKey(
                    name: "FK_UserNotices_AppUsers_UserId",
                    column: item => item.UserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserNotices_UserId_DedupeKey",
            table: "UserNotices",
            columns: new[] { "UserId", "DedupeKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserNotices_UserId_CreatedAtUtc",
            table: "UserNotices",
            columns: new[] { "UserId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserNotices");
        migrationBuilder.DropTable(name: "CatalogLookups");
    }
}
