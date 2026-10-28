using System;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations;

// Strictly additive and nullable: user-entered structured dose (amount, unit, frequency, time or
// schedule) on CompoundRecords, plus the matching snapshot columns on ProtocolItems so saved
// protocol versions and the provider summary keep what the user entered at save time. Existing rows
// stay valid with all four columns NULL (no backfill, no default, no parsing of free-text Notes);
// the previous release simply ignores the new columns, so this is backward-compatible for one
// release. BioStack never suggests, prefills or computes these values; they hold only what the
// user typed. Hand-written per this repository's migration pattern (central model snapshot
// intentionally left untouched; see 20261024000000 and 20261025020000). The decimal columns carry
// no explicit provider type so EF maps numeric(18,4) on PostgreSQL and TEXT on SQLite; the string
// columns match the maxLength set in BioStackDbContext.
[DbContext(typeof(BioStackDbContext))]
[Migration("20261028150000_AddUserEnteredDoseFields")]
public partial class AddUserEnteredDoseFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "DoseAmount",
            table: "CompoundRecords",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DoseUnit",
            table: "CompoundRecords",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DoseFrequency",
            table: "CompoundRecords",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DoseTime",
            table: "CompoundRecords",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "CompoundDoseAmountSnapshot",
            table: "ProtocolItems",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompoundDoseUnitSnapshot",
            table: "ProtocolItems",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompoundDoseFrequencySnapshot",
            table: "ProtocolItems",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompoundDoseTimeSnapshot",
            table: "ProtocolItems",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CompoundDoseTimeSnapshot", table: "ProtocolItems");
        migrationBuilder.DropColumn(name: "CompoundDoseFrequencySnapshot", table: "ProtocolItems");
        migrationBuilder.DropColumn(name: "CompoundDoseUnitSnapshot", table: "ProtocolItems");
        migrationBuilder.DropColumn(name: "CompoundDoseAmountSnapshot", table: "ProtocolItems");
        migrationBuilder.DropColumn(name: "DoseTime", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "DoseFrequency", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "DoseUnit", table: "CompoundRecords");
        migrationBuilder.DropColumn(name: "DoseAmount", table: "CompoundRecords");
    }
}
