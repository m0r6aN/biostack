namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;
using BioStack.Contracts.Responses;
using Json.Schema;

/// <summary>
/// Loads and validates the versioned KEO-73 evaluation taxonomy and coverage matrix.
/// These files are offline evaluation contracts only: they are not registered with DI,
/// exposed by an endpoint, or consumed by runtime intelligence behavior.
/// </summary>
public sealed class EvaluationCoverageArtifactLoader
{
    private static readonly EvaluationOptions SchemaOptions = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private static readonly string[] RuntimeSafetyStatuses =
    [
        SafetyStatus.Allowed,
        SafetyStatus.Constrained,
        SafetyStatus.Refused,
        SafetyStatus.Warning,
    ];

    private static readonly string[] AnswerDispositions =
    [
        "safety_refusal",
        "supported",
        "unknown",
        "unsupported",
    ];

    private readonly string _repositoryRoot;
    private EvaluationCoverageArtifactSet? _cached;

    public EvaluationCoverageArtifactLoader(string? repositoryRoot = null)
    {
        _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
            ? LocateRepositoryRoot()
            : Path.GetFullPath(repositoryRoot);
    }

    public EvaluationCoverageArtifactSet Load()
        => _cached ??= LoadCore();

    private EvaluationCoverageArtifactSet LoadCore()
    {
        var artifactDirectory = Path.Combine(_repositoryRoot, "research", "protocol-intelligence");
        var schemaDirectory = Path.Combine(_repositoryRoot, "backend", "src", "BioStack.KnowledgeWorker", "Schemas");

        var taxonomy = ReadAndValidate(
            Path.Combine(artifactDirectory, "evaluation-taxonomy.json"),
            Path.Combine(schemaDirectory, "evaluation-taxonomy.schema.json"));
        var matrix = ReadAndValidate(
            Path.Combine(artifactDirectory, "evaluation-coverage-matrix.json"),
            Path.Combine(schemaDirectory, "evaluation-coverage-matrix.schema.json"));

        ValidateSourceResearch(taxonomy, "evaluation-taxonomy.json");
        ValidateSourceResearch(matrix, "evaluation-coverage-matrix.json");

        var dimensions = RequiredArray(taxonomy, "dimensions");
        EnsureOrdered(dimensions, "dimensions", ItemId);

        var valuesByDimension = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var ownerRoleIds = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var dimensionNode in dimensions)
        {
            var dimension = RequiredObject(dimensionNode, "dimension");
            var dimensionId = RequiredString(dimension, "id");
            ownerRoleIds.Add(RequiredString(dimension, "ownerRoleId"));

            var values = RequiredArray(dimension, "values");
            EnsureOrdered(values, $"dimensions[{dimensionId}].values", ItemId);
            valuesByDimension[dimensionId] = values
                .Select(ItemId)
                .ToHashSet(StringComparer.Ordinal);
        }

        var dimensionIds = valuesByDimension.Keys.Order(StringComparer.Ordinal).ToArray();
        var coveragePolicy = RequiredObject(taxonomy["coveragePolicy"], "coveragePolicy");
        EnsureExactOrderedIds(
            RequiredArray(coveragePolicy, "mandatoryDimensionIds"),
            dimensionIds,
            "coveragePolicy.mandatoryDimensionIds");

        var declaredDispositions = ReadLabeledIds(taxonomy, "answerDispositions");
        EnsureExact(declaredDispositions, AnswerDispositions, "answerDispositions");
        EnsureExactOrderedIds(
            RequiredArray(coveragePolicy, "mandatoryAnswerDispositionIds"),
            AnswerDispositions,
            "coveragePolicy.mandatoryAnswerDispositionIds");

        var declaredSafetyStatuses = ReadLabeledIds(taxonomy, "safetyStatuses");
        EnsureExact(declaredSafetyStatuses, RuntimeSafetyStatuses, "safetyStatuses");
        EnsureExactOrderedIds(
            RequiredArray(coveragePolicy, "mandatorySafetyStatusIds"),
            RuntimeSafetyStatuses,
            "coveragePolicy.mandatorySafetyStatusIds");

        var taxonomyVersion = RequiredString(taxonomy, "artifactVersion");
        if (!StringComparer.Ordinal.Equals(taxonomyVersion, RequiredString(matrix, "taxonomyVersion")))
        {
            throw ContractError("Matrix taxonomyVersion does not match taxonomy artifactVersion.");
        }

