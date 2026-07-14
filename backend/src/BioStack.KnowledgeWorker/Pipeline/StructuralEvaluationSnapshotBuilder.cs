namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;

/// <summary>
/// Produces an offline structural snapshot from already validated evaluation artifacts.
/// This is aggregate contract evidence only; it does not execute or score model output.
/// </summary>
public sealed class StructuralEvaluationSnapshotBuilder
{
    public const string CurrentSnapshotVersion = "1.0.0";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly EvaluationCoverageArtifactLoader _coverageLoader;
    private readonly ProtocolDesignFixtureCorpusLoader _fixtureLoader;
    private readonly AdversarialQueryCorpusLoader _adversarialLoader;

    public StructuralEvaluationSnapshotBuilder(string? repositoryRoot = null)
    {
        _coverageLoader = new EvaluationCoverageArtifactLoader(repositoryRoot);
        _fixtureLoader = new ProtocolDesignFixtureCorpusLoader(repositoryRoot);
        _adversarialLoader = new AdversarialQueryCorpusLoader(repositoryRoot);
    }

    public StructuralEvaluationSnapshot Build()
        => Build(_coverageLoader.Load(), _fixtureLoader.Load(), _adversarialLoader.Load());

    public StructuralEvaluationSnapshot Build(
        EvaluationCoverageArtifactSet coverage,
        ProtocolDesignFixtureCorpus fixtures,
        AdversarialQueryCorpus adversarial)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        ArgumentNullException.ThrowIfNull(fixtures);
        ArgumentNullException.ThrowIfNull(adversarial);

        EnsureEqual(coverage.TaxonomyVersion, fixtures.TaxonomyVersion, "fixture taxonomy version");
        EnsureEqual(coverage.MatrixVersion, fixtures.MatrixVersion, "fixture matrix version");
        EnsureEqual(coverage.TaxonomyVersion, adversarial.TaxonomyVersion, "adversarial taxonomy version");
        EnsureEqual(coverage.MatrixVersion, adversarial.MatrixVersion, "adversarial matrix version");
        EnsureEqual(fixtures.ArtifactVersion, adversarial.FixtureCorpusVersion, "adversarial fixture corpus version");

        var fixtureDifference = CompareCases(coverage.CoverageCaseIds, fixtures.CoverageCaseIds);
        var adversarialDifference = CompareCases(coverage.CoverageCaseIds, adversarial.CoverageCaseIds);
        var ownerRoleIds = coverage.OwnerRoleIds
            .Concat(fixtures.OwnerRoleIds)
            .Concat(adversarial.OwnerRoleIds)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new StructuralEvaluationSnapshot(
            SnapshotVersion: CurrentSnapshotVersion,
            Scope: "offline-structural-only",
            TaxonomyVersion: coverage.TaxonomyVersion,
            MatrixVersion: coverage.MatrixVersion,
            FixtureCorpusVersion: fixtures.ArtifactVersion,
            AdversarialCorpusVersion: adversarial.ArtifactVersion,
            Taxonomy: new StructuralTaxonomyCounts(
                DimensionCount: coverage.DimensionIds.Count,
                CoverageCaseCount: coverage.CoverageCaseIds.Count,
                RequiredValuePairCount: coverage.RequiredValuePairIds.Count),
            ProtocolFixtures: new StructuralProtocolFixtureCounts(
                FixtureCount: fixtures.FixtureIds.Count,
                InputTypeCount: fixtures.InputTypes.Count,
                BehaviorClassCount: fixtures.BehaviorClasses.Count,
                CoverageTagCount: fixtures.CoverageTags.Count),
            AdversarialQueries: new StructuralAdversarialCounts(
                CaseCount: adversarial.CaseIds.Count,
                ThreatClassCount: adversarial.ThreatClasses.Count,
                AnswerDispositionCount: adversarial.AnswerDispositions.Count,
                SafetyStatusCount: adversarial.SafetyStatuses.Count,
                LongTailCaseCount: adversarial.LongTailCaseCount),
            FixtureCoverageCases: fixtureDifference,
            AdversarialCoverageCases: adversarialDifference,
            OwnerRoleIds: ownerRoleIds,
            ModelInvoked: false,
            NetworkAccessed: false,
            Limitations:
            [
                "no semantic correctness or factuality scoring",
                "no retrieval, citation, safety-quality, or refusal-quality scoring",
                "no thresholds, baselines, regression gate, latency, or cost measurement",
                "no runtime, staging, production, or live model validation",
            ]);
    }

    public string BuildJson()
        => JsonSerializer.Serialize(Build(), SerializerOptions);

    private static StructuralCoverageCaseDifference CompareCases(
        IReadOnlyList<string> declared,
        IReadOnlyList<string> observed)
    {
        var missing = declared
            .Except(observed, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var unexpected = observed
            .Except(declared, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new StructuralCoverageCaseDifference(
            SetsMatch: missing.Length == 0 && unexpected.Length == 0,
            MissingCaseIds: missing,
            UnexpectedCaseIds: unexpected);
    }

    private static void EnsureEqual(string expected, string actual, string field)
    {
        if (!StringComparer.Ordinal.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Structural evaluation snapshot cannot compare {field}: expected '{expected}', found '{actual}'.");
        }
    }
}

public sealed record StructuralEvaluationSnapshot(
    string SnapshotVersion,
    string Scope,
    string TaxonomyVersion,
    string MatrixVersion,
    string FixtureCorpusVersion,
    string AdversarialCorpusVersion,
    StructuralTaxonomyCounts Taxonomy,
    StructuralProtocolFixtureCounts ProtocolFixtures,
    StructuralAdversarialCounts AdversarialQueries,
    StructuralCoverageCaseDifference FixtureCoverageCases,
    StructuralCoverageCaseDifference AdversarialCoverageCases,
    IReadOnlyList<string> OwnerRoleIds,
    bool ModelInvoked,
    bool NetworkAccessed,
    IReadOnlyList<string> Limitations);

public sealed record StructuralTaxonomyCounts(
    int DimensionCount,
    int CoverageCaseCount,
    int RequiredValuePairCount);

public sealed record StructuralProtocolFixtureCounts(
    int FixtureCount,
    int InputTypeCount,
    int BehaviorClassCount,
    int CoverageTagCount);

public sealed record StructuralAdversarialCounts(
    int CaseCount,
    int ThreatClassCount,
    int AnswerDispositionCount,
    int SafetyStatusCount,
    int LongTailCaseCount);

public sealed record StructuralCoverageCaseDifference(
    bool SetsMatch,
    IReadOnlyList<string> MissingCaseIds,
    IReadOnlyList<string> UnexpectedCaseIds);
