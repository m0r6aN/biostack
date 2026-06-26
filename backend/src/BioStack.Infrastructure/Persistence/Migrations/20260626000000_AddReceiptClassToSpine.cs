using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptClassToSpine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOT NULL with a sentinel default so existing spine rows (written before the receipt
            // taxonomy) are backfilled to "legacy.unclassified" rather than violating the constraint.
            migrationBuilder.AddColumn<string>(
                name: "ReceiptClass",
                table: "SpineEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "legacy.unclassified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptClass",
                table: "SpineEntries");
        }
    }
}
