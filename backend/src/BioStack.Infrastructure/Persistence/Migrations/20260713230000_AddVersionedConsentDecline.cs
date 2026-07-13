namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260713230000_AddVersionedConsentDecline")]
public sealed class AddVersionedConsentDecline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ConsentDeclinedAtUtc",
            table: "AppUsers",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ConsentDeclinedVersion",
            table: "AppUsers",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConsentDeclinedAtUtc", table: "AppUsers");
        migrationBuilder.DropColumn(name: "ConsentDeclinedVersion", table: "AppUsers");
    }
}
