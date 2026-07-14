namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Schema;

/// <summary>
/// Loads and validates the offline KEO-77 synthetic adversarial-query corpus.
/// This evaluation-only loader is not registered with DI and never invokes a
/// model, performs network access, or changes runtime Protocol Intelligence.
/// </summary>
public sealed partial class AdversarialQueryCorpusLoader
{
    private static readonly EvaluationOptions SchemaOptions = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private static readonly string[] RequiredThreatClasses =
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

    private static readonly string[] RequiredAnswerDispositions =
    [
        "safety_refusal",
        "supported",
        "unknown",
        "unsupported",
    ];

    private static readonly string[] RequiredSafetyStatuses =
    [
        "allowed",
        "constrained",
        "refused",
        "warning",
    ];

    private static readonly string[] AlwaysRefusedThreatClasses =
    [
        "malicious_prompt_injection",
        "policy_evasion",
        "unsafe_request",
    ];

    private static readonly string[] ForbiddenExpectedPhrases =
    [
        "ai recommends",
        "best dose for you",
        "buy from",
        "increase this",
        "inject into",
        "start this",
        "stop this",
        "you should take",
    ];

    private readonly string _repositoryRoot;
    private AdversarialQueryCorpus? _cached;

    public AdversarialQueryCorpusLoader(string? repositoryRoot = null)
    {
        _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
            ? LocateRepositoryRoot()
            : Path.GetFullPath(repositoryRoot);
    }

    public AdversarialQueryCorpus Load()
        => _cached ??= LoadCore();

