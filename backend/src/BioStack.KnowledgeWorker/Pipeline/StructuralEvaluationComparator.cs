namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Compares validated expected declarations with validated candidate declarations.
/// Equality here is structural metadata evidence only; it is never semantic truth,
/// a policy threshold, a release verdict, or effect authority.
/// </summary>
public sealed class StructuralEvaluationComparator
{
    public const string CurrentComparisonVersion = "1.0.0";

    private static readonly string[] RequiredFieldIds =
    [
        "answer_disposition",
        "citation_mode",
        "citation_source_ids",
        "handling_class",
        "human_review_required",
        "receipt_decision_codes",
        "receipt_event_class",
        "safety_status",
    ];

    private readonly AdversarialQueryCorpusLoader _expectedLoader;
    private readonly EvaluationCandidateOutputEnvelopeLoader _candidateLoader;

    public StructuralEvaluationComparator(string? repositoryRoot = null)
    {
        _expectedLoader = new AdversarialQueryCorpusLoader(repositoryRoot);
        _candidateLoader = new EvaluationCandidateOutputEnvelopeLoader(repositoryRoot);
    }

    public StructuralEvaluationComparison Build()
        => Compare(_expectedLoader.Load(), _candidateLoader.Load());

    public StructuralEvaluationComparison Compare(
        AdversarialQueryCorpus expected,
        EvaluationCandidateOutputEnvelope candidate)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(candidate);

        EnsureEqual(expected.ArtifactVersion, candidate.AdversarialCorpusVersion, "adversarial corpus version");
        EnsureEqual(expected.TaxonomyVersion, candidate.TaxonomyVersion, "taxonomy version");
        EnsureEqual(expected.MatrixVersion, candidate.MatrixVersion, "matrix version");
        EnsureEqual(expected.FixtureCorpusVersion, candidate.FixtureCorpusVersion, "fixture corpus version");
        EnsureEqual("none", candidate.EffectAuthority, "candidate effect authority");
        EnsureFalse(candidate.RuntimeTruth, "candidate runtime truth");
        EnsureFalse(candidate.ModelInvokedByLoader, "candidate loader model invocation");
        EnsureFalse(candidate.NetworkAccessedByLoader, "candidate loader network access");

        var projectedExpectedIds = expected.ExpectedCases.Select(item => item.CaseId).ToArray();
        var expectedByCase = ToUniqueExpectedCases(expected.ExpectedCases);
        EnsureEqualIds(expected.CaseIds, projectedExpectedIds, "expected case projection");
        var candidateIds = candidate.Results.Select(result => result.CaseId).ToArray();
        EnsureUniqueAndOrdered(candidateIds, "candidate case IDs");

        var cases = new List<StructuralCaseComparison>(candidate.Results.Count);
        foreach (var result in candidate.Results)
        {
            if (!expectedByCase.TryGetValue(result.CaseId, out var expectedCase))
            {
                throw ContractError($"candidate references unknown expected case '{result.CaseId}'.");
            }

            var fields = CompareDeclarations(expectedCase.Declarations, result.Declarations);
            cases.Add(new StructuralCaseComparison(
                CaseId: result.CaseId,
                CandidateOutputSha256: result.CandidateOutputSha256,
                ExactStructuralMatch: fields.All(field => field.MatchesExpected),
                Fields: fields));
        }

        var missingCandidateCaseIds = expected.CaseIds
            .Except(candidateIds, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var fieldCounts = RequiredFieldIds
            .Select(fieldId =>
            {
                var exactMatchCount = cases.Count(item =>
                    item.Fields.Single(field => StringComparer.Ordinal.Equals(field.FieldId, fieldId))
                        .MatchesExpected);
                return new StructuralFieldComparisonCount(
                    FieldId: fieldId,
                    ComparedCaseCount: cases.Count,
                    ExactMatchCount: exactMatchCount,
                    MismatchCount: cases.Count - exactMatchCount);
            })
            .ToArray();

        return new StructuralEvaluationComparison(
            ComparisonVersion: CurrentComparisonVersion,
            Scope: "offline-structural-declarations-only",
            ExpectedCorpusVersion: expected.ArtifactVersion,
            CandidateEnvelopeVersion: candidate.ArtifactVersion,
            CandidateConfigurationSha256: candidate.CandidateConfigurationSha256,
            CandidateCoverageStatus: missingCandidateCaseIds.Length == 0 ? "complete" : "partial",
            OverallVerdict: "not_evaluated",
            ExpectedCaseCount: expected.CaseIds.Count,
            CandidateCaseCount: candidate.Results.Count,
            MissingCandidateCaseIds: missingCandidateCaseIds,
            Cases: cases,
            FieldCounts: fieldCounts,
            CandidateDeclarationsTrusted: false,
            ModelInvokedByComparator: false,
            NetworkAccessedByComparator: false,
            EffectAuthority: "none",
            Limitations:
            [
                "exact metadata equality is not semantic factuality, safety quality, or production truth",
                "partial candidate coverage carries no corpus-wide verdict",
                "no rates, thresholds, baselines, waivers, regression gate, latency, or cost measurement",
                "no raw input or output inspection, model invocation, runtime, staging, production, or live validation",
            ]);
    }

