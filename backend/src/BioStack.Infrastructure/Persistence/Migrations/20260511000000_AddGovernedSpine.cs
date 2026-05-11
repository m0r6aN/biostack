using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernedSpine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpineEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiptUri = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectUri = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Decision = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyHashValue = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyHashVersion = table.Column<string>(type: "TEXT", nullable: false),
                    InputHash = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceRefsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    EffectStatus = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpineEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpineEntries_ActorId",
                table: "SpineEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_SpineEntries_ReceiptUri",
                table: "SpineEntries",
                column: "ReceiptUri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpineEntries_SubjectUri",
                table: "SpineEntries",
                column: "SubjectUri");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpineEntries");
        }
    }
}