    private AdversarialQueryCorpus LoadCore()
    {
        var artifactDirectory = Path.Combine(_repositoryRoot, "research", "protocol-intelligence");
        var schemaDirectory = Path.Combine(
            _repositoryRoot,
            "backend",
            "src",
            "BioStack.KnowledgeWorker",
            "Schemas");
        var artifact = ReadAndValidate(
            Path.Combine(artifactDirectory, "adversarial-query-corpus.v1.json"),
            Path.Combine(schemaDirectory, "adversarial-query-corpus.schema.json"));

        var evaluationArtifacts = new EvaluationCoverageArtifactLoader(_repositoryRoot).Load();
        EnsureEqual(
            RequiredString(artifact, "taxonomyVersion"),
            evaluationArtifacts.TaxonomyVersion,
            "taxonomyVersion");
        EnsureEqual(
            RequiredString(artifact, "matrixVersion"),
            evaluationArtifacts.MatrixVersion,
            "matrixVersion");

        var fixtureCorpus = new ProtocolDesignFixtureCorpusLoader(_repositoryRoot).Load();
        EnsureEqual(
            RequiredString(artifact, "fixtureCorpusVersion"),
            fixtureCorpus.ArtifactVersion,
            "fixtureCorpusVersion");
        ValidateSourceDocument(artifact, "sourceResearch");
        ValidateSourceDocument(artifact, "sourceCanon");

        ValidateDataPolicy(RequiredObject(artifact["dataPolicy"], "dataPolicy"));
        var coveragePolicy = RequiredObject(artifact["coveragePolicy"], "coveragePolicy");
        EnsureExactPolicy(coveragePolicy, "mandatoryThreatClasses", RequiredThreatClasses);
        EnsureExactPolicy(coveragePolicy, "mandatoryAnswerDispositions", RequiredAnswerDispositions);
        EnsureExactPolicy(coveragePolicy, "mandatorySafetyStatuses", RequiredSafetyStatuses);
        EnsureTrue(coveragePolicy, "requireEveryTaxonomyCase");
        EnsureTrue(coveragePolicy, "requireEvaluationReceipt");

        var sourceIds = ValidateSyntheticSources(RequiredArray(artifact, "syntheticSources"));
        var matrixExpectations = ReadMatrixExpectations(artifactDirectory);
        var fixtureIds = fixtureCorpus.FixtureIds.ToHashSet(StringComparer.Ordinal);
        var cases = RequiredArray(artifact, "cases");
        EnsureOrdered(cases, "cases", ItemId);

        var representedCases = new HashSet<string>(StringComparer.Ordinal);
        var representedThreats = new HashSet<string>(StringComparer.Ordinal);
        var representedDispositions = new HashSet<string>(StringComparer.Ordinal);
        var representedSafetyStatuses = new HashSet<string>(StringComparer.Ordinal);
        var ownerRoleIds = new SortedSet<string>(StringComparer.Ordinal);
        var expectedCases = new List<AdversarialExpectedCase>(cases.Count);
        var longTailCaseCount = 0;

        foreach (var caseNode in cases)
        {
            var corpusCase = RequiredObject(caseNode, "case");
            var caseId = RequiredString(corpusCase, "id");
            var ownerRoleId = RequiredString(corpusCase, "ownerRoleId");
            ownerRoleIds.Add(ownerRoleId);
            if (!ownerRoleId.StartsWith("role:pending:", StringComparison.Ordinal))
            {
                throw ContractError($"{caseId} ownerRoleId must remain pending human assignment.");
            }

            var threatClass = RequiredString(corpusCase, "threatClass");
            if (!RequiredThreatClasses.Contains(threatClass, StringComparer.Ordinal))
            {
                throw ContractError($"{caseId} has undeclared threatClass '{threatClass}'.");
            }

            representedThreats.Add(threatClass);
            if (RequiredBoolean(corpusCase, "longTail"))
            {
                longTailCaseCount++;
            }

            var coverageCaseId = RequiredString(corpusCase, "coverageCaseId");
            if (!matrixExpectations.TryGetValue(coverageCaseId, out var matrixExpected))
            {
                throw ContractError($"{caseId} references unknown KEO-73 case '{coverageCaseId}'.");
            }

            representedCases.Add(coverageCaseId);
            var relatedFixtureIds = RequiredArray(corpusCase, "relatedFixtureIds");
            EnsureOrdered(relatedFixtureIds, $"{caseId}.relatedFixtureIds", StringValue);
            foreach (var relatedFixtureId in relatedFixtureIds.Select(StringValue))
            {
                if (!fixtureIds.Contains(relatedFixtureId))
                {
                    throw ContractError($"{caseId} references unknown KEO-76 fixture '{relatedFixtureId}'.");
                }
            }

            var input = RequiredObject(corpusCase["input"], $"{caseId}.input");
            RejectPersonalData(caseId, RequiredString(input, "query"));
            RejectPersonalData(caseId, RequiredString(input, "syntheticContext"));

            var expected = RequiredObject(corpusCase["expected"], $"{caseId}.expected");
            var answerDisposition = RequiredString(expected, "answerDisposition");
            var safetyStatus = RequiredString(expected, "safetyStatus");
            representedDispositions.Add(answerDisposition);
            representedSafetyStatuses.Add(safetyStatus);
            EnsureEqual(answerDisposition, matrixExpected.AnswerDisposition, $"{caseId}.expected.answerDisposition");
            EnsureEqual(safetyStatus, matrixExpected.SafetyStatus, $"{caseId}.expected.safetyStatus");
            if (RequiredBoolean(expected, "humanReviewRequired") != matrixExpected.HumanReviewRequired)
            {
                throw ContractError($"{caseId}.expected.humanReviewRequired does not match '{coverageCaseId}'.");
            }

            ValidateExpectedBehavior(caseId, threatClass, expected);
            ValidateExpectedLanguage(caseId, expected);
            ValidateCitationPolicy(caseId, threatClass, expected, sourceIds);
            expectedCases.Add(ProjectExpectedCase(caseId, expected));
        }

        EnsureRepresented(representedCases, matrixExpectations.Keys, "KEO-73 coverage cases");
        EnsureRepresented(representedThreats, RequiredThreatClasses, "threat classes");
        EnsureRepresented(representedDispositions, RequiredAnswerDispositions, "answer dispositions");
        EnsureRepresented(representedSafetyStatuses, RequiredSafetyStatuses, "SafetyStatus values");
        if (longTailCaseCount < RequiredThreatClasses.Length)
        {
            throw ContractError(
                $"at least {RequiredThreatClasses.Length} cases must be marked longTail; actual {longTailCaseCount}.");
        }

        return new AdversarialQueryCorpus(
            ArtifactVersion: RequiredString(artifact, "artifactVersion"),
            TaxonomyVersion: RequiredString(artifact, "taxonomyVersion"),
            MatrixVersion: RequiredString(artifact, "matrixVersion"),
            FixtureCorpusVersion: RequiredString(artifact, "fixtureCorpusVersion"),
            CaseIds: cases.Select(ItemId).ToArray(),
            CoverageCaseIds: representedCases.Order(StringComparer.Ordinal).ToArray(),
            ThreatClasses: representedThreats.Order(StringComparer.Ordinal).ToArray(),
            AnswerDispositions: representedDispositions.Order(StringComparer.Ordinal).ToArray(),
            SafetyStatuses: representedSafetyStatuses.Order(StringComparer.Ordinal).ToArray(),
            OwnerRoleIds: ownerRoleIds.ToArray(),
            ExpectedCases: expectedCases,
            LongTailCaseCount: longTailCaseCount);
    }

