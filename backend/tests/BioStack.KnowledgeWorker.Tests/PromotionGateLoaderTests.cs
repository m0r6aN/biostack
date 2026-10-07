namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Fail-closed coverage for <see cref="PromotionGateLoader"/>: every way the
/// review-decision index can fail to load must throw <see cref="PromotionGateLoadException"/>
/// rather than silently returning an empty (and therefore "everything looks unpromoted")
/// index. See promotion-state-audit.md §1.2/§6 risk 1 for why an unenforced Refresh is
/// unsafe, and the parcel spec's "fail closed" requirement.
/// </summary>
public class PromotionGateLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public PromotionGateLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"biostack-promotion-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void Throws_When_Directory_Does_Not_Exist_And_No_Path_Set()
    {
        var options = new WorkerOptions
        {
            ReviewDecisionDirectory = Path.Combine(_tempDir, "does-not-exist"),
            ReviewDecisionPath = null,
        };

        var ex = Assert.Throws<PromotionGateLoadException>(
            () => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));

        Assert.Contains("directory does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Throws_When_Directory_Exists_But_Is_Empty()
    {
        var options = new WorkerOptions { ReviewDecisionDirectory = _tempDir, ReviewDecisionPath = null };

        Assert.Throws<PromotionGateLoadException>(
            () => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));
    }

    [Fact]
    public void Throws_When_A_Matching_File_Is_Not_Valid_Json()
    {
        File.WriteAllText(Path.Combine(_tempDir, "review-decision-batch-broken.json"), "{ not json");
        var options = new WorkerOptions { ReviewDecisionDirectory = _tempDir, ReviewDecisionPath = null };

        var ex = Assert.Throws<PromotionGateLoadException>(
            () => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));

        Assert.Contains("could not parse", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Throws_When_A_Matching_File_Fails_Schema_Validation()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "review-decision-batch-invalid.json"),
            """{ "schemaVersion": "1.0.0", "recordType": "review-decision-batch" }""");
        var options = new WorkerOptions { ReviewDecisionDirectory = _tempDir, ReviewDecisionPath = null };

        var ex = Assert.Throws<PromotionGateLoadException>(
            () => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));

        Assert.Contains("schema validation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Loads_A_Valid_Batch_And_Reflects_Its_Decisions()
    {
        File.WriteAllText(
            Path.Combine(_tempDir, "review-decision-batch-2026-09-16-test.json"),
            ValidBatchJson("Semaglutide", "approve-for-promotion", clears: true));
        var options = new WorkerOptions { ReviewDecisionDirectory = _tempDir, ReviewDecisionPath = null };

        var index = PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator());

        Assert.True(index.HasPromotionApproval("Semaglutide"));
        Assert.False(index.HasPromotionApproval("Some Other Compound"));
    }

    [Fact]
    public void Ignores_Files_That_Do_Not_Match_The_Review_Decision_Glob()
    {
        File.WriteAllText(Path.Combine(_tempDir, "not-a-review-decision.json"), "{}");
        var options = new WorkerOptions { ReviewDecisionDirectory = _tempDir, ReviewDecisionPath = null };

        Assert.Throws<PromotionGateLoadException>(
            () => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Explicit_Valid_File_Does_Not_Mask_Missing_Or_Empty_Configured_Directory(bool empty)
    {
        var file = Path.Combine(_tempDir, "explicit.json");
        File.WriteAllText(file, ValidBatchJson("Semaglutide", "approve-for-promotion", true));
        var directory = Path.Combine(_tempDir, "corpus");
        if (empty) Directory.CreateDirectory(directory);
        var options = new WorkerOptions { ReviewDecisionPath = file, ReviewDecisionDirectory = directory };
        Assert.Throws<PromotionGateLoadException>(() => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));
    }

    [Fact]
    public void Intentional_Single_File_Mode_Loads_Without_Configured_Directory()
    {
        var file = Path.Combine(_tempDir, "explicit.json");
        File.WriteAllText(file, ValidBatchJson("Semaglutide", "approve-for-promotion", true));
        var options = new WorkerOptions { ReviewDecisionPath = file, ReviewDecisionDirectory = null };
        Assert.True(PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()).HasPromotionApproval("Semaglutide"));
    }

    [Fact]
    public void Valid_Directory_Does_Not_Mask_Missing_Explicit_File()
    {
        File.WriteAllText(Path.Combine(_tempDir, "review-decision-batch-valid.json"),
            ValidBatchJson("Semaglutide", "approve-for-promotion", true));
        var options = new WorkerOptions { ReviewDecisionDirectory = _tempDir, ReviewDecisionPath = Path.Combine(_tempDir, "missing.json") };
        Assert.Throws<PromotionGateLoadException>(() => PromotionGateLoader.LoadReviewDecisionIndexOrThrow(options, Validator()));
    }

    private static IResearchArtifactValidator Validator()
        => ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

    private static string ValidBatchJson(string compoundName, string decision, bool clears) => $$"""
        {
          "schemaVersion": "1.0.0",
          "recordType": "review-decision-batch",
          "batch": {
            "batchId": "test-batch",
            "reviewerId": "test-reviewer",
            "reviewedAt": "2026-09-16T00:00:00Z",
            "notes": []
          },
          "decisions": [
            {
              "decisionId": "test-{{compoundName}}",
              "compoundName": "{{compoundName}}",
              "decision": "{{decision}}",
              "reviewerId": "test-reviewer",
              "reviewedAt": "2026-09-16T00:00:00Z",
              "scope": {
                "claimIds": [],
                "qualityFlags": [],
                "reviewCategories": [],
                "promotionBlockers": []
              },
              "clearsSoftPromotionBlockers": {{(clears ? "true" : "false")}},
              "expiresAt": null,
              "notes": []
            }
          ]
        }
        """;
}
