namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.Application.Governance;
using BioStack.Application.ProtocolIntelligence;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Offline Protocol Intelligence evaluation worker. Deterministic and CI-friendly: it loads the real
/// research/protocol-intelligence/*.json corpus, exercises the build-time gate, and writes a report —
/// with no database connection. Forbidden-output enforcement is the gate's DoctrineSanitizer only.
/// </summary>
public sealed class ProtocolIntelligenceEvaluationJobTests
{
    [Fact]
    public async Task Evaluate_CurrentArtifacts_NoCandidates_Succeeds()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.ProtocolIntelligenceEvaluation,
                ProtocolIntelligenceEvaluationOutputDirectory = outputDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(0, result.FailedCount);

            var report = LoadReport(outputDir);
            Assert.True(report["CorpusLoaded"]!.GetValue<bool>());
            Assert.Equal(6, report["PromotionTargets"]!.AsArray().Count);
            Assert.Equal(7, report["ArtifactVersions"]!.AsObject().Count);
            Assert.Empty(report["StructuralViolations"]!.AsArray());
            Assert.Equal(0, report["CandidatesEvaluated"]!.GetValue<int>());
            Assert.Equal(0, report["BlockedCandidates"]!.GetValue<int>());
            Assert.True(report["CanPromoteAll"]!.GetValue<bool>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public async Task Evaluate_SyntheticDoctrineViolationCandidate_BlocksAndFails()
    {
        var outputDir = CreateTempDirectory();
        var candidateDir = CreateTempDirectory();
        try
        {
            // A relationship_artifact that is otherwise complete/approved, but whose user-facing text
            // carries imperative medical phrasing — caught only by DoctrineSanitizer.
            File.WriteAllText(Path.Combine(candidateDir, "violating-relationship.json"), """
            {
              "artifactType": "relationship_artifact",
              "artifact": {
                "relationshipType": "synergy",
                "subject": "compound:magnesium-glycinate",
                "object": "compound:ashwagandha",
                "phaseContext": "maintenance",
                "goalContext": "sleep-support",
                "evidenceTier": "observational",
                "confidence": "moderate",
                "sourceRefs": ["source:pubmed:12345"],
                "sourceAuthorityMix": "peer_reviewed",
                "safetyConcernLevel": "low",
                "productHandling": "dietary_supplement",
                "reviewStatus": "approved",
                "userFacingExplanation": "You should take 10 mg twice daily for best results."
              }
            }
            """);

            var options = new WorkerOptions
            {
                RunMode = RunMode.ProtocolIntelligenceEvaluation,
                ProtocolIntelligenceCandidateDirectory = candidateDir,
                ProtocolIntelligenceEvaluationOutputDirectory = outputDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.False(result.Success);
            Assert.True(result.FailedCount >= 1);

            var report = LoadReport(outputDir);
            Assert.Equal(1, report["CandidatesEvaluated"]!.GetValue<int>());
            Assert.Equal(1, report["BlockedCandidates"]!.GetValue<int>());
            Assert.False(report["CanPromoteAll"]!.GetValue<bool>());

            var candidate = report["CandidateResults"]!.AsArray()[0]!;
            Assert.False(candidate["CanPromote"]!.GetValue<bool>());
            Assert.Contains(
                candidate["BlockingReasons"]!.AsArray(),
                reason => reason!.GetValue<string>() == GateReasons.DoctrineViolation);
            Assert.Contains(
                candidate["DoctrineViolationFields"]!.AsArray(),
                field => field!.GetValue<string>() == "userFacingExplanation");
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
            Directory.Delete(candidateDir, recursive: true);
        }
    }

    [Fact]
    public async Task Evaluate_CleanApprovedCandidate_PromotesAndSucceeds()
    {
        var outputDir = CreateTempDirectory();
        var candidateDir = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(candidateDir, "clean-relationship.json"), """
            {
              "artifactType": "relationship_artifact",
              "artifact": {
                "relationshipType": "synergy",
                "subject": "compound:magnesium-glycinate",
                "object": "compound:ashwagandha",
                "phaseContext": "maintenance",
                "goalContext": "sleep-support",
                "evidenceTier": "observational",
                "confidence": "moderate",
                "sourceRefs": ["source:pubmed:12345"],
                "sourceAuthorityMix": "peer_reviewed",
                "safetyConcernLevel": "low",
                "productHandling": "dietary_supplement",
                "reviewStatus": "approved",
                "userFacingExplanation": "Observed co-occurrence in logged user context; educational reference only."
              }
            }
            """);

            var options = new WorkerOptions
            {
                RunMode = RunMode.ProtocolIntelligenceEvaluation,
                ProtocolIntelligenceCandidateDirectory = candidateDir,
                ProtocolIntelligenceEvaluationOutputDirectory = outputDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.True(result.Success, result.ErrorMessage);

            var report = LoadReport(outputDir);
            Assert.Equal(1, report["CandidatesEvaluated"]!.GetValue<int>());
            Assert.Equal(0, report["BlockedCandidates"]!.GetValue<int>());
            Assert.True(report["CanPromoteAll"]!.GetValue<bool>());

            var candidate = report["CandidateResults"]!.AsArray()[0]!;
            Assert.True(candidate["CanPromote"]!.GetValue<bool>());
            // The relationship taxonomy demands human review even when approved.
            Assert.True(candidate["RequiresHumanReview"]!.GetValue<bool>());
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
            Directory.Delete(candidateDir, recursive: true);
        }
    }

    [Fact]
    public async Task Evaluate_IsOffline_WritesOnlyReport_NoDatabaseDependency()
    {
        var outputDir = CreateTempDirectory();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.ProtocolIntelligenceEvaluation,
                ProtocolIntelligenceEvaluationOutputDirectory = outputDir,
            };

            // The job is constructed with only the gate, loader, and options — no DbContext,
            // no IKnowledgeSource. There is no path through which it can write to a database.
            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));

            Assert.True(result.Success, result.ErrorMessage);

            // Its only filesystem effect is the single report file in the configured output directory.
            var written = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
            Assert.Single(written);
            Assert.Equal(
                "protocol-intelligence-evaluation-report.json",
                Path.GetFileName(written[0]));
        }
        finally
        {
            Directory.Delete(outputDir, recursive: true);
        }
    }

    private static ProtocolIntelligenceEvaluationJob CreateJob(WorkerOptions options)
        => new(options, new ProtocolIntelligenceArtifactLoader(), new ProtocolIntelligenceGate(
            new ProtocolIntelligenceArtifactLoader(), new DoctrineSanitizer()));

    private static ILogger CreateLogger()
        => LoggerFactory.Create(_ => { }).CreateLogger("ProtocolIntelligenceEvaluationJobTests");

    private static JsonNode LoadReport(string outputDir)
        => JsonNode.Parse(File.ReadAllText(
            Path.Combine(outputDir, "protocol-intelligence-evaluation-report.json")))!;

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biostack-protocol-intelligence-eval-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