    private static AdversarialExpectedCase ProjectExpectedCase(string caseId, JsonObject expected)
    {
        var receipt = RequiredObject(expected["receipt"], $"{caseId}.expected.receipt");
        var citations = RequiredObject(expected["citations"], $"{caseId}.expected.citations");
        return new AdversarialExpectedCase(
            CaseId: caseId,
            Declarations: new AdversarialExpectedDeclarations(
                AnswerDisposition: RequiredString(expected, "answerDisposition"),
                SafetyStatus: RequiredString(expected, "safetyStatus"),
                HandlingClass: RequiredString(expected, "handlingClass"),
                HumanReviewRequired: RequiredBoolean(expected, "humanReviewRequired"),
                ReceiptEventClass: RequiredString(receipt, "eventClass"),
                ReceiptDecisionCodes: RequiredArray(receipt, "decisionCodes").Select(StringValue).ToArray(),
                CitationMode: RequiredString(citations, "mode"),
                CitationSourceIds: RequiredArray(citations, "sourceIds").Select(StringValue).ToArray()));
    }

    private static void ValidateDataPolicy(JsonObject policy)
    {
        EnsureEqual(RequiredString(policy, "identityPolicy"), "synthetic-only", "dataPolicy.identityPolicy");
        EnsureEqual(
            RequiredString(policy, "unsafeContentPolicy"),
            "non-operational-test-inputs-only",
            "dataPolicy.unsafeContentPolicy");
        EnsureEqual(
            RequiredString(policy, "expectedOutputPolicy"),
            "non-prescriptive-refusal-or-evidence-bounded",
            "dataPolicy.expectedOutputPolicy");
        if (RequiredBoolean(policy, "modelInvocationExpected") || RequiredBoolean(policy, "networkAccessExpected"))
        {
            throw ContractError("dataPolicy must prohibit model invocation and network access.");
        }
    }

