namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260828000000_AddProfileGoals")]
public sealed class AddProfileGoals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProfileGoals",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                ProfileId = table.Column<Guid>(nullable: false),
                GoalDefinitionId = table.Column<string>(maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProfileGoals", goal => goal.Id);
                table.ForeignKey(
                    name: "FK_ProfileGoals_PersonProfiles_ProfileId",
                    column: goal => goal.ProfileId,
                    principalTable: "PersonProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProfileGoals_ProfileId_GoalDefinitionId",
            table: "ProfileGoals",
            columns: new[] { "ProfileId", "GoalDefinitionId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProfileGoals");
    }
}
