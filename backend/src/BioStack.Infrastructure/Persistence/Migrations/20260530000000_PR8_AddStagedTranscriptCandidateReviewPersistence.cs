namespace BioStack.Infrastructure.Persistence.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

public partial class PR8_AddStagedTranscriptCandidateReviewPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StagedTranscriptCandidateReviews",
            columns: table => new
            {
                ArtifactId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Canonicality = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                ReviewState = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SourceType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                SourceUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                SourceMetadataJson = table.Column<string>(type: "TEXT", maxLength: 32768, nullable: false),
                Provider = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                IsDeterministicFixture = table.Column<bool>(type: "INTEGER", nullable: false),
                SegmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                SegmentSnapshotSignature = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                UpdatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StagedTranscriptCandidateReviews", x => x.ArtifactId);
                table.CheckConstraint("CK_StagedTranscriptCandidateReviews_Canonicality_NonCanonical", "\"Canonicality\" = 'non_canonical'");
                table.CheckConstraint("CK_StagedTranscriptCandidateReviews_ReviewState_Lifecycle", "\"ReviewState\" IN ('pending_review','review_deferred','review_rejected','review_approved_for_promotion')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_StagedTranscriptCandidateReviews_ReviewState",
            table: "StagedTranscriptCandidateReviews",
            column: "ReviewState");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StagedTranscriptCandidateReviews");
    }
}