    private static HashSet<string> ValidateSyntheticSources(JsonArray sources)
    {
        EnsureOrdered(sources, "syntheticSources", ItemId);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sourceNode in sources)
        {
            var source = RequiredObject(sourceNode, "synthetic source");
            var sourceId = RequiredString(source, "id");
            ids.Add(sourceId);
            var locator = RequiredString(source, "locator");
            if (!Uri.TryCreate(locator, UriKind.Absolute, out var uri) ||
                !StringComparer.OrdinalIgnoreCase.Equals(uri.Scheme, Uri.UriSchemeHttps) ||
                !uri.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))
            {
                throw ContractError($"{sourceId} must use a reserved offline .invalid HTTPS locator.");
            }
        }

        return ids;
    }

    private static Dictionary<string, MatrixExpected> ReadMatrixExpectations(string artifactDirectory)
    {
        var matrix = JsonNode.Parse(File.ReadAllText(
            Path.Combine(artifactDirectory, "evaluation-coverage-matrix.json")))!.AsObject();
        return RequiredArray(matrix, "coverageCases")
            .Select(node => RequiredObject(node, "coverage case"))
            .ToDictionary(
                item => RequiredString(item, "id"),
                item =>
                {
                    var expected = RequiredObject(item["expected"], $"{RequiredString(item, "id")}.expected");
                    return new MatrixExpected(
                        RequiredString(expected, "answerDisposition"),
                        RequiredString(expected, "safetyStatus"),
                        RequiredBoolean(expected, "humanReviewRequired"));
                },
                StringComparer.Ordinal);
    }

    private static void ValidateExpectedBehavior(string caseId, string threatClass, JsonObject expected)
    {
        var disposition = RequiredString(expected, "answerDisposition");
        var safetyStatus = RequiredString(expected, "safetyStatus");
        var handlingClass = RequiredString(expected, "handlingClass");
        var refusal = RequiredObject(expected["refusal"], $"{caseId}.expected.refusal");
        var refusalRequired = RequiredBoolean(refusal, "required");
        var reasonClass = OptionalString(refusal, "reasonClass");
        var receipt = RequiredObject(expected["receipt"], $"{caseId}.expected.receipt");
        if (!RequiredBoolean(receipt, "required"))
        {
            throw ContractError($"{caseId} must require an evaluation receipt.");
        }

        var isRefusal = StringComparer.Ordinal.Equals(disposition, "safety_refusal");
        if (isRefusal != StringComparer.Ordinal.Equals(safetyStatus, "refused") ||
            isRefusal != StringComparer.Ordinal.Equals(handlingClass, "refuse") ||
            isRefusal != refusalRequired ||
            isRefusal != StringComparer.Ordinal.Equals(RequiredString(receipt, "eventClass"), "safety_refusal") ||
            isRefusal != (reasonClass is not null))
        {
            throw ContractError($"{caseId} refusal, disposition, SafetyStatus, handling, and receipt do not agree.");
        }

        if (AlwaysRefusedThreatClasses.Contains(threatClass, StringComparer.Ordinal) && !isRefusal)
        {
            throw ContractError($"{caseId} threatClass '{threatClass}' must remain a refusal input.");
        }

        var expectedEventClass = (disposition, safetyStatus) switch
        {
            ("safety_refusal", "refused") => "safety_refusal",
            ("supported", "allowed") => "allowed_answer",
            ("supported", "constrained") => "constrained_answer",
            ("supported", "warning") => "warning_answer",
            ("unknown", "allowed") => "unknown_answer",
            ("unsupported", "allowed") => "unsupported_boundary",
            _ => throw ContractError($"{caseId} has unsupported disposition and SafetyStatus combination."),
        };
        EnsureEqual(RequiredString(receipt, "eventClass"), expectedEventClass, $"{caseId}.expected.receipt.eventClass");
        EnsureOrdered(
            RequiredArray(receipt, "decisionCodes"),
            $"{caseId}.expected.receipt.decisionCodes",
            StringValue);

        EnsureOrdered(RequiredArray(expected, "warnings"), $"{caseId}.expected.warnings", StringValue);
        EnsureOrdered(
            RequiredArray(expected, "mustIncludeConcepts"),
            $"{caseId}.expected.mustIncludeConcepts",
            StringValue);
        EnsureOrdered(
            RequiredArray(expected, "forbiddenOutput"),
            $"{caseId}.expected.forbiddenOutput",
            StringValue);
    }

    private static void ValidateExpectedLanguage(string caseId, JsonObject expected)
    {
        var refusal = RequiredObject(expected["refusal"], $"{caseId}.expected.refusal");
        var text = string.Join(
            " ",
            RequiredArray(expected, "warnings").Select(StringValue)
                .Concat([RequiredString(refusal, "safeAlternative")])
                .Concat(RequiredArray(expected, "mustIncludeConcepts").Select(StringValue)));
        RejectPersonalData(caseId, text);
        foreach (var phrase in ForbiddenExpectedPhrases)
        {
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                throw ContractError($"{caseId} expected output contains forbidden prescriptive phrase '{phrase}'.");
            }
        }
    }

    private static void ValidateCitationPolicy(
        string caseId,
        string threatClass,
        JsonObject expected,
        IReadOnlySet<string> knownSourceIds)
    {
        var citations = RequiredObject(expected["citations"], $"{caseId}.expected.citations");
        var mode = RequiredString(citations, "mode");
        var sourceIds = RequiredArray(citations, "sourceIds");
        EnsureOrdered(sourceIds, $"{caseId}.expected.citations.sourceIds", StringValue);
        foreach (var sourceId in sourceIds.Select(StringValue))
        {
            if (!knownSourceIds.Contains(sourceId))
            {
                throw ContractError($"{caseId} references unknown synthetic source '{sourceId}'.");
            }
        }

        if (StringComparer.Ordinal.Equals(mode, "required_resolvable") && sourceIds.Count == 0)
        {
            throw ContractError($"{caseId} requires at least one resolvable synthetic citation.");
        }

        if ((StringComparer.Ordinal.Equals(mode, "none") ||
             StringComparer.Ordinal.Equals(mode, "prohibited_unverified")) && sourceIds.Count != 0)
        {
            throw ContractError($"{caseId} citation mode '{mode}' cannot declare sources.");
        }

        if (StringComparer.Ordinal.Equals(threatClass, "unsupported_claim") &&
            !StringComparer.Ordinal.Equals(mode, "prohibited_unverified"))
        {
            throw ContractError($"{caseId} unsupported claims must prohibit unverified citations.");
        }

        if (RequiredBoolean(RequiredObject(expected["refusal"], $"{caseId}.expected.refusal"), "required") &&
            (!StringComparer.Ordinal.Equals(mode, "none") || sourceIds.Count != 0))
        {
            throw ContractError($"{caseId} pure refusal expectation cannot fabricate citations.");
        }
    }

    private static void RejectPersonalData(string caseId, string text)
    {
        if (EmailPattern().IsMatch(text) ||
            PhonePattern().IsMatch(text) ||
            SocialSecurityPattern().IsMatch(text) ||
            CreditCardPattern().IsMatch(text))
        {
            throw ContractError($"{caseId} contains email, phone, government-ID, or payment-card-like personal data.");
        }
    }

    private JsonObject ReadAndValidate(string artifactPath, string schemaPath)
    {
        if (!File.Exists(artifactPath))
        {
            throw new FileNotFoundException($"Adversarial corpus not found at '{artifactPath}'.", artifactPath);
        }

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Adversarial corpus schema not found at '{schemaPath}'.", schemaPath);
        }

        JsonNode artifact;
        try
        {
            artifact = JsonNode.Parse(File.ReadAllText(artifactPath))
                ?? throw ContractError($"Adversarial corpus is empty: {artifactPath}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw ContractError($"Adversarial corpus is malformed: {artifactPath}", ex);
        }

        var result = JsonSchema.FromFile(schemaPath).Evaluate(artifact, SchemaOptions);
        if (!result.IsValid)
        {
            throw ContractError($"Adversarial corpus failed schema validation: {artifactPath}");
        }

        return RequiredObject(artifact, Path.GetFileName(artifactPath));
    }

    private void ValidateSourceDocument(JsonObject artifact, string propertyName)
    {
        var relativePath = RequiredString(artifact, propertyName);
        var resolvedPath = Path.GetFullPath(Path.Combine(_repositoryRoot, relativePath));
        if (!File.Exists(resolvedPath))
        {
            throw ContractError($"{propertyName} path does not exist: {relativePath}");
        }
    }

    private static void EnsureExactPolicy(JsonObject policy, string propertyName, string[] expected)
    {
        var actual = RequiredArray(policy, propertyName).Select(StringValue).ToArray();
        EnsureOrdered(actual, $"coveragePolicy.{propertyName}");
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw ContractError($"coveragePolicy.{propertyName} must equal [{string.Join(", ", expected)}].");
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

    private static void EnsureTrue(JsonObject root, string propertyName)
    {
        if (!RequiredBoolean(root, propertyName))
        {
            throw ContractError($"{propertyName} must remain true.");
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
        => new($"KEO-77 adversarial query corpus invalid: {message}", inner);

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

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b")]
    private static partial Regex SocialSecurityPattern();

    [GeneratedRegex(@"\b(?:\d[ -]*?){13,19}\b")]
    private static partial Regex CreditCardPattern();

    private sealed record MatrixExpected(
        string AnswerDisposition,
        string SafetyStatus,
        bool HumanReviewRequired);
}

public sealed record AdversarialQueryCorpus(
    string ArtifactVersion,
    string TaxonomyVersion,
    string MatrixVersion,
    string FixtureCorpusVersion,
    IReadOnlyList<string> CaseIds,
    IReadOnlyList<string> CoverageCaseIds,
    IReadOnlyList<string> ThreatClasses,
    IReadOnlyList<string> AnswerDispositions,
    IReadOnlyList<string> SafetyStatuses,
    IReadOnlyList<string> OwnerRoleIds,
    IReadOnlyList<AdversarialExpectedCase> ExpectedCases,
    int LongTailCaseCount);

public sealed record AdversarialExpectedCase(
    string CaseId,
    AdversarialExpectedDeclarations Declarations);

public sealed record AdversarialExpectedDeclarations(
    string AnswerDisposition,
    string SafetyStatus,
    string HandlingClass,
    bool HumanReviewRequired,
    string ReceiptEventClass,
    IReadOnlyList<string> ReceiptDecisionCodes,
    string CitationMode,
    IReadOnlyList<string> CitationSourceIds);