        var cases = RequiredArray(matrix, "coverageCases");
        EnsureOrdered(cases, "coverageCases", ItemId);

        var caseSelections = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        var representedValues = dimensionIds.ToDictionary(
            id => id,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        var representedDispositions = new HashSet<string>(StringComparer.Ordinal);
        var representedStatuses = new HashSet<string>(StringComparer.Ordinal);

        foreach (var caseNode in cases)
        {
            var coverageCase = RequiredObject(caseNode, "coverage case");
            var caseId = RequiredString(coverageCase, "id");
            ownerRoleIds.Add(RequiredString(coverageCase, "ownerRoleId"));

            var selectionsNode = RequiredObject(coverageCase["selections"], $"{caseId}.selections");
            var selectionIds = selectionsNode.Select(pair => pair.Key).ToArray();
            EnsureExact(selectionIds, dimensionIds, $"{caseId}.selections dimension IDs");
            EnsureOrdered(selectionIds, $"{caseId}.selections");

            var selections = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var dimensionId in dimensionIds)
            {
                var valueId = RequiredString(selectionsNode, dimensionId);
                if (!valuesByDimension[dimensionId].Contains(valueId))
                {
                    throw ContractError($"{caseId} selects unknown value '{dimensionId}:{valueId}'.");
                }

                selections[dimensionId] = valueId;
                representedValues[dimensionId].Add(valueId);
            }

            caseSelections[caseId] = selections;

            var expected = RequiredObject(coverageCase["expected"], $"{caseId}.expected");
            var disposition = RequiredString(expected, "answerDisposition");
            var safetyStatus = RequiredString(expected, "safetyStatus");
            if (!AnswerDispositions.Contains(disposition, StringComparer.Ordinal))
            {
                throw ContractError($"{caseId} has unknown answer disposition '{disposition}'.");
            }

            if (!RuntimeSafetyStatuses.Contains(safetyStatus, StringComparer.Ordinal))
            {
                throw ContractError($"{caseId} has unknown runtime SafetyStatus '{safetyStatus}'.");
            }

            representedDispositions.Add(disposition);
            representedStatuses.Add(safetyStatus);

            var refusalClass = selections["refusal_class"];
            var isRefusal = !StringComparer.Ordinal.Equals(refusalClass, "none");
            if (isRefusal != StringComparer.Ordinal.Equals(disposition, "safety_refusal") ||
                isRefusal != StringComparer.Ordinal.Equals(safetyStatus, SafetyStatus.Refused))
            {
                throw ContractError(
                    $"{caseId} refusal_class, answerDisposition, and SafetyStatus do not agree.");
            }
        }

        foreach (var dimensionId in dimensionIds)
        {
            EnsureExact(
                representedValues[dimensionId].Order(StringComparer.Ordinal).ToArray(),
                valuesByDimension[dimensionId].Order(StringComparer.Ordinal).ToArray(),
                $"represented values for {dimensionId}");
        }

        EnsureExact(
            representedDispositions.Order(StringComparer.Ordinal).ToArray(),
            AnswerDispositions,
            "represented answer dispositions");
        EnsureExact(
            representedStatuses.Order(StringComparer.Ordinal).ToArray(),
            RuntimeSafetyStatuses,
            "represented runtime SafetyStatus values");

        var pairs = RequiredArray(matrix, "requiredValuePairs");
        EnsureOrdered(pairs, "requiredValuePairs", ItemId);
        foreach (var pairNode in pairs)
        {
            var pair = RequiredObject(pairNode, "required value pair");
            var pairId = RequiredString(pair, "id");
            var caseId = RequiredString(pair, "coveredByCaseId");
            if (!caseSelections.TryGetValue(caseId, out var selections))
            {
                throw ContractError($"{pairId} names unknown coverage case '{caseId}'.");
            }

            ValidatePairSide(pairId, "left", RequiredObject(pair["left"], $"{pairId}.left"), selections, valuesByDimension);
            ValidatePairSide(pairId, "right", RequiredObject(pair["right"], $"{pairId}.right"), selections, valuesByDimension);
        }

