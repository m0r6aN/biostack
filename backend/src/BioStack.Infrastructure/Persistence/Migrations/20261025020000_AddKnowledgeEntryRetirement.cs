using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Strictly additive: soft retirement for knowledge entries. Refresh sets RetiredAtUtc on a row
// whose record is no longer in the seed or discovered records, and SupersededBy when another
// record absorbed it (e.g. Testolone merged into Vosilasarm, #591). Retired rows stay for audit
// and redirects; public reads skip them. Hand-written per this repository's migration pattern
// (central model snapshot intentionally left untouched; see 20261024000000).
[DbContext(typeof(BioStackDbContext))]
[Migration("20261025020000_AddKnowledgeEntryRetirement")]
public partial class AddKnowledgeEntryRetirement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RetiredAtUtc",
            table: "KnowledgeEntries",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SupersededBy",
            table: "KnowledgeEntries",
            maxLength: 255,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SupersededBy", table: "KnowledgeEntries");
        migrationBuilder.DropColumn(name: "RetiredAtUtc", table: "KnowledgeEntries");
    }
}
