namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;

/// <summary>
/// Loads and validates the offline KEO-76 protocol-design fixture corpus.
/// The corpus is synthetic evaluation input only; this loader is not registered
/// with DI and never invokes models, performs network access, or changes runtime
/// Protocol Intelligence behavior.
/// </summary>
public sealed partial class ProtocolDesignFixtureCorpusLoader
{
    private static readonly EvaluationOptions SchemaOptions = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private static readonly string[] RequiredInputTypes =
    [
        "camera_scan",
        "file_upload",
        "link",
        "paste",
    ];

    private static readonly string[] RequiredBehaviorClasses =
    [
        "normalization",
        "observation",
        "refusal",
        "uncertainty",
        "warning",
    ];

    private static readonly string[] RequiredCoverageTags =
    [
        "blend",
        "contradictory_value",
        "cyclic",
        "dose_entry_decimal",
        "dose_entry_fraction",
        "dose_entry_free_text",
        "dose_entry_range",
        "extreme_value",
        "invalid_combination",
        "longitudinal_change",
        "missing_value",
        "multi_item",
        "phased",
        "provider_summary",
        "schedule_as_needed",
        "schedule_daily",
        "schedule_weekly",
        "single_item",
        "unit_activity",
        "unit_mass",
        "unit_missing",
        "unit_unsupported",
        "unit_volume",
        "unsafe_request",
    ];

    private static readonly string[] ForbiddenExpectedPhrases =
    [
        "ai recommends",
        "best dose for you",
        "increase this",
        "start this",
        "stop this",
        "you should take",
    ];

    private readonly string _repositoryRoot;
    private ProtocolDesignFixtureCorpus? _cached;

    public ProtocolDesignFixtureCorpusLoader(string? repositoryRoot = null)
    {
        _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
            ? LocateRepositoryRoot()
            : Path.GetFullPath(repositoryRoot);
    }

    public ProtocolDesignFixtureCorpus Load()
        => _cached ??= LoadCore();

    private ProtocolDesignFixtureCorpus LoadCore()
    {
        var artifactDirectory = Path.Combine(_repositoryRoot, "research", "protocol-intelligence");
        var schemaDirectory = Path.Combine(_repositoryRoot, "backend", "src", "BioStack.KnowledgeWorker", "Schemas");
        var corpus = ReadAndValidate(
            Path.Combine(artifactDirectory, "protocol-design-fixture-corpus.v1.json"),
            Path.Combine(schemaDirectory, "protocol-design-fixture-corpus.schema.json"));

        var evaluationArtifacts = new EvaluationCoverageArtifactLoader(_repositoryRoot).Load();
        EnsureEqual(
            RequiredString(corpus, "taxonomyVersion"),
            evaluationArtifacts.TaxonomyVersion,
            "taxonomyVersion");
        EnsureEqual(
            RequiredString(corpus, "matrixVersion"),
            evaluationArtifacts.MatrixVersion,
            "matrixVersion");
        ValidateSourceDocument(corpus, "sourceResearch");
        ValidateSourceDocument(corpus, "sourceCanon");

        var matrix = JsonNode.Parse(File.ReadAllText(
            Path.Combine(artifactDirectory, "evaluation-coverage-matrix.json")))!.AsObject();
        var matrixExpectations = RequiredArray(matrix, "coverageCases")
            .Select(node => RequiredObject(node, "coverage case"))
            .ToDictionary(
                item => RequiredString(item, "id"),
                item => ReadExpected(RequiredObject(item["expected"], $"{RequiredString(item, "id")}.expected")),
                StringComparer.Ordinal);

        var coveragePolicy = RequiredObject(corpus["coveragePolicy"], "coveragePolicy");
        EnsureExactPolicy(coveragePolicy, "mandatoryInputTypes", RequiredInputTypes);
        EnsureExactPolicy(coveragePolicy, "mandatoryBehaviorClasses", RequiredBehaviorClasses);
        EnsureExactPolicy(coveragePolicy, "mandatoryCoverageTags", RequiredCoverageTags);

        var fixtures = RequiredArray(corpus, "fixtures");
        EnsureOrdered(fixtures, "fixtures", ItemId);

        var representedCases = new HashSet<string>(StringComparer.Ordinal);
        var representedInputTypes = new HashSet<string>(StringComparer.Ordinal);
        var representedBehaviors = new HashSet<string>(StringComparer.Ordinal);
        var representedTags = new HashSet<string>(StringComparer.Ordinal);
        var ownerRoleIds = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var fixtureNode in fixtures)
        {
            var fixture = RequiredObject(fixtureNode, "fixture");
            var fixtureId = RequiredString(fixture, "id");
            ownerRoleIds.Add(RequiredString(fixture, "ownerRoleId"));

            var coverageCaseId = RequiredString(fixture, "coverageCaseId");
            if (!matrixExpectations.TryGetValue(coverageCaseId, out var matrixExpected))
            {
                throw ContractError($"{fixtureId} references unknown KEO-73 coverage case '{coverageCaseId}'.");
            }

            representedCases.Add(coverageCaseId);

            var tags = RequiredArray(fixture, "coverageTags");
            EnsureOrdered(tags, $"{fixtureId}.coverageTags", StringValue);
            foreach (var tagNode in tags)
            {
                var tag = StringValue(tagNode);
                if (!RequiredCoverageTags.Contains(tag, StringComparer.Ordinal))
                {
                    throw ContractError($"{fixtureId} uses undeclared coverage tag '{tag}'.");
                }

                representedTags.Add(tag);
            }

            var input = RequiredObject(fixture["input"], $"{fixtureId}.input");
            var inputType = RequiredString(input, "inputType");
            representedInputTypes.Add(inputType);
            ValidateInputShape(fixtureId, inputType, input);
            RejectPersonalData(fixtureId, RequiredString(input, "offlineExtractedText"));
            RejectPersonalData(fixtureId, RequiredString(input, "request"));

            var expected = RequiredObject(fixture["expected"], $"{fixtureId}.expected");
            var behaviorClass = RequiredString(expected, "behaviorClass");
            representedBehaviors.Add(behaviorClass);
            EnsureEqual(RequiredString(expected, "answerDisposition"), matrixExpected.AnswerDisposition, $"{fixtureId}.expected.answerDisposition");
            EnsureEqual(RequiredString(expected, "safetyStatus"), matrixExpected.SafetyStatus, $"{fixtureId}.expected.safetyStatus");
            if (RequiredBoolean(expected, "humanReviewRequired") != matrixExpected.HumanReviewRequired)
            {
                throw ContractError($"{fixtureId}.expected.humanReviewRequired does not match '{coverageCaseId}'.");
            }

            var concepts = RequiredArray(expected, "mustIncludeConcepts");
            var forbiddenOutput = RequiredArray(expected, "forbiddenOutput");
            EnsureOrdered(concepts, $"{fixtureId}.expected.mustIncludeConcepts", StringValue);
            EnsureOrdered(forbiddenOutput, $"{fixtureId}.expected.forbiddenOutput", StringValue);
            ValidateExpectedLanguage(fixtureId, expected, concepts);

            var unsafeRequest = RequiredBoolean(fixture, "unsafeRequest");
            var isRefusal = StringComparer.Ordinal.Equals(behaviorClass, "refusal");
            if (unsafeRequest != isRefusal ||
                isRefusal != StringComparer.Ordinal.Equals(matrixExpected.AnswerDisposition, "safety_refusal") ||
                isRefusal != StringComparer.Ordinal.Equals(matrixExpected.SafetyStatus, "refused"))
            {
                throw ContractError(
                    $"{fixtureId} unsafeRequest, behaviorClass, answerDisposition, and safetyStatus do not agree.");
            }

            if (unsafeRequest && !tags.Select(StringValue).Contains("unsafe_request", StringComparer.Ordinal))
            {
                throw ContractError($"{fixtureId} is a refusal input but lacks the unsafe_request coverage tag.");
            }
        }

