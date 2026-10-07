namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using BioStack.KnowledgeWorker.Pipeline.Graph;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// End-to-end containment tests for owner decision C1, run through <see cref="ResearchJob"/> itself.
///
/// A unit test of <see cref="RestrictedExcerptPolicy"/> proves the helper behaves; it does not prove the
/// worker actually calls it on the path that writes the artifact. These tests read the file the job
/// emitted from disk.
///
/// They prove the write path only. Artifacts written before this policy existed are unaffected by it,
/// and a packet supplied directly to a downstream consumer never passes through here. Read-time and
/// provider-boundary containment are separate requirements.
/// </summary>
public sealed class ResearchJobContainmentTests
{
    private const string ContainedQuote = "CONTAINED DRUGBANK EXCERPT THAT MUST NOT REACH AN ARTIFACT";
    private const string PermittedQuote = "permitted pubchem excerpt";

    [Fact]
    public async Task Emitted_artifact_contains_no_drugbank_excerpt_when_no_registry_is_configured()
    {
        var (outputDir, packetPath) = CreateInputs();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchCandidateFilePath = TestPaths.FixturePath("compound-candidates.sample.json"),
                // Deliberately no ResearchSourceRegistryFilePath: containment must not depend on it.
                ResearchEvidencePacketPath = packetPath,
                ResearchOutputDirectory = outputDir,
            };
            var context = new IngestionContext(options, CreateLogger());

            var result = await CreateJob(options).RunAsync(context);
            Assert.True(result.Success, result.ErrorMessage);

            var artifact = File.ReadAllText(
                Path.Combine(outputDir, "evidence-packet", "creatine.json"));

            Assert.DoesNotContain(ContainedQuote, artifact);
        }
        finally
        {
            Cleanup(outputDir, packetPath);
        }
    }

    [Fact]
    public async Task Emitted_artifact_preserves_citation_locator_and_permitted_excerpts()
    {
        var (outputDir, packetPath) = CreateInputs();
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchCandidateFilePath = TestPaths.FixturePath("compound-candidates.sample.json"),
                ResearchEvidencePacketPath = packetPath,
                ResearchOutputDirectory = outputDir,
            };
            var context = new IngestionContext(options, CreateLogger());

            var result = await CreateJob(options).RunAsync(context);
            Assert.True(result.Success, result.ErrorMessage);

            var artifact = JsonNode.Parse(File.ReadAllText(
                Path.Combine(outputDir, "evidence-packet", "creatine.json")))!;

            var evidence = artifact["claims"]!.AsArray()[0]!["extractedEvidence"]!.AsArray();
            var contained = evidence.First(e => (string?)e!["sourceRef"] == "drugbank-db00148")!;

            // The claim still records what it rested on and where; only the text is withheld.
            Assert.Null((string?)contained["quote"]);
            Assert.Equal("Pharmacology", (string?)contained["pageOrSection"]);

            // A source outside the contained scope is untouched.
            Assert.Contains(PermittedQuote, artifact.ToJsonString());

            // The withholding is recorded, alongside whatever flags the pipeline already set.
            var flags = artifact["ops"]!["qualityFlags"]!.AsArray()
                .Select(f => (string?)f).ToList();
            Assert.Contains(RestrictedExcerptPolicy.WithheldFlag, flags);
        }
        finally
        {
            Cleanup(outputDir, packetPath);
        }
    }

    [Fact]
    public async Task Input_packet_bytes_are_not_modified_by_the_run()
    {
        // Containment withholds from the artifact; it must never edit the operator's own record.
        var (outputDir, packetPath) = CreateInputs();
        try
        {
            var before = File.ReadAllBytes(packetPath);
            var options = new WorkerOptions
            {
                RunMode = RunMode.Research,
                ResearchCandidateFilePath = TestPaths.FixturePath("compound-candidates.sample.json"),
                ResearchEvidencePacketPath = packetPath,
                ResearchOutputDirectory = outputDir,
            };

            var result = await CreateJob(options).RunAsync(new IngestionContext(options, CreateLogger()));
            Assert.True(result.Success, result.ErrorMessage);

            Assert.Equal(before, File.ReadAllBytes(packetPath));
        }
        finally
        {
            Cleanup(outputDir, packetPath);
        }
    }

    /// <summary>
    /// Builds a packet carrying one contained DrugBank excerpt and one permitted excerpt, from the
    /// shared fixture so the rest of the pipeline's expectations still hold.
    /// </summary>
    private static (string OutputDir, string PacketPath) CreateInputs()
    {
        var outputDir = CreateTempDirectory();
        var packet = JsonNode.Parse(
            File.ReadAllText(TestPaths.FixturePath("evidence-packet.sample.json")))!.AsObject();

        packet["sources"]!.AsArray().Add(JsonNode.Parse("""
        {
          "sourceId": "drugbank-db00148",
          "sourceType": "database",
          "authorityTier": "C1",
          "title": "Creatine",
          "publisher": "DrugBank",
          "url": "https://go.drugbank.com/drugs/DB00148",
          "doi": null,
          "pmid": null,
          "publishedAt": null,
          "accessedAt": "2026-08-29T00:00:00Z"
        }
        """)!);

        var claim = packet["claims"]!.AsArray()[0]!.AsObject();
        claim["sourceRefs"]!.AsArray().Add("drugbank-db00148");
        var evidence = claim["extractedEvidence"]!.AsArray();
        evidence[0]!["quote"] = PermittedQuote;
        evidence.Add(JsonNode.Parse($$"""
        { "sourceRef": "drugbank-db00148", "quote": "{{ContainedQuote}}", "pageOrSection": "Pharmacology" }
        """)!);

        var packetPath = Path.Combine(outputDir, "input-evidence-packet.json");
        File.WriteAllText(packetPath, packet.ToJsonString());
        return (outputDir, packetPath);
    }

    private static void Cleanup(string outputDir, string packetPath)
    {
        try
        {
            if (File.Exists(packetPath)) File.Delete(packetPath);
            if (Directory.Exists(outputDir)) Directory.Delete(outputDir, recursive: true);
        }
        catch (IOException)
        {
            // A leaked temp directory must not fail a containment test.
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"biostack-containment-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static ILogger CreateLogger()
        => LoggerFactory.Create(_ => { }).CreateLogger("ResearchJobContainmentTests");

    private static ResearchJob CreateJob(WorkerOptions options) => new(
        options,
        new ResearchArtifactLoader(),
        ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory()),
        new EvidencePacketPreprocessor(),
        new SourceRegistryAuthorizer(),
        new EvidencePacketSubstanceRecordCompiler(),
        new ResearchReviewQueueBuilder(),
        new ResearchSummaryBuilder(),
        new ResearchTaskQueueBuilder(),
        new PromotionManifestBuilder(),
        new ReviewResolutionPlanBuilder(),
        new PromotionExporter(),
        new PromotionImportPreviewBuilder(),
        SubstanceRecordValidator.LoadFromFile(
            Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json")),
        new CompoundGraphBuilder(new RelationshipPacketAuthorizer()));
}
