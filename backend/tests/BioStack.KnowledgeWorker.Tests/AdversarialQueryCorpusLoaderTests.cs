namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class AdversarialQueryCorpusLoaderTests
{
    private static readonly string[] ExpectedThreatClasses =
    [
        "ambiguous",
        "benign",
        "contradictory_context",
        "malicious_prompt_injection",
        "out_of_scope",
        "policy_evasion",
        "privacy_sensitive",
        "unsafe_request",
        "unsupported_claim",
    ];

    [Fact]
    public void Load_CurrentCorpus_CoversEveryTaxonomyCaseAndThreatClass()
    {
        var corpus = new AdversarialQueryCorpusLoader().Load();

        Assert.Equal("1.0.0", corpus.ArtifactVersion);
        Assert.Equal("1.0.0", corpus.TaxonomyVersion);
        Assert.Equal("1.0.0", corpus.MatrixVersion);
        Assert.Equal("1.0.0", corpus.FixtureCorpusVersion);
        Assert.Equal(20, corpus.CaseIds.Count);
        Assert.Equal(16, corpus.CoverageCaseIds.Count);
        Assert.Equal(ExpectedThreatClasses, corpus.ThreatClasses);
        Assert.Equal(["safety_refusal", "supported", "unknown", "unsupported"], corpus.AnswerDispositions);
        Assert.Equal(["allowed", "constrained", "refused", "warning"], corpus.SafetyStatuses);
        Assert.True(corpus.LongTailCaseCount >= ExpectedThreatClasses.Length);
        Assert.All(corpus.OwnerRoleIds, roleId => Assert.StartsWith("role:pending:", roleId));
    }

    [Fact]
    public void CurrentCorpus_DeclaresSyntheticOfflineNoModelPolicy()
    {
        var artifact = ReadCurrentArtifact();
        var policy = artifact["dataPolicy"]!.AsObject();

        Assert.False((bool)artifact["runtimeTruth"]!);
        Assert.Equal("pending-human-assignment", (string?)artifact["ownerAssignmentState"]);
        Assert.Equal("synthetic-only", (string?)policy["identityPolicy"]);
        Assert.Equal("non-operational-test-inputs-only", (string?)policy["unsafeContentPolicy"]);
        Assert.Equal(
            "non-prescriptive-refusal-or-evidence-bounded",
            (string?)policy["expectedOutputPolicy"]);
        Assert.False((bool)policy["modelInvocationExpected"]!);
        Assert.False((bool)policy["networkAccessExpected"]!);
    }

    [Fact]
    public void AdversarialCorpusSchema_IsDraft202012AndDeclaresRecordType()
    {
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(
            TestPaths.WorkerSchemaDirectory(),
            "adversarial-query-corpus.schema.json")))!.AsObject();

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", (string?)schema["$schema"]);
        Assert.Equal(
            "biostack-adversarial-query-corpus",
            (string?)schema["properties"]!["recordType"]!["const"]);
    }

    [Fact]
    public void Load_MissingThreatClass_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-018-unsupported-claim")["threatClass"] = "ambiguous";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("threat classes", error.Message, StringComparison.Ordinal);
        Assert.Contains("unsupported_claim", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_UnknownFixtureLink_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-001-benign-evidence")["relatedFixtureIds"] =
            new JsonArray("fixture-999-not-declared");
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("unknown KEO-76 fixture", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RefusalWithoutRefusedSafetyStatus_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-010-dose-prompt-injection")["expected"]!["safetyStatus"] = "allowed";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("safetyStatus", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_UnknownCitationSource_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-001-benign-evidence")["expected"]!["citations"]!["sourceIds"] =
            new JsonArray("source-not-declared");
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("unknown synthetic source", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_PersonalDataLikeInput_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-017-privacy-sensitive-summary")["input"]!["query"] =
            "Summarize this note for private.person@example.com.";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("personal data", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_PrescriptiveExpectedPhrase_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-001-benign-evidence")["expected"]!["refusal"]!["safeAlternative"] =
            "You should take the synthetic item.";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("forbidden prescriptive phrase", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_ReceiptEventThatDisagreesWithSafetyStatus_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-004-contradictory-record")["expected"]!["receipt"]!["eventClass"] =
            "allowed_answer";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("receipt.eventClass", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_UnsupportedClaimThatAllowsCitation_FailsClosed()
    {
        using var repository = TemporaryAdversarialCorpusRepository.Create();
        var artifact = repository.ReadArtifact();
        FindCase(artifact, "adversarial-018-unsupported-claim")["expected"]!["citations"]!["mode"] = "none";
        repository.WriteArtifact(artifact);

        var error = Assert.Throws<InvalidOperationException>(
            () => new AdversarialQueryCorpusLoader(repository.Root).Load());

        Assert.Contains("unsupported claims", error.Message, StringComparison.Ordinal);
    }

    private static JsonObject FindCase(JsonObject artifact, string caseId)
        => artifact["cases"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(item => (string?)item["id"] == caseId);

    private static JsonObject ReadCurrentArtifact()
        => JsonNode.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "research",
            "protocol-intelligence",
            "adversarial-query-corpus.v1.json")))!.AsObject();

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

    private sealed class TemporaryAdversarialCorpusRepository : IDisposable
    {
        private static readonly string[] ArtifactFiles =
        [
            "adversarial-query-corpus.v1.json",
            "evaluation-coverage-matrix.json",
            "evaluation-taxonomy.json",
            "protocol-design-fixture-corpus.v1.json",
        ];

        private static readonly string[] SchemaFiles =
        [
            "adversarial-query-corpus.schema.json",
            "evaluation-coverage-matrix.schema.json",
            "evaluation-taxonomy.schema.json",
            "protocol-design-fixture-corpus.schema.json",
        ];

        private TemporaryAdversarialCorpusRepository(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TemporaryAdversarialCorpusRepository Create()
        {
            var sourceRoot = RepositoryRoot();
            var root = Path.Combine(Path.GetTempPath(), $"biostack-keo77-{Guid.NewGuid():N}");
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

            return new TemporaryAdversarialCorpusRepository(root);
        }

        public JsonObject ReadArtifact()
            => JsonNode.Parse(File.ReadAllText(Path.Combine(
                Root,
                "research",
                "protocol-intelligence",
                "adversarial-query-corpus.v1.json")))!.AsObject();

        public void WriteArtifact(JsonObject artifact)
            => File.WriteAllText(
                Path.Combine(
                    Root,
                    "research",
                    "protocol-intelligence",
                    "adversarial-query-corpus.v1.json"),
                artifact.ToJsonString(new() { WriteIndented = true }));

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
