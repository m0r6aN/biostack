using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Additive column plus a data-integrity guard. Hand-written per this repository's migration
// pattern (central model snapshot intentionally left untouched; see
// 20260908000000_AddProviderAccessAuditEntries).
//
// 1. KnowledgeEntries.Details: one JSON document with the full reviewed record (safety, claims,
//    doses from both sides, sources, field authority) so nothing gathered stays hidden.
// 2. Duplicate canonical names: production held two "Retatrutide" rows and both the read path
//    and the upsert took an unordered first match, so a Refresh could update one row while the
//    API served the other. Duplicates (case-insensitive) are collapsed to the lowest Id, and a
//    unique index on lower("CanonicalName") stops new ones. Run a Refresh afterwards: it rewrites
//    every surviving row from the reviewed seed.
[DbContext(typeof(BioStackDbContext))]
[Migration("20261024000000_AddKnowledgeEntryDetailsAndUniqueName")]
public partial class AddKnowledgeEntryDetailsAndUniqueName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Details",
            table: "KnowledgeEntries",
            nullable: true);

        if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.Sql("""
                DELETE FROM "KnowledgeEntries" k
                USING "KnowledgeEntries" keep
                WHERE lower(k."CanonicalName") = lower(keep."CanonicalName")
                  AND k."Id" > keep."Id";
                """);
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "UX_KnowledgeEntries_CanonicalName_Lower"
                ON "KnowledgeEntries" (lower("CanonicalName"));
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "UX_KnowledgeEntries_CanonicalName_Lower";""");
        }

        migrationBuilder.DropColumn(name: "Details", table: "KnowledgeEntries");
    }
}