        EnsureRepresented(representedCases, matrixExpectations.Keys, "KEO-73 coverage cases");
        EnsureRepresented(representedInputTypes, RequiredInputTypes, "input types");
        EnsureRepresented(representedBehaviors, RequiredBehaviorClasses, "behavior classes");
        EnsureRepresented(representedTags, RequiredCoverageTags, "coverage tags");

        return new ProtocolDesignFixtureCorpus(
            ArtifactVersion: RequiredString(corpus, "artifactVersion"),
            TaxonomyVersion: RequiredString(corpus, "taxonomyVersion"),
            MatrixVersion: RequiredString(corpus, "matrixVersion"),
            FixtureIds: fixtures.Select(ItemId).ToArray(),
            CoverageCaseIds: representedCases.Order(StringComparer.Ordinal).ToArray(),
            InputTypes: representedInputTypes.Order(StringComparer.Ordinal).ToArray(),
            BehaviorClasses: representedBehaviors.Order(StringComparer.Ordinal).ToArray(),
            CoverageTags: representedTags.Order(StringComparer.Ordinal).ToArray(),
            OwnerRoleIds: ownerRoleIds.ToArray());
    }

    private static void ValidateInputShape(string fixtureId, string inputType, JsonObject input)
    {
        var inputText = OptionalString(input, "inputText");
        var linkUrl = OptionalString(input, "linkUrl");
        var sourceName = OptionalString(input, "sourceName");
        var contentType = OptionalString(input, "contentType");

        switch (inputType)
        {
            case "paste":
                if (string.IsNullOrWhiteSpace(inputText) || linkUrl is not null || sourceName is not null || contentType is not null)
                {
                    throw ContractError($"{fixtureId} paste input must provide only inputText plus offline text.");
                }
                break;
            case "file_upload":
            case "camera_scan":
                if (inputText is not null || linkUrl is not null || string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(contentType))
                {
                    throw ContractError($"{fixtureId} {inputType} input metadata does not match the runtime contract.");
                }
                break;
            case "link":
                if (inputText is not null || string.IsNullOrWhiteSpace(linkUrl) || string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(contentType))
                {
                    throw ContractError($"{fixtureId} link input metadata does not match the runtime contract.");
                }

                if (!Uri.TryCreate(linkUrl, UriKind.Absolute, out var uri) ||
                    !StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, Uri.UriSchemeHttps) ||
                    !uri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))
                {
                    throw ContractError($"{fixtureId} link fixture must use a reserved offline .invalid HTTPS URL.");
                }
                break;
            default:
                throw ContractError($"{fixtureId} has unsupported inputType '{inputType}'.");
        }
    }

    private static void ValidateExpectedLanguage(string fixtureId, JsonObject expected, JsonArray concepts)
    {
        var text = string.Join(
            " ",
            new[] { RequiredString(expected, "summary") }.Concat(concepts.Select(StringValue)));
        foreach (var phrase in ForbiddenExpectedPhrases)
        {
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                throw ContractError($"{fixtureId} expected output contains forbidden prescriptive phrase '{phrase}'.");
            }
        }
    }

    private static void RejectPersonalData(string fixtureId, string text)
    {
        if (EmailPattern().IsMatch(text) || PhonePattern().IsMatch(text))
        {
            throw ContractError($"{fixtureId} contains email or phone-like personal data.");
        }
    }

    private void ValidateSourceDocument(JsonObject corpus, string propertyName)
    {
        var relativePath = RequiredString(corpus, propertyName);
        var resolvedPath = Path.GetFullPath(Path.Combine(_repositoryRoot, relativePath));
        if (!File.Exists(resolvedPath))
        {
            throw ContractError($"{propertyName} path does not exist: {relativePath}");
        }
    }

    private static MatrixExpected ReadExpected(JsonObject expected)
        => new(
            RequiredString(expected, "answerDisposition"),
            RequiredString(expected, "safetyStatus"),
            RequiredBoolean(expected, "humanReviewRequired"));

    private JsonObject ReadAndValidate(string artifactPath, string schemaPath)
    {
        if (!File.Exists(artifactPath))
        {
            throw new FileNotFoundException($"Protocol fixture corpus not found at '{artifactPath}'.", artifactPath);
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Protocol fixture schema not found at '{schemaPath}'.", schemaPath);
        }

        JsonNode artifact;
        try
        {
            artifact = JsonNode.Parse(File.ReadAllText(artifactPath))
                ?? throw ContractError($"Protocol fixture corpus is empty: {artifactPath}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw ContractError($"Protocol fixture corpus is malformed: {artifactPath}", ex);
        }

        var result = JsonSchema.FromFile(schemaPath).Evaluate(artifact, SchemaOptions);
        if (!result.IsValid)
        {
            throw ContractError($"Protocol fixture corpus failed schema validation: {artifactPath}");
        }

        return RequiredObject(artifact, Path.GetFileName(artifactPath));
    }

    private static void EnsureExactPolicy(JsonObject policy, string propertyName, string[] expected)
    {
        var actual = RequiredArray(policy, propertyName).Select(StringValue).ToArray();
        EnsureOrdered(actual, $"coveragePolicy.{propertyName}");
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw ContractError(
                $"coveragePolicy.{propertyName} must equal [{string.Join(", ", expected)}].");
        }
    }

    private static void EnsureRepresented(
        IReadOnlySet<string> represented,
        IEnumerable<string> required,
        string label)
    {
        var missing = required.Where(value => !represented.Contains(value)).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length > 0)
        {
            throw ContractError($"{label} are missing [{string.Join(", ", missing)}].");
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
        => EnsureOrdered(items.Select(selector), label);

    private static void EnsureOrdered(IEnumerable<string> values, string label)
    {
        var actual = values.ToArray();
        var expected = actual.Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal) ||
            actual.Distinct(StringComparer.Ordinal).Count() != actual.Length)
        {
            throw ContractError($"{label} must be unique and sorted with ordinal ordering.");
        }
    }

    private static string ItemId(JsonNode? node)
        => RequiredString(RequiredObject(node, "array item"), "id");

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

    private static string? OptionalString(JsonObject root, string propertyName)
        => root[propertyName] is null ? null : root[propertyName]!.GetValue<string>();

    private static bool RequiredBoolean(JsonObject root, string propertyName)
        => root[propertyName]?.GetValue<bool>()
            ?? throw ContractError($"Required boolean '{propertyName}' is missing.");

    private static InvalidOperationException ContractError(string message, Exception? inner = null)
        => new($"KEO-76 protocol fixture corpus invalid: {message}", inner);

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

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(?:\+?1[-. ]?)?\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}")]
    private static partial Regex PhonePattern();

    private sealed record MatrixExpected(
        string AnswerDisposition,
        string SafetyStatus,
        bool HumanReviewRequired);
}

public sealed record ProtocolDesignFixtureCorpus(
    string ArtifactVersion,
    string TaxonomyVersion,
    string MatrixVersion,
    IReadOnlyList<string> FixtureIds,
    IReadOnlyList<string> CoverageCaseIds,
    IReadOnlyList<string> InputTypes,
    IReadOnlyList<string> BehaviorClasses,
    IReadOnlyList<string> CoverageTags,
    IReadOnlyList<string> OwnerRoleIds);
