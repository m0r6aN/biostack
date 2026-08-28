namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260828090000_AddPasskeyAuthentication")]
public sealed class AddPasskeyAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PasskeyOperationChallenges",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: true),
                Operation = table.Column<string>(maxLength: 32, nullable: false),
                RequestIdHash = table.Column<string>(maxLength: 64, nullable: false),
                OptionsJson = table.Column<string>(type: "text", nullable: false),
                RedirectPath = table.Column<string>(maxLength: 512, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(nullable: false),
                ConsumedAtUtc = table.Column<DateTime>(nullable: true),
                AttemptCount = table.Column<int>(nullable: false),
                IpAddress = table.Column<string>(maxLength: 128, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasskeyOperationChallenges", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasskeyOperationChallenges_AppUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PasskeyCredentials",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                IdentityId = table.Column<Guid>(nullable: false),
                CredentialId = table.Column<byte[]>(nullable: false),
                PublicKey = table.Column<byte[]>(nullable: false),
                UserHandle = table.Column<byte[]>(nullable: false),
                CredentialType = table.Column<string>(maxLength: 32, nullable: false),
                SignatureCounter = table.Column<long>(nullable: false),
                Transports = table.Column<string>(maxLength: 256, nullable: false),
                AaGuid = table.Column<Guid>(nullable: false),
                IsBackupEligible = table.Column<bool>(nullable: false),
                IsBackedUp = table.Column<bool>(nullable: false),
                DisplayName = table.Column<string>(maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                LastUsedAtUtc = table.Column<DateTime>(nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasskeyCredentials", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasskeyCredentials_AuthIdentities_IdentityId",
                    column: x => x.IdentityId,
                    principalTable: "AuthIdentities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PasskeyOperationChallenges_ExpiresAtUtc",
            table: "PasskeyOperationChallenges",
            column: "ExpiresAtUtc");
        migrationBuilder.CreateIndex(
            name: "IX_PasskeyOperationChallenges_RequestIdHash",
            table: "PasskeyOperationChallenges",
            column: "RequestIdHash",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_PasskeyOperationChallenges_UserId",
            table: "PasskeyOperationChallenges",
            column: "UserId");
        migrationBuilder.CreateIndex(
            name: "IX_PasskeyCredentials_CredentialId",
            table: "PasskeyCredentials",
            column: "CredentialId",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_PasskeyCredentials_IdentityId",
            table: "PasskeyCredentials",
            column: "IdentityId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PasskeyCredentials");
        migrationBuilder.DropTable(name: "PasskeyOperationChallenges");
    }
}
