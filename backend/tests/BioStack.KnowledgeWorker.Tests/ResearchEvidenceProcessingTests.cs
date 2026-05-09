namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class ResearchEvidenceProcessingTests
{
    [Fact]
    public void Preprocessor_Flags_SafetyCritical_Claim_Without_Authoritative_Source()
    {
        var packet = LoadEvidencePacket();
        packet["claims"]![0]!["claimType"] = "warning";
        packet["claims"]![0]!["fieldAuthorityRequired"] = true;

        var result = new EvidencePacketPreprocessor().Preprocess(packet);

        Assert.Contains("missing-authoritative-support", result.QualityFlags);
        Assert.True(result.Packet["ops"]!["needsReview"]!.GetValue<bool>());
        Assert.Contains(result.ReviewReasons, r => r.Contains("requires authoritative A1/A2 support"));
    }

    [Fact]
    public void Preprocessor_Flags_Unknown_Source_Reference()
    {
        var packet = LoadEvidencePacket();
        packet["claims"]![0]!["sourceRefs"]!.AsArray().Add("missing-source");

        var result = new EvidencePacketPreprocessor().Preprocess(packet);

        Assert.Contains("unknown-source-ref", result.QualityFlags);
        Assert.Contains(result.ReviewReasons, r => r.Contains("missing-source"));
    }

    [Fact]
    public void Compiler_Produces_Substance_Record_Draft_That_Validates_Against_Canonical_Schema()
    {
        var packet = new EvidencePacketPreprocessor().Preprocess(LoadEvidencePacket()).Packet;
        var draft = new EvidencePacketSubstanceRecordCompiler().CompileDraft(packet);
        var schemaPath = Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json");
        var validator = SubstanceRecordValidator.LoadFromFile(schemaPath);

        var result = validator.Validate(draft);

        Assert.True(result.IsValid, result.Summary());
        Assert.Equal("substance", draft["recordType"]!.GetValue<string>());
        Assert.True(draft["ops"]!["needsReview"]!.GetValue<bool>());
    }

    [Fact]
    public void ReviewQueueBuilder_Creates_Items_For_Preprocessing_Review_Reasons()
    {
        var packet = LoadEvidencePacket();
        packet["claims"]![0]!["claimType"] = "contraindication";

        var preprocessed = new EvidencePacketPreprocessor().Preprocess(packet).Packet;
        var items = new ResearchReviewQueueBuilder().BuildFromEvidencePacket(preprocessed);

        Assert.NotEmpty(items);
        Assert.Contains(items, item => item.CompoundName == "Creatine" && item.Severity == "review");
    }

    [Fact]
    public void SummaryBuilder_Groups_Draft_Quality_Flags_And_Review_Reasons()
    {
        var packet = new EvidencePacketPreprocessor().Preprocess(LoadEvidencePacket()).Packet;
        var draft = new EvidencePacketSubstanceRecordCompiler().CompileDraft(packet);
        var drafts = new JsonArray(draft);
        var reviewQueue = new ResearchReviewQueueBuilder().BuildFromEvidencePacket(packet);

        var summary = new ResearchSummaryBuilder().Build(drafts, reviewQueue);

        Assert.Equal(1, summary.DraftSubstanceCount);
        Assert.Equal("Creatine", summary.Compounds.Single().Name);
        Assert.Contains(summary.QualityFlags, bucket => bucket.Name == "compiled-from-evidence-packet");
        Assert.Contains(summary.ReviewReasons, bucket => bucket.Name.Contains("Compiled draft requires human review"));
    }

    [Fact]
    public void SummaryBuilder_Classifies_Regulatory_Safety_And_Misinformation_Risk()
    {
        var draft = new EvidencePacketSubstanceRecordCompiler().CompileDraft(
            new EvidencePacketPreprocessor().Preprocess(LoadEvidencePacket()).Packet);
        draft["ops"]!["qualityFlags"]!.AsArray().Add("regulatory-ambiguity");
        draft["ops"]!["qualityFlags"]!.AsArray().Add("no-safety-canonicalization");
        draft["ops"]!["qualityFlags"]!.AsArray().Add("misinformation-heavy");
        draft["ops"]!["qualityFlags"]!.AsArray().Add("source-registry-field-mismatch");
        var drafts = new JsonArray(draft);

        var summary = new ResearchSummaryBuilder().Build(drafts, Array.Empty<ResearchReviewQueueItem>());

        Assert.Contains(summary.ReviewCategories, c => c.Name == "Regulatory / Approval" && c.Compounds.Contains("Creatine"));
        Assert.Contains(summary.ReviewCategories, c => c.Name == "Safety Critical" && c.Compounds.Contains("Creatine"));
        Assert.Contains(summary.ReviewCategories, c => c.Name == "Misinformation / Vendor Claims" && c.Compounds.Contains("Creatine"));
        Assert.Contains(summary.ReviewCategories, c => c.Name == "Source Registry Authorization" && c.Compounds.Contains("Creatine"));
        Assert.Contains(summary.ReviewCategories,
            c => c.Name == "Source Registry Authorization"
                 && c.RecommendedActions.Any(a => a.Contains("Add a source authorized", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void SourceRegistryAuthorizer_Accepts_Claim_When_Source_Is_Authorized_For_Field()
    {
        var packet = LoadEvidencePacket();
        var registry = JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("source-registry.sample.json")))!;

        var result = new SourceRegistryAuthorizer().Authorize(packet, registry);

        Assert.DoesNotContain("source-registry-field-mismatch", result.QualityFlags);
        Assert.DoesNotContain(result.ReviewReasons, r => r.Contains("lacks source-registry authorization"));
    }

    [Fact]
    public void SourceRegistryAuthorizer_Flags_Claim_When_Source_Is_Not_Authorized_For_Field()
    {
        var packet = LoadEvidencePacket();
        packet["claims"]![0]!["claimType"] = "approved-indication";
        var registry = JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("source-registry.sample.json")))!;

        var result = new SourceRegistryAuthorizer().Authorize(packet, registry);

        Assert.Contains("source-registry-field-mismatch", result.QualityFlags);
        Assert.Contains(result.ReviewReasons, r => r.Contains("approved-indications"));
        Assert.True(result.Packet["ops"]!["needsReview"]!.GetValue<bool>());
    }

    [Fact]
    public void SummaryBuilder_Marks_SourceRegistry_Mismatch_As_Blocked()
    {
        var draft = Draft("Blocked Compound", "Strong", "complete", needsReview: false,
            qualityFlags: new[] { "source-registry-field-mismatch" });

        var summary = new ResearchSummaryBuilder().Build(new JsonArray(draft), Array.Empty<ResearchReviewQueueItem>());

        var compound = summary.Compounds.Single();
        Assert.Equal("blocked", compound.PromotionReadiness);
        Assert.Contains(compound.PromotionBlockers, b => b.Contains("source-registry", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(summary.PromotionReadiness, b => b.Name == "blocked" && b.Compounds.Contains("Blocked Compound"));
    }

    [Fact]
    public void SummaryBuilder_Marks_NeedsReview_Draft_As_ReviewRequired()
    {
        var draft = Draft("Review Compound", "Moderate", "substantial", needsReview: true);

        var summary = new ResearchSummaryBuilder().Build(new JsonArray(draft), Array.Empty<ResearchReviewQueueItem>());

        var compound = summary.Compounds.Single();
        Assert.Equal("review-required", compound.PromotionReadiness);
        Assert.Contains(compound.PromotionBlockers, b => b.Contains("needsReview", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SummaryBuilder_Marks_Clean_Substantial_Draft_As_CandidateForPromotion()
    {
        var draft = Draft("Clean Compound", "Strong", "complete", needsReview: false);

        var summary = new ResearchSummaryBuilder().Build(new JsonArray(draft), Array.Empty<ResearchReviewQueueItem>());

        var compound = summary.Compounds.Single();
        Assert.Equal("candidate-for-promotion", compound.PromotionReadiness);
        Assert.Empty(compound.PromotionBlockers);
        Assert.Contains(summary.PromotionReadiness, b => b.Name == "candidate-for-promotion" && b.Compounds.Contains("Clean Compound"));
    }

    [Fact]
    public void SummaryBuilder_ReviewDecision_Clears_Soft_Blockers()
    {
        var draft = Draft("Creatine", "Unknown", "partial", needsReview: true);
        var decisionBatch = JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("review-decision.sample.json")))!;
        var decisions = ReviewDecisionIndex.FromBatches(new[] { decisionBatch });

        var summary = new ResearchSummaryBuilder().Build(new JsonArray(draft), Array.Empty<ResearchReviewQueueItem>(), decisions);

        var compound = summary.Compounds.Single();
        Assert.Equal("candidate-for-promotion", compound.PromotionReadiness);
        Assert.Contains("approve-creatine-fixture-001", compound.ReviewDecisionIds);
        Assert.Empty(compound.PromotionBlockers);
    }

    [Fact]
    public void SummaryBuilder_ReviewDecision_Does_Not_Clear_Hard_Blockers()
    {
        var draft = Draft("Creatine", "Unknown", "partial", needsReview: true,
            qualityFlags: new[] { "source-registry-field-mismatch" });
        var decisionBatch = JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("review-decision.sample.json")))!;
        var decisions = ReviewDecisionIndex.FromBatches(new[] { decisionBatch });

        var summary = new ResearchSummaryBuilder().Build(new JsonArray(draft), Array.Empty<ResearchReviewQueueItem>(), decisions);

        var compound = summary.Compounds.Single();
        Assert.Equal("blocked", compound.PromotionReadiness);
        Assert.Contains(compound.PromotionBlockers, b => b.Contains("source-registry", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PromotionManifestBuilder_Groups_Candidates_By_Readiness()
    {
        var drafts = new JsonArray(
            Draft("Blocked Compound", "Strong", "complete", needsReview: false, qualityFlags: new[] { "source-registry-field-mismatch" }),
            Draft("Review Compound", "Moderate", "partial", needsReview: true),
            Draft("Clean Compound", "Strong", "complete", needsReview: false));
        var summary = new ResearchSummaryBuilder().Build(drafts, Array.Empty<ResearchReviewQueueItem>());

        var manifest = new PromotionManifestBuilder().Build(summary, new PromotionManifestOutputs(
            DraftSubstances: "draft-substances.json",
            ReviewQueue: "review-queue.json",
            ResearchSummary: "research-summary.json",
            RunReport: "research-run-report.json"));

        Assert.Equal(3, manifest.Counts.TotalDrafts);
        Assert.Single(manifest.Blocked);
        Assert.Single(manifest.ReviewRequired);
        Assert.Single(manifest.CandidatesForPromotion);
        Assert.Contains(manifest.Blocked.Single().RequiredNextActions, a => a.Contains("Resolve all blocked", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("draft-substances.json", manifest.Outputs.DraftSubstances);
    }

    [Fact]
    public void PromotionManifestBuilder_Includes_ReviewDecisionIds_For_Approved_Candidates()
    {
        var draft = Draft("Creatine", "Unknown", "partial", needsReview: true);
        var decisionBatch = JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("review-decision.sample.json")))!;
        var decisions = ReviewDecisionIndex.FromBatches(new[] { decisionBatch });
        var summary = new ResearchSummaryBuilder().Build(new JsonArray(draft), Array.Empty<ResearchReviewQueueItem>(), decisions);

        var manifest = new PromotionManifestBuilder().Build(summary, new PromotionManifestOutputs(
            DraftSubstances: "draft-substances.json",
            ReviewQueue: "review-queue.json",
            ResearchSummary: "research-summary.json",
            RunReport: "research-run-report.json"));

        Assert.Contains("approve-creatine-fixture-001", manifest.CandidatesForPromotion.Single().ReviewDecisionIds);
    }

    [Fact]
    public void ReviewResolutionPlanBuilder_Emits_Actionable_Items_By_ResolutionType()
    {
        var drafts = new JsonArray(
            Draft("Blocked Compound", "Strong", "complete", needsReview: false, qualityFlags: new[] { "source-registry-field-mismatch" }),
            Draft("Review Compound", "Moderate", "partial", needsReview: true));
        var summary = new ResearchSummaryBuilder().Build(drafts, Array.Empty<ResearchReviewQueueItem>());
        var manifest = new PromotionManifestBuilder().Build(summary, new PromotionManifestOutputs(
            DraftSubstances: "draft-substances.json",
            ReviewQueue: "review-queue.json",
            ResearchSummary: "research-summary.json",
            RunReport: "research-run-report.json"));

        var plan = new ReviewResolutionPlanBuilder().Build(manifest, Array.Empty<ResearchReviewQueueItem>());

        Assert.True(plan.Counts.TotalItems >= 2);
        Assert.Contains(plan.Items, item => item.CompoundName == "Blocked Compound" && item.ResolutionType == "fix-source-authorization");
        Assert.Contains(plan.Items, item => item.CompoundName == "Review Compound" && item.ResolutionType == "human-review");
        Assert.Contains(plan.Counts.ResolutionTypes, bucket => bucket.Name == "fix-source-authorization");
    }

    [Fact]
    public void PromotionExporter_Exports_Only_Candidates_For_Promotion()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var drafts = new JsonArray(
                DraftSubstance("Creatine", needsReview: false),
                DraftSubstance("Review Compound", needsReview: true));
            var decisionBatch = JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("review-decision.sample.json")))!;
            var decisions = ReviewDecisionIndex.FromBatches(new[] { decisionBatch });
            var summary = new ResearchSummaryBuilder().Build(drafts, Array.Empty<ResearchReviewQueueItem>(), decisions);
            var manifest = new PromotionManifestBuilder().Build(summary, new PromotionManifestOutputs(
                DraftSubstances: "draft-substances.json",
                ReviewQueue: "review-queue.json",
                ResearchSummary: "research-summary.json",
                RunReport: "research-run-report.json"));

            var result = new PromotionExporter().Export(drafts, manifest, outputDir);

            Assert.Equal(1, result.ExportedCount);
            Assert.True(File.Exists(Path.Combine(result.SubstancesDirectory, "creatine.json")));
            var aggregate = JsonNode.Parse(File.ReadAllText(result.AggregatePath))!.AsArray();
            Assert.Single(aggregate);
            Assert.Equal("Creatine", aggregate[0]!["identity"]!["canonicalName"]!.GetValue<string>());
            Assert.False(aggregate[0]!["ops"]!["isActive"]!.GetValue<bool>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void PromotionExporter_Writes_Empty_Export_When_No_Candidates_Are_Promotable()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var drafts = new JsonArray(DraftSubstance("Review Compound", needsReview: true));
            var summary = new ResearchSummaryBuilder().Build(drafts, Array.Empty<ResearchReviewQueueItem>());
            var manifest = new PromotionManifestBuilder().Build(summary, new PromotionManifestOutputs(
                DraftSubstances: "draft-substances.json",
                ReviewQueue: "review-queue.json",
                ResearchSummary: "research-summary.json",
                RunReport: "research-run-report.json"));

            var result = new PromotionExporter().Export(drafts, manifest, outputDir);

            Assert.Equal(0, result.ExportedCount);
            Assert.Empty(JsonNode.Parse(File.ReadAllText(result.AggregatePath))!.AsArray());
            var exportManifest = JsonNode.Parse(File.ReadAllText(result.ManifestPath))!;
            Assert.Equal(0, exportManifest["ExportedCount"]!.GetValue<int>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void PromotionImportPreview_Classifies_Create_Update_And_Skip()
    {
        var exported = new JsonArray(
            DraftSubstance("Create Compound", needsReview: false),
            DraftSubstance("Update Compound", needsReview: false),
            DraftSubstance("Active Compound", needsReview: false));
        exported[2]!["ops"]!["isActive"] = true;
        var existing = new JsonArray(DraftSubstance("Update Compound", needsReview: false));
        var manifest = new PromotionExportManifest(
            ManifestVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            ExportedCount: 3,
            Candidates: new[]
            {
                Candidate("Create Compound"),
                Candidate("Update Compound"),
                Candidate("Active Compound"),
            },
            SkippedCompounds: Array.Empty<string>());

        var preview = new PromotionImportPreviewBuilder().Build(exported, manifest, existing, Validator());

        Assert.Equal(3, preview.Counts.TotalExported);
        Assert.Equal(1, preview.Counts.WouldCreate);
        Assert.Equal(1, preview.Counts.WouldUpdate);
        Assert.Equal(1, preview.Counts.WouldSkip);
        Assert.Contains(preview.Items, i => i.Name == "Create Compound" && i.Action == "create");
        Assert.Contains(preview.Items, i => i.Name == "Update Compound" && i.Action == "update");
        Assert.Contains(preview.Items, i => i.Name == "Active Compound" && i.Action == "skip" && i.Reasons.Any(r => r.Contains("active")));
    }

    [Fact]
    public void PromotionImportPreview_Flags_Duplicate_Slug_As_Skip()
    {
        var first = DraftSubstance("Duplicate One", needsReview: false);
        var second = DraftSubstance("Duplicate Two", needsReview: false);
        second["identity"]!["slug"] = first["identity"]!["slug"]!.GetValue<string>();
        var exported = new JsonArray(first, second);
        var manifest = new PromotionExportManifest(
            ManifestVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            ExportedCount: 2,
            Candidates: new[] { Candidate("Duplicate One"), Candidate("Duplicate Two") },
            SkippedCompounds: Array.Empty<string>());

        var preview = new PromotionImportPreviewBuilder().Build(exported, manifest, new JsonArray(), Validator());

        Assert.Equal(1, preview.Counts.DuplicateSlugs);
        Assert.Equal(2, preview.Counts.WouldSkip);
        Assert.All(preview.Items, item => Assert.Contains(item.Reasons, r => r.Contains("duplicate slug")));
    }

    private static JsonNode LoadEvidencePacket()
        => JsonNode.Parse(File.ReadAllText(TestPaths.FixturePath("evidence-packet.sample.json")))!;

    private static SubstanceRecordValidator Validator()
        => SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json"));

    private static PromotionExportCandidate Candidate(string name)
        => new(
            Name: name,
            Slug: SubstanceRecordNormalizer.Slugify(name),
            Readiness: "candidate-for-promotion",
            SubstanceFile: $"{SubstanceRecordNormalizer.Slugify(name)}.json",
            AggregateIndex: 0,
            ReviewDecisionIds: Array.Empty<string>(),
            QualityFlags: Array.Empty<string>());

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biostack-promotion-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static JsonNode DraftSubstance(string name, bool needsReview)
    {
        var packet = new EvidencePacketPreprocessor().Preprocess(LoadEvidencePacket()).Packet;
        packet["compound"]!["canonicalName"] = name;
        var draft = new EvidencePacketSubstanceRecordCompiler().CompileDraft(packet);
        draft["identity"]!["canonicalName"] = name;
        draft["identity"]!["canonicalId"] = SubstanceRecordNormalizer.Slugify(name);
        draft["identity"]!["slug"] = SubstanceRecordNormalizer.Slugify(name);
        draft["ops"]!["needsReview"] = needsReview;
        if (!needsReview)
        {
            draft["ops"]!["completeness"] = "complete";
            draft["evidence"]!["overallTier"] = "Strong";
            draft["ops"]!["reviewReasons"] = new JsonArray();
        }
        return draft;
    }

    private static JsonNode Draft(
        string name,
        string evidenceTier,
        string completeness,
        bool needsReview,
        IReadOnlyList<string>? qualityFlags = null,
        IReadOnlyList<string>? reviewReasons = null)
        => new JsonObject
        {
            ["identity"] = new JsonObject
            {
                ["canonicalName"] = name,
                ["classification"] = "Supplement",
            },
            ["evidence"] = new JsonObject { ["overallTier"] = evidenceTier },
            ["ops"] = new JsonObject
            {
                ["completeness"] = completeness,
                ["needsReview"] = needsReview,
                ["qualityFlags"] = ToArray(qualityFlags ?? Array.Empty<string>()),
                ["reviewReasons"] = ToArray(reviewReasons ?? Array.Empty<string>()),
            },
        };

    private static JsonArray ToArray(IEnumerable<string> values)
    {
        var arr = new JsonArray();
        foreach (var value in values) arr.Add(value);
        return arr;
    }
}