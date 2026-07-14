namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

/// <summary>
/// Loads a metadata-only, pre-recorded candidate-output envelope for offline evaluation.
/// Candidate declarations are untrusted observations, not truth or effect authority.
/// This loader never invokes a model, accesses a network, or reads raw prompt/output text.
/// </summary>
public sealed class EvaluationCandidateOutputEnvelopeLoader
{
    public const long MaximumEnvelopeBytes = 1_048_576;
    public const int MaximumJsonDepth = 32;

    private const string RequiredSourceResearch = "docs/testing/knowledge-engine-evaluation-harness.md";
    private const string RequiredSourceCanon = "docs/canon/biostack-protocol-intelligence-canon.md";

    private static readonly EvaluationOptions SchemaOptions = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private readonly string _repositoryRoot;
    private EvaluationCandidateOutputEnvelope? _cached;

    public EvaluationCandidateOutputEnvelopeLoader(string? repositoryRoot = null)
    {
        _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
            ? LocateRepositoryRoot()
            : Path.GetFullPath(repositoryRoot);
    }

    public EvaluationCandidateOutputEnvelope Load()
        => _cached ??= LoadCore();

    private EvaluationCandidateOutputEnvelope LoadCore()
    {
        var artifactDirectory = Path.Combine(_repositoryRoot, "research", "protocol-intelligence");
        var schemaDirectory = Path.Combine(
            _repositoryRoot,
            "backend",
            "src",
            "BioStack.KnowledgeWorker",
            "Schemas");
        var artifact = ReadAndValidate(
            Path.Combine(artifactDirectory, "evaluation-candidate-output-envelope.v1.json"),
            Path.Combine(schemaDirectory, "evaluation-candidate-output-envelope.schema.json"));

        var coverage = new EvaluationCoverageArtifactLoader(_repositoryRoot).Load();
        var fixtures = new ProtocolDesignFixtureCorpusLoader(_repositoryRoot).Load();
        var adversarial = new AdversarialQueryCorpusLoader(_repositoryRoot).Load();
        EnsureEqual(RequiredString(artifact, "taxonomyVersion"), coverage.TaxonomyVersion, "taxonomyVersion");
        EnsureEqual(RequiredString(artifact, "matrixVersion"), coverage.MatrixVersion, "matrixVersion");
        EnsureEqual(
            RequiredString(artifact, "fixtureCorpusVersion"),
            fixtures.ArtifactVersion,
            "fixtureCorpusVersion");
        EnsureEqual(
            RequiredString(artifact, "adversarialCorpusVersion"),
            adversarial.ArtifactVersion,
            "adversarialCorpusVersion");
        ValidatePinnedSource(artifact, "sourceResearch", RequiredSourceResearch);
        ValidatePinnedSource(artifact, "sourceCanon", RequiredSourceCanon);

        ValidateDataPolicy(RequiredObject(artifact["dataPolicy"], "dataPolicy"));
        if (RequiredBoolean(artifact, "runtimeTruth"))
        {
            throw ContractError("runtimeTruth must remain false.");
        }

        var knownCaseIds = adversarial.CaseIds.ToHashSet(StringComparer.Ordinal);
        var results = RequiredArray(artifact, "results");
        EnsureOrdered(results, "results", result => RequiredString(RequiredObject(result, "result"), "caseId"));

        var loadedResults = new List<EvaluationCandidateCaseResult>(results.Count);
        foreach (var resultNode in results)
        {
            var result = RequiredObject(resultNode, "result");
            var caseId = RequiredString(result, "caseId");
            if (!knownCaseIds.Contains(caseId))
            {
                throw ContractError($"result references unknown KEO-77 case '{caseId}'.");
            }

            var declarations = RequiredObject(result["candidateDeclarations"], $"{caseId}.candidateDeclarations");
            var receipt = RequiredObject(declarations["receipt"], $"{caseId}.candidateDeclarations.receipt");
            var decisionCodes = RequiredArray(receipt, "decisionCodes");
            EnsureOrdered(decisionCodes, $"{caseId}.candidateDeclarations.receipt.decisionCodes", StringValue);
            var citations = RequiredObject(
                declarations["citations"],
                $"{caseId}.candidateDeclarations.citations");
            var sourceIds = RequiredArray(citations, "sourceIds");
            EnsureOrdered(sourceIds, $"{caseId}.candidateDeclarations.citations.sourceIds", StringValue);

            loadedResults.Add(new EvaluationCandidateCaseResult(
                CaseId: caseId,
                CandidateOutputSha256: RequiredString(result, "candidateOutputSha256"),
                Declarations: new EvaluationCandidateDeclarations(
                    AnswerDisposition: RequiredString(declarations, "answerDisposition"),
                    SafetyStatus: RequiredString(declarations, "safetyStatus"),
                    HandlingClass: RequiredString(declarations, "handlingClass"),
                    HumanReviewRequired: RequiredBoolean(declarations, "humanReviewRequired"),
                    ReceiptEventClass: RequiredString(receipt, "eventClass"),
                    ReceiptDecisionCodes: decisionCodes.Select(StringValue).ToArray(),
                    CitationMode: RequiredString(citations, "mode"),
                    CitationSourceIds: sourceIds.Select(StringValue).ToArray())));
        }

        return new EvaluationCandidateOutputEnvelope(
            ArtifactVersion: RequiredString(artifact, "artifactVersion"),
            TaxonomyVersion: RequiredString(artifact, "taxonomyVersion"),
            MatrixVersion: RequiredString(artifact, "matrixVersion"),
            FixtureCorpusVersion: RequiredString(artifact, "fixtureCorpusVersion"),
            AdversarialCorpusVersion: RequiredString(artifact, "adversarialCorpusVersion"),
            EnvelopePurpose: RequiredString(artifact, "envelopePurpose"),
            CandidateConfigurationSha256: RequiredString(artifact, "candidateConfigurationSha256"),
            Results: loadedResults,
            RuntimeTruth: false,
            ModelInvokedByLoader: false,
            NetworkAccessedByLoader: false,
            EffectAuthority: "none");
    }

