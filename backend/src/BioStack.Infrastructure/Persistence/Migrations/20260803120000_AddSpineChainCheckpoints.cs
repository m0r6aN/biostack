using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpineChainCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F3+: signed snapshots of the chain head. The signing key is NOT stored here —
            // only the signature material that can be verified with a key held outside this DB.
            migrationBuilder.CreateTable(
                name: "SpineChainCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SequenceNumber = table.Column<long>(nullable: false),
                    HeadEntryHash = table.Column<string>(type: "TEXT", nullable: false),
                    CheckpointedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    SignatureAlgorithm = table.Column<string>(type: "TEXT", nullable: false),
                    Signature = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpineChainCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpineChainCheckpoints_SequenceNumber",
                table: "SpineChainCheckpoints",
                column: "SequenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SpineChainCheckpoints_CheckpointedAtUtc",
                table: "SpineChainCheckpoints",
                column: "CheckpointedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SpineChainCheckpoints");
        }
    }
}