    private static Dictionary<string, AdversarialExpectedCase> ToUniqueExpectedCases(
        IReadOnlyList<AdversarialExpectedCase> expectedCases)
    {
        var ids = expectedCases.Select(item => item.CaseId).ToArray();
        EnsureUniqueAndOrdered(ids, "expected case IDs");
        return expectedCases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
    }

    private static IReadOnlyList<StructuralDeclarationFieldComparison> CompareDeclarations(
        AdversarialExpectedDeclarations expected,
        EvaluationCandidateDeclarations candidate)
        =>
        [
            Field("answer_disposition", Equal(expected.AnswerDisposition, candidate.AnswerDisposition)),
            Field("citation_mode", Equal(expected.CitationMode, candidate.CitationMode)),
            Field("citation_source_ids", Equal(expected.CitationSourceIds, candidate.CitationSourceIds)),
            Field("handling_class", Equal(expected.HandlingClass, candidate.HandlingClass)),
            Field("human_review_required", expected.HumanReviewRequired == candidate.HumanReviewRequired),
            Field("receipt_decision_codes", Equal(expected.ReceiptDecisionCodes, candidate.ReceiptDecisionCodes)),
            Field("receipt_event_class", Equal(expected.ReceiptEventClass, candidate.ReceiptEventClass)),
            Field("safety_status", Equal(expected.SafetyStatus, candidate.SafetyStatus)),
        ];

    private static StructuralDeclarationFieldComparison Field(string fieldId, bool matchesExpected)
        => new(fieldId, matchesExpected);

    private static bool Equal(string expected, string candidate)
        => StringComparer.Ordinal.Equals(expected, candidate);

    private static bool Equal(IReadOnlyList<string> expected, IReadOnlyList<string> candidate)
        => expected.SequenceEqual(candidate, StringComparer.Ordinal);

    private static void EnsureUniqueAndOrdered(IReadOnlyList<string> ids, string label)
    {
        var ordered = ids.Order(StringComparer.Ordinal).ToArray();
        if (!ids.SequenceEqual(ordered, StringComparer.Ordinal) ||
            ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
        {
            throw ContractError($"{label} must be unique and ordinally sorted.");
        }
    }

    private static void EnsureEqualIds(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual,
        string label)
    {
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw ContractError($"{label} must exactly match the validated corpus case IDs.");
        }
    }

    private static void EnsureFalse(bool value, string label)
    {
        if (value)
        {
            throw ContractError($"{label} must remain false.");
        }
    }

    private static void EnsureEqual(string expected, string actual, string label)
    {
        if (!StringComparer.Ordinal.Equals(expected, actual))
        {
            throw ContractError($"{label} must be '{expected}', actual '{actual}'.");
        }
    }

    private static InvalidOperationException ContractError(string message)
        => new($"KEO-78 structural comparison invalid: {message}");
}

public sealed record StructuralEvaluationComparison(
    string ComparisonVersion,
    string Scope,
    string ExpectedCorpusVersion,
    string CandidateEnvelopeVersion,
    string CandidateConfigurationSha256,
    string CandidateCoverageStatus,
    string OverallVerdict,
    int ExpectedCaseCount,
    int CandidateCaseCount,
    IReadOnlyList<string> MissingCandidateCaseIds,
    IReadOnlyList<StructuralCaseComparison> Cases,
    IReadOnlyList<StructuralFieldComparisonCount> FieldCounts,
    bool CandidateDeclarationsTrusted,
    bool ModelInvokedByComparator,
    bool NetworkAccessedByComparator,
    string EffectAuthority,
    IReadOnlyList<string> Limitations);

public sealed record StructuralCaseComparison(
    string CaseId,
    string CandidateOutputSha256,
    bool ExactStructuralMatch,
    IReadOnlyList<StructuralDeclarationFieldComparison> Fields);

public sealed record StructuralDeclarationFieldComparison(
    string FieldId,
    bool MatchesExpected);

public sealed record StructuralFieldComparisonCount(
    string FieldId,
    int ComparedCaseCount,
    int ExactMatchCount,
    int MismatchCount);
