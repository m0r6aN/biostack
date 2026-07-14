namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class ProtocolDesignFixtureCorpusLoaderTests
{
    private static readonly string[] ExpectedInputTypes =
    [
        "camera_scan",
        "file_upload",
        "link",
        "paste",
    ];

    private static readonly string[] ExpectedBehaviorClasses =
    [
        "normalization",
        "observation",
        "refusal",
        "uncertainty",
        "warning",
    ];

    [Fact]
    public void Load_CurrentCorpus_CoversEveryTaxonomyCaseInputBehaviorAndDesignEdge()
    {
        var corpus = new ProtocolDesignFixtureCorpusLoader().Load();

        Assert.Equal("1.0.0", corpus.ArtifactVersion);
        Assert.Equal("1.0.0", corpus.TaxonomyVersion);
        Assert.Equal("1.0.0", corpus.MatrixVersion);
        Assert.Equal(20, corpus.FixtureIds.Count);
        Assert.Equal(16, corpus.CoverageCaseIds.Count);
        Assert.Equal(ExpectedInputTypes, corpus.InputTypes);
        Assert.Equal(ExpectedBehaviorClasses, corpus.BehaviorClasses);
        Assert.Equal(24, corpus.CoverageTags.Count);
        Assert.All(corpus.OwnerRoleIds, roleId => Assert.StartsWith("role:pending:", roleId));
    }

    [Fact]
    public void CurrentCorpus_DeclaresSyntheticOfflinePolicy()
    {
        var corpus = ReadCurrentArtifact();
        var policy = corpus["dataPolicy"]!.AsObject();

        Assert.False((bool)corpus["runtimeTruth"]!);
        Assert.Equal("synthetic-only", (string?)policy["identityPolicy"]);
        Assert.Equal("unsafe-requests-only-as-refusal-inputs", (string?)policy["medicalInstructionPolicy"]);
        Assert.False((bool)policy["modelInvocationExpected"]!);
        Assert.False((bool)policy["networkAccessExpected"]!);
    }

    [Fact]
    public void ProtocolFixtureSchema_IsDraft202012AndDeclaresRecordType()
    {
        var schemaPath = Path.Combine(
            TestPaths.WorkerSchemaDirectory(),
            "protocol-design-fixture-corpus.schema.json");
        var schema = JsonNode.Parse(File.ReadAllText(schemaPath))!.AsObject();

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", (string?)schema["$schema"]);
        Assert.Equal(
            "biostack-protocol-design-fixture-corpus",
            (string?)schema["properties"]!["recordType"]!["const"]);
    }

    [Fact]
    public void Load_UnsortedFixtures_FailsClosed()
    {
        using var repository = TemporaryProtocolFixtureRepository.Create();
        var corpus = repository.ReadCorpus();
        var fixtures = corpus["fixtures"]!.AsArray();
        var first = fixtures[0]!.DeepClone();
        var second = fixtures[1]!.DeepClone();
        fixtures[0] = second;
        fixtures[1] = first;
        repository.WriteCorpus(corpus);

        var error = Assert.Throws<InvalidOperationException>(
            () => new ProtocolDesignFixtureCorpusLoader(repository.Root).Load());

        Assert.Contains("ordinal ordering", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MissingTaxonomyCase_FailsClosed()
    {
        using var repository = TemporaryProtocolFixtureRepository.Create();
        var corpus = repository.ReadCorpus();
        FindFixture(corpus, "fixture-016-treatment-plan-refusal")["coverageCaseId"] = "case-001-supported-evidence";
        var expected = FindFixture(corpus, "fixture-016-treatment-plan-refusal")["expected"]!.AsObject();
        expected["answerDisposition"] = "supported";
        expected["safetyStatus"] = "allowed";
        expected["behaviorClass"] = "observation";
        FindFixture(corpus, "fixture-016-treatment-plan-refusal")["unsafeRequest"] = false;
        repository.WriteCorpus(corpus);

        var error = Assert.Throws<InvalidOperationException>(
            () => new ProtocolDesignFixtureCorpusLoader(repository.Root).Load());

        Assert.Contains("KEO-73 coverage cases", error.Message, StringComparison.Ordinal);
        Assert.Contains("case-016-refuse-treatment-plan", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RefusalWithoutUnsafeFlag_FailsClosed()
    {
        using var repository = TemporaryProtocolFixtureRepository.Create();
        var corpus = repository.ReadCorpus();
        FindFixture(corpus, "fixture-010-dosing-request-refusal")["unsafeRequest"] = false;
        repository.WriteCorpus(corpus);

        var error = Assert.Throws<InvalidOperationException>(
            () => new ProtocolDesignFixtureCorpusLoader(repository.Root).Load());

        Assert.Contains("unsafeRequest", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_LinkFixtureOutsideReservedOfflineDomain_FailsClosed()
    {
        using var repository = TemporaryProtocolFixtureRepository.Create();
        var corpus = repository.ReadCorpus();
        FindFixture(corpus, "fixture-004-longitudinal-provider-summary-link")["input"]!["linkUrl"] =
            "https://example.com/protocol.txt";
        repository.WriteCorpus(corpus);

        var error = Assert.Throws<InvalidOperationException>(
            () => new ProtocolDesignFixtureCorpusLoader(repository.Root).Load());

        Assert.Contains("reserved offline .invalid", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_PersonalDataLikeInput_FailsClosed()
    {
        using var repository = TemporaryProtocolFixtureRepository.Create();
        var corpus = repository.ReadCorpus();
        FindFixture(corpus, "fixture-001-single-daily-record")["input"]!["request"] =
            "Send this observation to synthetic.person@example.com.";
        repository.WriteCorpus(corpus);

        var error = Assert.Throws<InvalidOperationException>(
            () => new ProtocolDesignFixtureCorpusLoader(repository.Root).Load());

        Assert.Contains("personal data", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_PrescriptiveExpectedPhrase_FailsClosed()
    {
        using var repository = TemporaryProtocolFixtureRepository.Create();
        var corpus = repository.ReadCorpus();
        FindFixture(corpus, "fixture-001-single-daily-record")["expected"]!["summary"] =
            "You should take the recorded amount.";
        repository.WriteCorpus(corpus);

        var error = Assert.Throws<InvalidOperationException>(
            () => new ProtocolDesignFixtureCorpusLoader(repository.Root).Load());

        Assert.Contains("forbidden prescriptive phrase", error.Message, StringComparison.Ordinal);
    }

    private static JsonObject FindFixture(JsonObject corpus, string fixtureId)
        => corpus["fixtures"]!.AsArray()
            .Select(node => node!.AsObject())
            .Single(item => (string?)item["id"] == fixtureId);

    private static JsonObject ReadCurrentArtifact()
        => JsonNode.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "research",
            "protocol-intelligence",
            "protocol-design-fixture-corpus.v1.json")))!.AsObject();

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

    private sealed class TemporaryProtocolFixtureRepository : IDisposable
    {
        private static readonly string[] ArtifactFiles =
        [
            "evaluation-coverage-matrix.json",
            "evaluation-taxonomy.json",
            "protocol-design-fixture-corpus.v1.json",
        ];

        private static readonly string[] SchemaFiles =
        [
            "evaluation-coverage-matrix.schema.json",
            "evaluation-taxonomy.schema.json",
            "protocol-design-fixture-corpus.schema.json",
        ];

        private TemporaryProtocolFixtureRepository(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TemporaryProtocolFixtureRepository Create()
        {
            var sourceRoot = RepositoryRoot();
            var root = Path.Combine(Path.GetTempPath(), $"biostack-keo76-{Guid.NewGuid():N}");
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

            return new TemporaryProtocolFixtureRepository(root);
        }

        public JsonObject ReadCorpus()
            => JsonNode.Parse(File.ReadAllText(Path.Combine(
                Root,
                "research",
                "protocol-intelligence",
                "protocol-design-fixture-corpus.v1.json")))!.AsObject();

        public void WriteCorpus(JsonObject corpus)
            => File.WriteAllText(
                Path.Combine(
                    Root,
                    "research",
                    "protocol-intelligence",
                    "protocol-design-fixture-corpus.v1.json"),
                corpus.ToJsonString(new() { WriteIndented = true }));

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
