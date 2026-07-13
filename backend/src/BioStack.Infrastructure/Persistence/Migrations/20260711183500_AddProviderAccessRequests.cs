using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260711183500_AddProviderAccessRequests")]
public partial class AddProviderAccessRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProviderAccessRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 255, nullable: false),
                Name = table.Column<string>(maxLength: 160, nullable: false),
                Organization = table.Column<string>(maxLength: 200, nullable: false),
                Role = table.Column<string>(maxLength: 120, nullable: false),
                Status = table.Column<string>(maxLength: 32, nullable: false),
                Owner = table.Column<string>(maxLength: 160, nullable: true),
                ConsentVersion = table.Column<string>(maxLength: 64, nullable: false),
                ConsentRecordedAtUtc = table.Column<DateTime>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProviderAccessRequests", item => item.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProviderAccessRequests_Email",
            table: "ProviderAccessRequests",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProviderAccessRequests_Status_Owner_CreatedAtUtc",
            table: "ProviderAccessRequests",
            columns: new[] { "Status", "Owner", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProviderAccessRequests");
    }
}
