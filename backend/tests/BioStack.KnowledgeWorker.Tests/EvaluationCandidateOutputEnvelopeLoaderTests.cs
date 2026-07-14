namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class EvaluationCandidateOutputEnvelopeLoaderTests
{
    [Fact]
    public void Load_CurrentContractFixture_IsMetadataOnlyAndCandidateOnly()
    {
        var envelope = new EvaluationCandidateOutputEnvelopeLoader().Load();

        Assert.Equal("1.0.0", envelope.ArtifactVersion);
        Assert.Equal("1.0.0", envelope.TaxonomyVersion);
        Assert.Equal("1.0.0", envelope.MatrixVersion);
        Assert.Equal("1.0.0", envelope.FixtureCorpusVersion);
        Assert.Equal("1.0.0", envelope.AdversarialCorpusVersion);
        Assert.Equal("contract_fixture", envelope.EnvelopePurpose);
        Assert.Equal(64, envelope.CandidateConfigurationSha256.Length);
        Assert.Equal(4, envelope.Results.Count);
        Assert.Equal(
            envelope.Results.OrderBy(result => result.CaseId, StringComparer.Ordinal),
            envelope.Results);
        Assert.False(envelope.RuntimeTruth);
        Assert.False(envelope.ModelInvokedByLoader);
        Assert.False(envelope.NetworkAccessedByLoader);
        Assert.Equal("none", envelope.EffectAuthority);
    }

    [Fact]
    public void CurrentContract_ExcludesRawInputsOutputsAndCustomerData()
    {
        var artifact = ReadCurrentArtifact();
        var policy = artifact["dataPolicy"]!.AsObject();

        Assert.False((bool)policy["rawInputsIncluded"]!);
        Assert.False((bool)policy["rawOutputsIncluded"]!);
        Assert.False((bool)policy["customerDataIncluded"]!);
        Assert.False((bool)policy["modelInvocationPerformedByLoader"]!);
        Assert.False((bool)policy["networkAccessPerformedByLoader"]!);
        Assert.Equal("none", (string?)policy["effectAuthority"]);
        Assert.All(artifact["results"]!.AsArray(), result =>
        {
            Assert.Null(result!["prompt"]);
            Assert.Null(result["rawInput"]);
            Assert.Null(result["rawOutput"]);
            Assert.Null(result["outputText"]);
        });
    }

    [Fact]
    public void CandidateEnvelopeSchema_IsDraft202012AndClosed()
    {
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(
            TestPaths.WorkerSchemaDirectory(),
            "evaluation-candidate-output-envelope.schema.json")))!.AsObject();

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", (string?)schema["$schema"]);
        Assert.Equal(
            "biostack-evaluation-candidate-output-envelope",
            (string?)schema["properties"]!["recordType"]!["const"]);
        Assert.False((bool)schema["additionalProperties"]!);
        Assert.Equal(20, (int)schema["properties"]!["results"]!["maxItems"]!);
    }

    [Fact]
    public void Load_UnknownAdversarialCase_FailsClosed()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        var artifact = repository.ReadArtifact();
        artifact["results"]!.AsArray()[^1]!["caseId"] = "adversarial-999-unknown";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load());

        Assert.Contains("unknown KEO-77 case", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_UnorderedResults_FailsClosed()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        var artifact = repository.ReadArtifact();
        var results = artifact["results"]!.AsArray();
        var first = results[0]!.DeepClone();
        var second = results[1]!.DeepClone();
        results[0] = second;
        results[1] = first;
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load());

        Assert.Contains("unique and sorted", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_VersionMismatch_FailsClosed()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        var artifact = repository.ReadArtifact();
        artifact["adversarialCorpusVersion"] = "9.9.9";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load());

        Assert.Contains("adversarialCorpusVersion", error.Message, StringComparison.Ordinal);
        Assert.Contains("9.9.9", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RawOutputProperty_FailsSchemaValidation()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        var artifact = repository.ReadArtifact();
        artifact["results"]!.AsArray()[0]!["rawOutput"] = "not allowed";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load());

        Assert.Contains("schema validation", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_AlternateSourcePath_FailsSchemaValidation()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        var artifact = repository.ReadArtifact();
        artifact["sourceResearch"] = "../outside.md";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load());

        Assert.Contains("schema validation", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_OversizedEnvelope_FailsBeforeParsing()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        repository.WriteRawArtifact(new string('x', (int)EvaluationCandidateOutputEnvelopeLoader.MaximumEnvelopeBytes + 1));

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load());

        Assert.Contains("offline limit", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_CandidateDeclarationDisagreesWithExpectedTruth_RemainsUntrustedData()
    {
        using var repository = TemporaryCandidateEnvelopeRepository.Create();
        var artifact = repository.ReadArtifact();
        artifact["results"]!.AsArray()[0]!["candidateDeclarations"]!["answerDisposition"] = "unknown";
        repository.WriteArtifact(artifact);

        var envelope = new EvaluationCandidateOutputEnvelopeLoader(repository.Root).Load();

        Assert.Equal("unknown", envelope.Results[0].Declarations.AnswerDisposition);
        Assert.False(envelope.RuntimeTruth);
        Assert.Equal("none", envelope.EffectAuthority);
    }

    private static JsonObject ReadCurrentArtifact()
        => JsonNode.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "research",
            "protocol-intelligence",
            "evaluation-candidate-output-envelope.v1.json")))!.AsObject();

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) &&
                Directory.Exists(Path.Combine(directory.FullName, "research")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the BioStack repository root.");
    }

    private sealed class TemporaryCandidateEnvelopeRepository : IDisposable
    {
        private static readonly string[] ArtifactFiles =
        [
            "adversarial-query-corpus.v1.json",
            "evaluation-candidate-output-envelope.v1.json",
            "evaluation-coverage-matrix.json",
            "evaluation-taxonomy.json",
            "protocol-design-fixture-corpus.v1.json",
        ];

        private static readonly string[] SchemaFiles =
        [
            "adversarial-query-corpus.schema.json",
            "evaluation-candidate-output-envelope.schema.json",
            "evaluation-coverage-matrix.schema.json",
            "evaluation-taxonomy.schema.json",
            "protocol-design-fixture-corpus.schema.json",
        ];

        private TemporaryCandidateEnvelopeRepository(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TemporaryCandidateEnvelopeRepository Create()
        {
            var sourceRoot = RepositoryRoot();
            var root = Path.Combine(Path.GetTempPath(), $"biostack-keo78-envelope-{Guid.NewGuid():N}");
            var artifactDirectory = Path.Combine(root, "research", "protocol-intelligence");
            var schemaDirectory = Path.Combine(root, "backend", "src", "BioStack.KnowledgeWorker", "Schemas");
            var canonDirectory = Path.Combine(root, "docs", "canon");
            var testingDirectory = Path.Combine(root, "docs", "testing");
            Directory.CreateDirectory(artifactDirectory);
            Directory.CreateDirectory(schemaDirectory);
            Directory.CreateDirectory(canonDirectory);
            Directory.CreateDirectory(testingDirectory);

            foreach (var fileName in ArtifactFiles)
            {
                File.Copy(
                    Path.Combine(sourceRoot, "research", "protocol-intelligence", fileName),
                    Path.Combine(artifactDirectory, fileName));
            }

            foreach (var fileName in SchemaFiles)
            {
                File.Copy(
                    Path.Combine(sourceRoot, "backend", "src", "BioStack.KnowledgeWorker", "Schemas", fileName),
                    Path.Combine(schemaDirectory, fileName));
            }

            File.Copy(
                Path.Combine(sourceRoot, "docs", "canon", "biostack-protocol-intelligence-canon.md"),
                Path.Combine(canonDirectory, "biostack-protocol-intelligence-canon.md"));
            File.Copy(
                Path.Combine(sourceRoot, "docs", "testing", "knowledge-engine-evaluation-harness.md"),
                Path.Combine(testingDirectory, "knowledge-engine-evaluation-harness.md"));

            return new TemporaryCandidateEnvelopeRepository(root);
        }

        public JsonObject ReadArtifact()
            => JsonNode.Parse(File.ReadAllText(ArtifactPath()))!.AsObject();

        public void WriteArtifact(JsonObject artifact)
            => File.WriteAllText(ArtifactPath(), artifact.ToJsonString(new() { WriteIndented = true }));

        public void WriteRawArtifact(string value)
            => File.WriteAllText(ArtifactPath(), value);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private string ArtifactPath()
            => Path.Combine(
                Root,
                "research",
                "protocol-intelligence",
                "evaluation-candidate-output-envelope.v1.json");
    }
}
