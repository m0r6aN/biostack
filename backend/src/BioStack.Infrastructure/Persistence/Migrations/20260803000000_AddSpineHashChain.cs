using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpineHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F3 tamper-evidence. Each entry commits to its predecessor's hash, so altering or
            // removing any Spine row invalidates every row after it.
            //
            // Column type is left to the provider for SequenceNumber: the repository targets both
            // Postgres (bigint) and SQLite (INTEGER), and pinning "TEXT" as the string columns do
            // would silently create a text column for a 64-bit integer on Postgres.
            migrationBuilder.AddColumn<long>(
                name: "SequenceNumber",
                table: "SpineEntries",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "PreviousEntryHash",
                table: "SpineEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "sha256:genesis");

            migrationBuilder.AddColumn<string>(
                name: "EntryHash",
                table: "SpineEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Backfill legacy rows BEFORE the unique indexes exist.
            //
            // Rows written before the chain existed cannot be retro-chained: their hashes were
            // never computed, and inventing them would be exactly the forgery the chain exists to
            // detect. They are instead given deterministic, per-row-unique placeholders derived
            // from ReceiptUri (already unique) so the constraints can be created. Verification
            // will report the first such row as a hash mismatch — that is correct and honest:
            // pre-migration history is not cryptographically verifiable. Treat the migration
            // point as the chain's effective genesis.
            var isNpgsql = migrationBuilder.ActiveProvider?.Contains(
                "Npgsql", StringComparison.OrdinalIgnoreCase) == true;

            if (isNpgsql)
            {
                migrationBuilder.Sql(@"
UPDATE ""SpineEntries"" AS s
SET ""SequenceNumber"" = x.rn - 1,
    ""EntryHash"" = 'sha256:pre-chain:' || s.""ReceiptUri""
FROM (
    SELECT ""Id"", row_number() OVER (ORDER BY ""CreatedAt"", ""Id"") AS rn
    FROM ""SpineEntries""
) AS x
WHERE s.""Id"" = x.""Id"";");
            }
            else
            {
                migrationBuilder.Sql(@"
UPDATE ""SpineEntries""
SET ""SequenceNumber"" = (
        SELECT x.rn - 1
        FROM (
            SELECT ""Id"", row_number() OVER (ORDER BY ""CreatedAt"", ""Id"") AS rn
            FROM ""SpineEntries""
        ) AS x
        WHERE x.""Id"" = ""SpineEntries"".""Id""
    ),
    ""EntryHash"" = 'sha256:pre-chain:' || ""ReceiptUri"";");
            }

            // A sequence slot may be claimed once — this is what makes a forked chain unwritable.
            migrationBuilder.CreateIndex(
                name: "IX_SpineEntries_SequenceNumber",
                table: "SpineEntries",
                column: "SequenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpineEntries_EntryHash",
                table: "SpineEntries",
                column: "EntryHash",
                unique: true);

            // Not unique: adds nothing over unique SequenceNumber, and a shared backfill default
            // would violate it on the second legacy row.
            migrationBuilder.CreateIndex(
                name: "IX_SpineEntries_PreviousEntryHash",
                table: "SpineEntries",
                column: "PreviousEntryHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SpineEntries_PreviousEntryHash",
                table: "SpineEntries");

            migrationBuilder.DropIndex(
                name: "IX_SpineEntries_EntryHash",
                table: "SpineEntries");

            migrationBuilder.DropIndex(
                name: "IX_SpineEntries_SequenceNumber",
                table: "SpineEntries");

            migrationBuilder.DropColumn(
                name: "EntryHash",
                table: "SpineEntries");

            migrationBuilder.DropColumn(
                name: "PreviousEntryHash",
                table: "SpineEntries");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "SpineEntries");
        }
    }
}
