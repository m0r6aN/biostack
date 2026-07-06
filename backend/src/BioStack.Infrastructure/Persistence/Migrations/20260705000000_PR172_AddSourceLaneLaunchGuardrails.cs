namespace BioStack.Infrastructure.Persistence.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

public partial class PR172_AddSourceLaneLaunchGuardrails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "IntakeRequestId",
            table: "StagedTranscriptCandidateReviews",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_StagedTranscriptCandidateReviews_IntakeRequestId",
            table: "StagedTranscriptCandidateReviews",
            column: "IntakeRequestId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StagedTranscriptCandidateReviews_IntakeRequestId",
            table: "StagedTranscriptCandidateReviews");

        migrationBuilder.DropColumn(
            name: "IntakeRequestId",
            table: "StagedTranscriptCandidateReviews");
    }
}
