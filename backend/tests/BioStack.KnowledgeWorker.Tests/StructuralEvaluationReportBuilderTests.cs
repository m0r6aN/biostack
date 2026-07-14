namespace BioStack.KnowledgeWorker.Tests;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class StructuralEvaluationReportBuilderTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [Fact]
    public void Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict()
    {
        var report = new StructuralEvaluationReportBuilder().Build();

        Assert.Equal("1.2.0", report.ReportVersion);
        Assert.Equal("offline-structural-evaluation", report.ReportKind);
        Assert.Equal("offline-structural-declaration-and-corpus-inventory", report.Payload.Scope);
        Assert.Equal("partial", report.Payload.EvaluationStatus);
        Assert.Equal("pending-approval", report.Payload.PolicyStatus);
        Assert.Equal("not_evaluated", report.Payload.OverallVerdict);
        Assert.False(report.Payload.ModelInvoked);
        Assert.False(report.Payload.NetworkAccessed);
        Assert.Equal("partial", report.Payload.Comparison.CandidateCoverageStatus);
        Assert.Equal("not_evaluated", report.Payload.Comparison.OverallVerdict);
        Assert.False(report.Payload.Comparison.CandidateDeclarationsTrusted);
        Assert.Equal("none", report.Payload.Comparison.EffectAuthority);
        Assert.Equal(4, report.Payload.Comparison.CandidateCaseCount);
        Assert.Equal("1.0.0", report.Payload.CorpusInventory.SnapshotVersion);
        Assert.Equal(49, report.Payload.CorpusInventory.SeedRecordCount);
        Assert.Equal(16, report.Payload.CorpusInventory.CandidateRecordCount);
        Assert.Equal(16, report.Payload.CorpusInventory.EvidencePacketCount);
        Assert.Equal(13, report.Payload.CorpusInventory.SourceRegistryRecordCount);
        Assert.Equal(10, report.Payload.CorpusInventory.SeedCandidateOverlapCount);
        Assert.Equal(39, report.Payload.CorpusInventory.SeedOnlyCanonicalIds.Count);
        Assert.Equal(6, report.Payload.CorpusInventory.CandidateOnlyCanonicalIds.Count);
        Assert.Empty(report.Payload.CorpusInventory.CandidatesMissingEvidenceCanonicalIds);
        Assert.Empty(report.Payload.CorpusInventory.EvidenceWithoutCandidateCanonicalIds);
        Assert.Equal(2, report.Payload.CorpusInventory.IdentityTokenCollisions.Count);
        Assert.Empty(report.Payload.CorpusInventory.ExternalIdentifierCollisions);
        Assert.Equal(0, report.Payload.CorpusInventory.ApprovedRightsSourceCount);
        Assert.Equal(0, report.Payload.CorpusInventory.ActiveOperationsSourceCount);
        Assert.Equal(0, report.Payload.CorpusInventory.AcquisitionEnabledSourceCount);
        Assert.Equal(0, report.Payload.CorpusInventory.RegistryAuthorizedEvidencePacketCount);
        Assert.False(report.Payload.CorpusInventory.ModelInvoked);
        Assert.False(report.Payload.CorpusInventory.NetworkAccessed);

        var observed = report.Payload.Metrics
            .Where(metric => metric.Status == "observed")
            .Select(metric => metric.MetricId)
            .ToArray();
        Assert.Equal(
            [
                "governed_corpus_identity_inventory",
                "structural_coverage",
                "structural_declaration_comparison",
            ],
            observed);

        var unavailable = report.Payload.Metrics
            .Where(metric => metric.Status != "observed")
            .ToArray();
        Assert.NotEmpty(unavailable);
        Assert.All(unavailable, metric => Assert.Equal("not_evaluated", metric.Status));
        Assert.DoesNotContain(report.Payload.Metrics, metric => metric.Status is "passed" or "failed");
        Assert.Equal(
            report.Payload.Metrics.OrderBy(metric => metric.MetricId, StringComparer.Ordinal),
            report.Payload.Metrics);
    }

    [Fact]
    public void Build_CurrentArtifacts_BindsCanonicalPayloadDigest()
    {
        var report = new StructuralEvaluationReportBuilder().Build();
        var canonicalPayloadJson = JsonSerializer.Serialize(report.Payload, SerializerOptions);
        var expected = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayloadJson))).ToLowerInvariant();

        Assert.Equal(expected, report.CanonicalPayloadSha256);
        Assert.Equal(64, report.CanonicalPayloadSha256.Length);
    }

    [Fact]
    public void BuildJson_CurrentArtifacts_IsStableAndOmitsRunTimeClaims()
    {
        var builder = new StructuralEvaluationReportBuilder();

        var first = builder.BuildJson();
        var second = builder.BuildJson();
        var json = JsonNode.Parse(first)!.AsObject();

        Assert.Equal(first, second);
        Assert.Null(json["generatedAtUtc"]);
        Assert.Equal("not_evaluated", (string?)json["payload"]?["overallVerdict"]);
        Assert.Equal("not_evaluated", (string?)json["payload"]?["comparison"]?["overallVerdict"]);
        Assert.False((bool)json["payload"]?["comparison"]?["candidateDeclarationsTrusted"]!);
        Assert.Equal(0, (int)json["payload"]?["corpusInventory"]?["approvedRightsSourceCount"]!);
        Assert.Equal(0, (int)json["payload"]?["corpusInventory"]?["registryAuthorizedEvidencePacketCount"]!);
        Assert.False((bool)json["payload"]?["corpusInventory"]?["modelInvoked"]!);
        Assert.False((bool)json["payload"]?["corpusInventory"]?["networkAccessed"]!);
        Assert.Null(json["payload"]?["corpusInventory"]?["generatedAtUtc"]);
        Assert.False((bool)json["payload"]?["modelInvoked"]!);
        Assert.False((bool)json["payload"]?["networkAccessed"]!);
        Assert.Null(json["payload"]?["comparison"]?["generatedAtUtc"]);
    }

    [Fact]
    public void WriteReport_CurrentArtifacts_UsesVersionedStableFile()
    {
        var outputDirectory = CreateTempDirectory();
        try
        {
            var builder = new StructuralEvaluationReportBuilder();

            var firstPath = builder.WriteReport(outputDirectory);
            var first = File.ReadAllBytes(firstPath);
            var secondPath = builder.WriteReport(outputDirectory);
            var second = File.ReadAllBytes(secondPath);

            Assert.Equal(firstPath, secondPath);
            Assert.Equal(StructuralEvaluationReportBuilder.ReportFileName, Path.GetFileName(firstPath));
            Assert.Equal(first, second);
            Assert.Equal((byte)'\n', second[^1]);
            Assert.Single(Directory.GetFiles(outputDirectory));
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biostack-structural-eval-report-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
