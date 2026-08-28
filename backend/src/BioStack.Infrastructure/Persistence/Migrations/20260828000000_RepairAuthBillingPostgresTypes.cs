namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(BioStackDbContext))]
[Migration("20260828000000_RepairAuthBillingPostgresTypes")]
public sealed class RepairAuthBillingPostgresTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) != true)
        {
            return;
        }

        migrationBuilder.Sql(PostgresCriticalTypeRepairSql.Build());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Forward-only production repair. Reintroducing provider-incompatible storage types
        // would make existing authentication and billing rows unreadable through Npgsql.
    }
}