        return new EvaluationCoverageArtifactSet(
            TaxonomyVersion: taxonomyVersion,
            MatrixVersion: RequiredString(matrix, "artifactVersion"),
            DimensionIds: dimensionIds,
            CoverageCaseIds: caseSelections.Keys.Order(StringComparer.Ordinal).ToArray(),
            RequiredValuePairIds: pairs.Select(ItemId).ToArray(),
            OwnerRoleIds: ownerRoleIds.ToArray());
    }

    private static void ValidatePairSide(
        string pairId,
        string sideName,
        JsonObject side,
        IReadOnlyDictionary<string, string> caseSelections,
        IReadOnlyDictionary<string, HashSet<string>> valuesByDimension)
    {
        var dimensionId = RequiredString(side, "dimensionId");
        var valueId = RequiredString(side, "valueId");
        if (!valuesByDimension.TryGetValue(dimensionId, out var values) || !values.Contains(valueId))
        {
            throw ContractError($"{pairId}.{sideName} references unknown value '{dimensionId}:{valueId}'.");
        }

        if (!caseSelections.TryGetValue(dimensionId, out var selectedValue) ||
            !StringComparer.Ordinal.Equals(selectedValue, valueId))
        {
            throw ContractError(
                $"{pairId}.{sideName} is not covered by its declared coverage case: '{dimensionId}:{valueId}'.");
        }
    }

    private JsonObject ReadAndValidate(string artifactPath, string schemaPath)
    {
        if (!File.Exists(artifactPath))
        {
            throw new FileNotFoundException($"Evaluation artifact not found at '{artifactPath}'.", artifactPath);
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Evaluation schema not found at '{schemaPath}'.", schemaPath);
        }

        JsonNode artifact;
        try
        {
            artifact = JsonNode.Parse(File.ReadAllText(artifactPath))
                ?? throw ContractError($"Evaluation artifact is empty: {artifactPath}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw ContractError($"Evaluation artifact is malformed: {artifactPath}", ex);
        }

        var result = JsonSchema.FromFile(schemaPath).Evaluate(artifact, SchemaOptions);
        if (!result.IsValid)
        {
            throw ContractError($"Evaluation artifact failed schema validation: {artifactPath}");
        }

        return RequiredObject(artifact, Path.GetFileName(artifactPath));
    }

    private void ValidateSourceResearch(JsonObject artifact, string fileName)
    {
        var relativePath = RequiredString(artifact, "sourceResearch");
        var resolvedPath = Path.GetFullPath(Path.Combine(_repositoryRoot, relativePath));
        if (!File.Exists(resolvedPath))
        {
            throw ContractError($"{fileName} sourceResearch path does not exist: {relativePath}");
        }
    }

    private static string[] ReadLabeledIds(JsonObject root, string propertyName)
    {
        var items = RequiredArray(root, propertyName);
        EnsureOrdered(items, propertyName, ItemId);
        return items.Select(ItemId).ToArray();
    }

    private static void EnsureExactOrderedIds(JsonArray actualNodes, string[] expected, string label)
    {
        var actual = actualNodes.Select(node => RequiredString(node, label)).ToArray();
        EnsureOrdered(actual, label);
        EnsureExact(actual, expected, label);
    }

    private static void EnsureExact(IEnumerable<string> actual, IEnumerable<string> expected, string label)
    {
        var actualArray = actual.ToArray();
        var expectedArray = expected.ToArray();
        if (!actualArray.SequenceEqual(expectedArray, StringComparer.Ordinal))
        {
            throw ContractError(
                $"{label} must equal [{string.Join(", ", expectedArray)}]; actual [{string.Join(", ", actualArray)}].");
        }
    }

    private static void EnsureOrdered(JsonArray items, string label, Func<JsonNode?, string> selector)
        => EnsureOrdered(items.Select(selector), label);

    private static void EnsureOrdered(IEnumerable<string> values, string label)
    {
        var actual = values.ToArray();
        var expected = actual.Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal) || actual.Distinct(StringComparer.Ordinal).Count() != actual.Length)
        {
            throw ContractError($"{label} must be unique and sorted with ordinal ordering.");
        }
    }

    private static string ItemId(JsonNode? node)
        => RequiredString(RequiredObject(node, "array item"), "id");

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

    private static string RequiredString(JsonNode? node, string label)
        => node?.GetValue<string>() is { Length: > 0 } value
            ? value
            : throw ContractError($"Required string '{label}' is missing.");

    private static InvalidOperationException ContractError(string message, Exception? inner = null)
        => new($"KEO-73 evaluation coverage contract invalid: {message}", inner);

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

public sealed record EvaluationCoverageArtifactSet(
    string TaxonomyVersion,
    string MatrixVersion,
    IReadOnlyList<string> DimensionIds,
    IReadOnlyList<string> CoverageCaseIds,
    IReadOnlyList<string> RequiredValuePairIds,
    IReadOnlyList<string> OwnerRoleIds);
