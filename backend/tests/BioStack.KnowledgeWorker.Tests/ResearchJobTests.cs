namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Extensions.Logging;
using Xunit;

public class ResearchJobTests
{
    [Fact]
    public async Task ResearchJob_Emits_Draft_Substances_Review_Queue_And_Report()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchCandidateFilePath = TestPaths.FixturePath("compound-candidates.sample.json"),
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchOutputDirectory = outputDir,
            };
            var context = new IngestionContext(options, CreateLogger());

            var result = await CreateJob(options).RunAsync(context);

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(1, result.ScannedCount);
            Assert.Equal(1, result.CreatedCount);
            Assert.Equal(1, result.FlaggedForReviewCount);
            AssertOutputJson(outputDir, "draft-substances.json");
            AssertOutputJson(outputDir, "review-queue.json");
            AssertOutputJson(outputDir, "research-summary.json");
            AssertOutputJson(outputDir, "promotion-manifest.json");
            AssertOutputJson(outputDir, "review-resolution-plan.json");
            AssertOutputJson(outputDir, "promotion-import-preview.json");
            AssertOutputJson(outputDir, "research-run-report.json");
            AssertOutputJson(Path.Combine(outputDir, "evidence-packet"), "creatine.json");
            AssertOutputJson(Path.Combine(outputDir, "promotion-export"), "promotion-export-manifest.json");
            AssertOutputJson(Path.Combine(outputDir, "promotion-export"), "substances.promotable.json");
            AssertOutputJson(outputDir, "promotion-import-preview.json");

            var drafts = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "draft-substances.json")))!.AsArray();
            Assert.Single(drafts);
            Assert.Equal("Creatine", drafts[0]!["identity"]!["canonicalName"]!.GetValue<string>());
            var summary = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "research-summary.json")))!;
            Assert.Equal(1, summary["DraftSubstanceCount"]!.GetValue<int>());
            Assert.Equal(0, summary["ReviewQueueItemCount"]!.GetValue<int>());
            Assert.Equal("Creatine", summary["Compounds"]![0]!["Name"]!.GetValue<string>());
            Assert.NotNull(summary["ReviewCategories"]);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "promotion-manifest.json")))!;
            Assert.Equal(1, manifest["Counts"]!["TotalDrafts"]!.GetValue<int>());
            Assert.Equal("review-required", manifest["ReviewRequired"]![0]!["Readiness"]!.GetValue<string>());
            var plan = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "review-resolution-plan.json")))!;
            Assert.True(plan["Counts"]!["TotalItems"]!.GetValue<int>() > 0);
            var evidencePacket = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "evidence-packet", "creatine.json")))!;
            Assert.Equal("Creatine", evidencePacket["compound"]!["canonicalName"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ResearchJob_Fails_When_No_Evidence_Packets_Are_Configured()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions { RunMode = RunMode.Research, ResearchOutputDirectory = outputDir };
            var context = new IngestionContext(options, CreateLogger());

            var result = await CreateJob(options).RunAsync(context);

            Assert.False(result.Success);
            Assert.Equal(1, result.FailedCount);
            var report = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "research-run-report.json")))!;
            Assert.Contains("No evidence packet files", report["Records"]![0]!["Error"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task ResearchJob_Applies_Review_Decision_To_Promotion_Manifest()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchReviewDecisionPath = TestPaths.FixturePath("review-decision.sample.json"),
                ResearchOutputDirectory = outputDir,
            };
            var context = new IngestionContext(options, CreateLogger());

            var result = await CreateJob(options).RunAsync(context);

            Assert.True(result.Success, result.ErrorMessage);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "promotion-manifest.json")))!;
            Assert.Equal(1, manifest["Counts"]!["CandidatesForPromotion"]!.GetValue<int>());
            Assert.Equal("Creatine", manifest["CandidatesForPromotion"]![0]!["Name"]!.GetValue<string>());
            Assert.Equal("approve-creatine-fixture-001", manifest["CandidatesForPromotion"]![0]!["ReviewDecisionIds"]![0]!.GetValue<string>());
            var exportManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "promotion-export", "promotion-export-manifest.json")))!;
            Assert.Equal(1, exportManifest["ExportedCount"]!.GetValue<int>());
            Assert.Equal("approve-creatine-fixture-001", exportManifest["Candidates"]![0]!["ReviewDecisionIds"]![0]!.GetValue<string>());
            var importPreview = JsonNode.Parse(File.ReadAllText(Path.Combine(outputDir, "promotion-import-preview.json")))!;
            Assert.Equal(1, importPreview["Counts"]!["TotalExported"]!.GetValue<int>());
            Assert.Equal("create", importPreview["Items"]![0]!["Action"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task PromotionImportDryRunJob_Succeeds_For_Safe_Create_Preview()
    {
        var researchOutput = CreateTempDirectory();
        var dryRunOutput = CreateTempDirectory();
        try
        {
            var researchOptions = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchReviewDecisionPath = TestPaths.FixturePath("review-decision.sample.json"),
                ResearchOutputDirectory = researchOutput,
            };
            await CreateJob(researchOptions).RunAsync(new IngestionContext(researchOptions, CreateLogger()));
            var options = new WorkerOptions
            {
                RunMode = RunMode.PromotionImportDryRun,
                PromotionImportPreviewPath = Path.Combine(researchOutput, "promotion-import-preview.json"),
                PromotionImportAggregatePath = Path.Combine(researchOutput, "promotion-export", "substances.promotable.json"),
                PromotionImportDryRunOutputDirectory = dryRunOutput,
            };

            var result = await CreateDryRunJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(1, result.ScannedCount);
            Assert.Equal(1, result.CreatedCount);
            var report = JsonNode.Parse(File.ReadAllText(Path.Combine(dryRunOutput, "promotion-import-dry-run-report.json")))!;
            Assert.True(report["SafeToApply"]!.GetValue<bool>());
            Assert.Equal("create", report["Items"]![0]!["PlannedAction"]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(researchOutput, recursive: true);
            Directory.Delete(dryRunOutput, recursive: true);
        }
    }

    [Fact]
    public async Task PromotionImportDryRunJob_Refuses_Preview_With_Skips()
    {
        var researchOutput = CreateTempDirectory();
        var dryRunOutput = CreateTempDirectory();
        try
        {
            var researchOptions = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchSourceRegistryFilePath = TestPaths.FixturePath("source-registry.sample.json"),
                ResearchEvidencePacketPath = TestPaths.FixturePath("evidence-packet.sample.json"),
                ResearchReviewDecisionPath = TestPaths.FixturePath("review-decision.sample.json"),
                ResearchOutputDirectory = researchOutput,
            };
            await CreateJob(researchOptions).RunAsync(new IngestionContext(researchOptions, CreateLogger()));
            var preview = JsonNode.Parse(File.ReadAllText(Path.Combine(researchOutput, "promotion-import-preview.json")))!;
            preview["Counts"]!["WouldSkip"] = 1;
            var unsafePreview = Path.Combine(researchOutput, "promotion-import-preview-unsafe.json");
            File.WriteAllText(unsafePreview, preview.ToJsonString());
            var options = new WorkerOptions
            {
                RunMode = RunMode.PromotionImportDryRun,
                PromotionImportPreviewPath = unsafePreview,
                PromotionImportAggregatePath = Path.Combine(researchOutput, "promotion-export", "substances.promotable.json"),
                PromotionImportDryRunOutputDirectory = dryRunOutput,
            };

            var result = await CreateDryRunJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.False(result.Success);
            var report = JsonNode.Parse(File.ReadAllText(Path.Combine(dryRunOutput, "promotion-import-dry-run-report.json")))!;
            Assert.False(report["SafeToApply"]!.GetValue<bool>());
            Assert.Contains("skipped", report["RefusalReasons"]![0]!.GetValue<string>());
        }
        finally
        {
            Directory.Delete(researchOutput, recursive: true);
            Directory.Delete(dryRunOutput, recursive: true);
        }
    }

    private static ResearchJob CreateJob(WorkerOptions options) => new(
        options,
        new ResearchArtifactLoader(),
        ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory()),
        new EvidencePacketPreprocessor(),
        new SourceRegistryAuthorizer(),
        new EvidencePacketSubstanceRecordCompiler(),
        new ResearchReviewQueueBuilder(),
        new ResearchSummaryBuilder(),
        new PromotionManifestBuilder(),
        new ReviewResolutionPlanBuilder(),
        new PromotionExporter(),
        new PromotionImportPreviewBuilder(),
        SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json")));

    private static PromotionImportDryRunJob CreateDryRunJob(WorkerOptions options) => new(
        options,
        SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json")));

    private static ILogger CreateLogger()
        => LoggerFactory.Create(_ => { }).CreateLogger("ResearchJobTests");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biostack-research-job-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void AssertOutputJson(string outputDir, string fileName)
    {
        var path = Path.Combine(outputDir, fileName);
        Assert.True(File.Exists(path), $"Expected output file at {path}");
        Assert.NotNull(JsonNode.Parse(File.ReadAllText(path)));
    }
}