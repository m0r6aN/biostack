namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.Contracts.Responses;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class EvaluationCoverageArtifactLoaderTests
{
    private static readonly string[] MandatoryDimensions =
    [
        "evidence_type",
        "goal",
        "input_quality",
        "interaction",
        "language",
        "lifecycle_stage",
        "persona",
        "protocol_structure",
        "question_intent",
        "reading_level",
        "reference_query",
        "refusal_class",
        "schedule",
        "substance",
        "uncertainty",
        "unit",
    ];

    [Fact]
    public void Load_CurrentArtifacts_ValidatesMandatoryDimensionsFixturesOwnersAndPairs()
    {
        var artifacts = new EvaluationCoverageArtifactLoader().Load();

        Assert.Equal("1.0.0", artifacts.TaxonomyVersion);
        Assert.Equal("1.0.0", artifacts.MatrixVersion);
        Assert.Equal(MandatoryDimensions, artifacts.DimensionIds);
        Assert.Equal(16, artifacts.CoverageCaseIds.Count);
        Assert.Equal(8, artifacts.RequiredValuePairIds.Count);
        Assert.All(artifacts.OwnerRoleIds, roleId => Assert.StartsWith("role:pending:", roleId));
    }

    [Fact]
    public void CurrentTaxonomy_PreservesRuntimeSafetyStatusAndKeepsUnknownAsDisposition()
    {
        var taxonomy = ReadArtifact("evaluation-taxonomy.json");
        var safetyStatuses = taxonomy["safetyStatuses"]!.AsArray()
            .Select(item => item!["id"]!.GetValue<string>())
            .ToArray();
        var dispositions = taxonomy["answerDispositions"]!.AsArray()
            .Select(item => item!["id"]!.GetValue<string>())
            .ToArray();

        Assert.Equal(
            [SafetyStatus.Allowed, SafetyStatus.Constrained, SafetyStatus.Refused, SafetyStatus.Warning],
            safetyStatuses);
        Assert.DoesNotContain("unknown", safetyStatuses);
        Assert.Contains("unknown", dispositions);
    }

    [Theory]
    [InlineData("evaluation-taxonomy.schema.json", "biostack-evaluation-taxonomy")]
    [InlineData("evaluation-coverage-matrix.schema.json", "biostack-evaluation-coverage-matrix")]
    public void EvaluationSchemas_AreDraft202012AndDeclareRecordType(string fileName, string recordType)
    {
        var schemaPath = Path.Combine(TestPaths.WorkerSchemaDirectory(), fileName);
        var schema = JsonNode.Parse(File.ReadAllText(schemaPath))!.AsObject();

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", (string?)schema["$schema"]);
        Assert.Equal(recordType, (string?)schema["properties"]!["recordType"]!["const"]);
    }

    [Fact]
    public void Load_ChangedSafetyStatus_FailsClosed()
    {
        using var repository = TemporaryEvaluationRepository.Create();
        var taxonomy = repository.ReadArtifact("evaluation-taxonomy.json");
        taxonomy["safetyStatuses"]!.AsArray()[0]!["id"] = "bogus";
        repository.WriteArtifact("evaluation-taxonomy.json", taxonomy);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCoverageArtifactLoader(repository.Root).Load());

        Assert.Contains("safetyStatuses", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_UnsortedCases_FailsClosed()
    {
        using var repository = TemporaryEvaluationRepository.Create();
        var matrix = repository.ReadArtifact("evaluation-coverage-matrix.json");
        var cases = matrix["coverageCases"]!.AsArray();
        var first = cases[0]!.DeepClone();
        var second = cases[1]!.DeepClone();
        cases[0] = second;
        cases[1] = first;
        repository.WriteArtifact("evaluation-coverage-matrix.json", matrix);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCoverageArtifactLoader(repository.Root).Load());

        Assert.Contains("ordinal ordering", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RequiredPairNotCoveredByDeclaredCase_FailsClosed()
    {
        using var repository = TemporaryEvaluationRepository.Create();
        var matrix = repository.ReadArtifact("evaluation-coverage-matrix.json");
        matrix["requiredValuePairs"]!.AsArray()[0]!["coveredByCaseId"] = "case-001-supported-evidence";
        repository.WriteArtifact("evaluation-coverage-matrix.json", matrix);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCoverageArtifactLoader(repository.Root).Load());

        Assert.Contains("not covered", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_MissingRepresentativeDimensionValue_FailsClosed()
    {
        using var repository = TemporaryEvaluationRepository.Create();
        var matrix = repository.ReadArtifact("evaluation-coverage-matrix.json");
        foreach (var coverageCase in matrix["coverageCases"]!.AsArray())
        {
            if ((string?)coverageCase!["selections"]!["language"] == "es")
            {
                coverageCase["selections"]!["language"] = "en";
            }
        }
        repository.WriteArtifact("evaluation-coverage-matrix.json", matrix);

        var error = Assert.Throws<InvalidOperationException>(
            () => new EvaluationCoverageArtifactLoader(repository.Root).Load());

        Assert.Contains("represented values for language", error.Message, StringComparison.Ordinal);
    }

    private static JsonObject ReadArtifact(string fileName)
        => JsonNode.Parse(File.ReadAllText(Path.Combine(ArtifactDirectory(RepositoryRoot()), fileName)))!.AsObject();

    private static string ArtifactDirectory(string root)
        => Path.Combine(root, "research", "protocol-intelligence");

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

    private sealed class TemporaryEvaluationRepository : IDisposable
    {
        private static readonly string[] ArtifactFiles =
        [
            "evaluation-coverage-matrix.json",
            "evaluation-taxonomy.json",
        ];

        private static readonly string[] SchemaFiles =
        [
            "evaluation-coverage-matrix.schema.json",
            "evaluation-taxonomy.schema.json",
        ];

        private TemporaryEvaluationRepository(string root)
        {
            Root = root;
        }

        public string Root { get; }

        public static TemporaryEvaluationRepository Create()
        {
            var sourceRoot = RepositoryRoot();
            var root = Path.Combine(Path.GetTempPath(), $"biostack-keo73-{Guid.NewGuid():N}");
            var artifactDirectory = Path.Combine(root, "research", "protocol-intelligence");
            var schemaDirectory = Path.Combine(root, "backend", "src", "BioStack.KnowledgeWorker", "Schemas");
            var documentationDirectory = Path.Combine(root, "docs", "testing");
            Directory.CreateDirectory(artifactDirectory);
            Directory.CreateDirectory(schemaDirectory);
            Directory.CreateDirectory(documentationDirectory);

            foreach (var fileName in ArtifactFiles)
            {
                File.Copy(
                    Path.Combine(ArtifactDirectory(sourceRoot), fileName),
                    Path.Combine(artifactDirectory, fileName));
            }

            foreach (var fileName in SchemaFiles)
            {
                File.Copy(
                    Path.Combine(sourceRoot, "backend", "src", "BioStack.KnowledgeWorker", "Schemas", fileName),
                    Path.Combine(schemaDirectory, fileName));
            }

            File.WriteAllText(
                Path.Combine(documentationDirectory, "knowledge-engine-evaluation-harness.md"),
                "Temporary KEO-73 source research fixture.");

            return new TemporaryEvaluationRepository(root);
        }

        public JsonObject ReadArtifact(string fileName)
            => JsonNode.Parse(File.ReadAllText(Path.Combine(ArtifactDirectory(Root), fileName)))!.AsObject();

        public void WriteArtifact(string fileName, JsonObject artifact)
            => File.WriteAllText(
                Path.Combine(ArtifactDirectory(Root), fileName),
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
