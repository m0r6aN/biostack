using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BioStack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompoundGraphArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Lane C step 1: persist the reviewed compound graph so runtime intelligence reads
            // materialized relationships/findings instead of recomputing from KnowledgeEntry strings.
            // Hand-written to match the repo's migration style (type "TEXT"); dev/test build the
            // schema from the model via EnsureCreated, production applies this on Migrate().
            migrationBuilder.CreateTable(
                name: "CompoundGraphArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArtifactHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    BuilderVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceManifestHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ReviewState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RelationshipCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FindingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompoundGraphArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompoundGraphRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraphArtifactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubjectCompound = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SubjectSlug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ObjectCompound = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ObjectSlug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Directionality = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Confidence = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    EvidenceTier = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    SourceRefsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SafetyConcernLevel = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReviewState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    NeedsReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompoundGraphRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompoundGraphRelationships_CompoundGraphArtifacts_GraphArtifactId",
                        column: x => x.GraphArtifactId,
                        principalTable: "CompoundGraphArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompoundGraphFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraphArtifactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FindingType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SubjectCompound = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ObjectCompound = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Pathway = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    EvidenceRefsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    RecommendedAction = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompoundGraphFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompoundGraphFindings_CompoundGraphArtifacts_GraphArtifactId",
                        column: x => x.GraphArtifactId,
                        principalTable: "CompoundGraphArtifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompoundGraphArtifacts_ArtifactHash",
                table: "CompoundGraphArtifacts",
                column: "ArtifactHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompoundGraphArtifacts_IsActive",
                table: "CompoundGraphArtifacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CompoundGraphRelationships_GraphArtifactId_SubjectSlug",
                table: "CompoundGraphRelationships",
                columns: new[] { "GraphArtifactId", "SubjectSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_CompoundGraphRelationships_GraphArtifactId_ObjectSlug",
                table: "CompoundGraphRelationships",
                columns: new[] { "GraphArtifactId", "ObjectSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_CompoundGraphFindings_GraphArtifactId",
                table: "CompoundGraphFindings",
                column: "GraphArtifactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CompoundGraphFindings");
            migrationBuilder.DropTable(name: "CompoundGraphRelationships");
            migrationBuilder.DropTable(name: "CompoundGraphArtifacts");
        }
    }
}
