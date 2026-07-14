namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260713222500_HardenStripeWebhookLifecycle")]
public sealed class HardenStripeWebhookLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AttemptCount",
            table: "StripeWebhookEvents",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "FailureCode",
            table: "StripeWebhookEvents",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastAttemptAtUtc",
            table: "StripeWebhookEvents",
            nullable: false,
            defaultValue: DateTime.UnixEpoch);

        migrationBuilder.AddColumn<string>(
            name: "ProcessingStatus",
            table: "StripeWebhookEvents",
            maxLength: 32,
            nullable: false,
            defaultValue: "processed");

        migrationBuilder.Sql(
            "UPDATE \"StripeWebhookEvents\" SET \"LastAttemptAtUtc\" = \"ProcessedAtUtc\";");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AttemptCount", table: "StripeWebhookEvents");
        migrationBuilder.DropColumn(name: "FailureCode", table: "StripeWebhookEvents");
        migrationBuilder.DropColumn(name: "LastAttemptAtUtc", table: "StripeWebhookEvents");
        migrationBuilder.DropColumn(name: "ProcessingStatus", table: "StripeWebhookEvents");
    }
}
