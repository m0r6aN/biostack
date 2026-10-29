using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Consent v2: strictly additive. One new table, ConsentEvents (append-only history of accepted or declined
// consent, user id cascades with the account), so accepting bio-observational-v2 no longer erases proof of an
// earlier acceptance.
//
// Backfill (PostgreSQL only): each existing latest-value record on AppUsers becomes one event, so v1
// acceptances already on file survive the bump. It is an INSERT ... SELECT into the new, empty table; no
// existing row is read-modified or rewritten. Ids use md5(random()) cast to uuid, which works on every
// PostgreSQL version (no extension, no gen_random_uuid dependency). The previous release ignores the table and
// keeps working; its own accepts after this migration are not mirrored, which is acceptable because the
// release that follows (v2) writes events itself and treats an earlier version as stale anyway.
// Hand-written per this repository's migration pattern (central model snapshot intentionally left untouched;
// see 20260908000000_AddProviderAccessAuditEntries and 20261029120000_AddAccountDeletionRecords).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261029130000_AddConsentEvents")]
public partial class AddConsentEvents : Migration
{
    internal const string BackfillAcceptedSql = """
        INSERT INTO "ConsentEvents" ("Id", "UserId", "EventType", "Version", "OccurredAtUtc")
        SELECT md5(random()::text || clock_timestamp()::text || "Id"::text)::uuid, "Id", 'accepted', "ConsentVersion", "ConsentAcceptedAtUtc"
        FROM "AppUsers"
        WHERE "ConsentAcceptedAtUtc" IS NOT NULL AND "ConsentVersion" IS NOT NULL AND "ConsentVersion" <> '';
        """;

    internal const string BackfillDeclinedSql = """
        INSERT INTO "ConsentEvents" ("Id", "UserId", "EventType", "Version", "OccurredAtUtc")
        SELECT md5(random()::text || clock_timestamp()::text || "Id"::text)::uuid, "Id", 'declined', "ConsentDeclinedVersion", "ConsentDeclinedAtUtc"
        FROM "AppUsers"
        WHERE "ConsentDeclinedAtUtc" IS NOT NULL AND "ConsentDeclinedVersion" IS NOT NULL AND "ConsentDeclinedVersion" <> '';
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConsentEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                EventType = table.Column<string>(maxLength: 16, nullable: false),
                Version = table.Column<string>(maxLength: 64, nullable: false),
                OccurredAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsentEvents", item => item.Id);
                table.ForeignKey(
                    name: "FK_ConsentEvents_AppUsers_UserId",
                    column: item => item.UserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConsentEvents_UserId_OccurredAtUtc",
            table: "ConsentEvents",
            columns: new[] { "UserId", "OccurredAtUtc" });

        if (migrationBuilder.ActiveProvider != null && migrationBuilder.ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql(BackfillAcceptedSql);
            migrationBuilder.Sql(BackfillDeclinedSql);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConsentEvents");
    }
}