    private static void ValidateDataPolicy(JsonObject policy)
    {
        EnsureEqual(RequiredString(policy, "identityPolicy"), "synthetic-only", "dataPolicy.identityPolicy");
        EnsureFalse(policy, "rawInputsIncluded");
        EnsureFalse(policy, "rawOutputsIncluded");
        EnsureFalse(policy, "customerDataIncluded");
        EnsureFalse(policy, "modelInvocationPerformedByLoader");
        EnsureFalse(policy, "networkAccessPerformedByLoader");
        EnsureEqual(RequiredString(policy, "effectAuthority"), "none", "dataPolicy.effectAuthority");
    }

    private void ValidatePinnedSource(JsonObject artifact, string propertyName, string expectedRelativePath)
    {
        EnsureEqual(RequiredString(artifact, propertyName), expectedRelativePath, propertyName);
        if (!File.Exists(Path.Combine(_repositoryRoot, expectedRelativePath)))
        {
            throw ContractError($"{propertyName} path does not exist: {expectedRelativePath}");
        }
    }

    private static JsonObject ReadAndValidate(string artifactPath, string schemaPath)
    {
        if (!File.Exists(artifactPath))
        {
            throw new FileNotFoundException($"Candidate envelope not found at '{artifactPath}'.", artifactPath);
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Candidate envelope schema not found at '{schemaPath}'.", schemaPath);
        }

        var artifactInfo = new FileInfo(artifactPath);
        if (artifactInfo.Length > MaximumEnvelopeBytes)
        {
            throw ContractError(
                $"candidate envelope exceeds the {MaximumEnvelopeBytes}-byte offline limit.");
        }

        JsonNode artifact;
        try
        {
            artifact = JsonNode.Parse(
                File.ReadAllText(artifactPath),
                documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = MaximumJsonDepth,
                }) ?? throw ContractError($"Candidate envelope is empty: {artifactPath}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw ContractError($"Candidate envelope is malformed: {artifactPath}", ex);
        }

        var result = JsonSchema.FromFile(schemaPath).Evaluate(artifact, SchemaOptions);
        if (!result.IsValid)
        {
            throw ContractError($"Candidate envelope failed schema validation: {artifactPath}");
        }

        return RequiredObject(artifact, Path.GetFileName(artifactPath));
    }

    private static void EnsureFalse(JsonObject root, string propertyName)
    {
        if (RequiredBoolean(root, propertyName))
        {
            throw ContractError($"{propertyName} must remain false.");
        }
    }

    private static void EnsureEqual(string actual, string expected, string label)
    {
        if (!StringComparer.Ordinal.Equals(actual, expected))
        {
            throw ContractError($"{label} must be '{expected}', actual '{actual}'.");
        }
    }

    private static void EnsureOrdered(JsonArray items, string label, Func<JsonNode?, string> selector)
    {
        var actual = items.Select(selector).ToArray();
        var expected = actual.Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal) ||
            actual.Distinct(StringComparer.Ordinal).Count() != actual.Length)
        {
            throw ContractError($"{label} must be unique and sorted with ordinal ordering.");
        }
    }

    private static string StringValue(JsonNode? node)
        => node?.GetValue<string>() is { Length: > 0 } value
            ? value
            : throw ContractError("Required string array value is missing.");

    private static JsonArray RequiredArray(JsonObject root, string propertyName)
        => root[propertyName] is JsonArray array
            ? array
            : throw ContractError($"Required array '{propertyName}' is missing.");

    private static JsonObject RequiredObject(JsonNode? node, string label)
        => node is JsonObject obj
            ? obj
            : throw ContractError($"Required object '{label}' is missing.");

    private static string RequiredString(JsonObject root, string propertyName)
        => root[propertyName]?.GetValue<string>() is { Length: > 0 } value
            ? value
            : throw ContractError($"Required string '{propertyName}' is missing.");

    private static bool RequiredBoolean(JsonObject root, string propertyName)
        => root[propertyName]?.GetValue<bool>()
            ?? throw ContractError($"Required boolean '{propertyName}' is missing.");

    private static InvalidOperationException ContractError(string message, Exception? inner = null)
        => new($"KEO-78 candidate output envelope invalid: {message}", inner);

    private static string LocateRepositoryRoot()
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
}

public sealed record EvaluationCandidateOutputEnvelope(
    string ArtifactVersion,
    string TaxonomyVersion,
    string MatrixVersion,
    string FixtureCorpusVersion,
    string AdversarialCorpusVersion,
    string EnvelopePurpose,
    string CandidateConfigurationSha256,
    IReadOnlyList<EvaluationCandidateCaseResult> Results,
    bool RuntimeTruth,
    bool ModelInvokedByLoader,
    bool NetworkAccessedByLoader,
    string EffectAuthority);

public sealed record EvaluationCandidateCaseResult(
    string CaseId,
    string CandidateOutputSha256,
    EvaluationCandidateDeclarations Declarations);

public sealed record EvaluationCandidateDeclarations(
    string AnswerDisposition,
    string SafetyStatus,
    string HandlingClass,
    bool HumanReviewRequired,
    string ReceiptEventClass,
    IReadOnlyList<string> ReceiptDecisionCodes,
    string CitationMode,
    IReadOnlyList<string> CitationSourceIds);
