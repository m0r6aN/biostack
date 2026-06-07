namespace BioStack.Infrastructure.Persistence.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

public partial class PR14A_AddPromotionTargetToStagedTranscriptCandidateReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TargetCanonicalName",
            table: "StagedTranscriptCandidateReviews",
            type: "TEXT",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PromotedKnowledgeEntryId",
            table: "StagedTranscriptCandidateReviews",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PromotedAtUtc",
            table: "StagedTranscriptCandidateReviews",
            type: "TEXT",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TargetCanonicalName",
            table: "StagedTranscriptCandidateReviews");

        migrationBuilder.DropColumn(
            name: "PromotedKnowledgeEntryId",
            table: "StagedTranscriptCandidateReviews");

        migrationBuilder.DropColumn(
            name: "PromotedAtUtc",
            table: "StagedTranscriptCandidateReviews");
    }
}
