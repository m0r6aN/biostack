namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class StructuralEvaluationSnapshotBuilderTests
{
    [Fact]
    public void Build_CurrentArtifacts_ReportsOnlyDeterministicStructuralCoverage()
    {
        var snapshot = new StructuralEvaluationSnapshotBuilder().Build();

        Assert.Equal("1.0.0", snapshot.SnapshotVersion);
        Assert.Equal("offline-structural-only", snapshot.Scope);
        Assert.Equal("1.0.0", snapshot.TaxonomyVersion);
        Assert.Equal("1.0.0", snapshot.MatrixVersion);
        Assert.Equal("1.0.0", snapshot.FixtureCorpusVersion);
        Assert.Equal("1.0.0", snapshot.AdversarialCorpusVersion);

        Assert.Equal(new StructuralTaxonomyCounts(16, 16, 8), snapshot.Taxonomy);
        Assert.Equal(new StructuralProtocolFixtureCounts(20, 4, 5, 24), snapshot.ProtocolFixtures);
        Assert.Equal(20, snapshot.AdversarialQueries.CaseCount);
        Assert.Equal(9, snapshot.AdversarialQueries.ThreatClassCount);
        Assert.Equal(4, snapshot.AdversarialQueries.AnswerDispositionCount);
        Assert.Equal(4, snapshot.AdversarialQueries.SafetyStatusCount);

        Assert.True(snapshot.FixtureCoverageCases.SetsMatch);
        Assert.Empty(snapshot.FixtureCoverageCases.MissingCaseIds);
        Assert.Empty(snapshot.FixtureCoverageCases.UnexpectedCaseIds);
        Assert.True(snapshot.AdversarialCoverageCases.SetsMatch);
        Assert.Empty(snapshot.AdversarialCoverageCases.MissingCaseIds);
        Assert.Empty(snapshot.AdversarialCoverageCases.UnexpectedCaseIds);
        Assert.All(snapshot.OwnerRoleIds, roleId => Assert.StartsWith("role:pending:", roleId));
        Assert.False(snapshot.ModelInvoked);
        Assert.False(snapshot.NetworkAccessed);
        Assert.Contains(snapshot.Limitations, value => value.Contains("no thresholds", StringComparison.Ordinal));
        Assert.Contains(snapshot.Limitations, value => value.Contains("no runtime", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildJson_CurrentArtifacts_IsStableAndCarriesLimitations()
    {
        var builder = new StructuralEvaluationSnapshotBuilder();

        var first = builder.BuildJson();
        var second = builder.BuildJson();
        var json = JsonNode.Parse(first)!.AsObject();

        Assert.Equal(first, second);
        Assert.Equal("offline-structural-only", (string?)json["scope"]);
        Assert.False((bool)json["modelInvoked"]!);
        Assert.False((bool)json["networkAccessed"]!);
        Assert.Equal(4, json["limitations"]!.AsArray().Count);
        Assert.Null(json["generatedAtUtc"]);
    }

    [Fact]
    public void Build_DifferentCoverageSets_ReportsExactMissingAndUnexpectedIds()
    {
        var coverage = new EvaluationCoverageArtifactLoader().Load();
        var fixtures = new ProtocolDesignFixtureCorpusLoader().Load();
        var adversarial = new AdversarialQueryCorpusLoader().Load();
        var changedFixtures = fixtures with
        {
            CoverageCaseIds = fixtures.CoverageCaseIds
                .Skip(1)
                .Append("coverage:unexpected")
                .ToArray(),
        };

        var snapshot = new StructuralEvaluationSnapshotBuilder().Build(
            coverage,
            changedFixtures,
            adversarial);

        Assert.False(snapshot.FixtureCoverageCases.SetsMatch);
        Assert.Equal([coverage.CoverageCaseIds[0]], snapshot.FixtureCoverageCases.MissingCaseIds);
        Assert.Equal(["coverage:unexpected"], snapshot.FixtureCoverageCases.UnexpectedCaseIds);
    }

    [Fact]
    public void Build_VersionDisagreement_FailsClosed()
    {
        var coverage = new EvaluationCoverageArtifactLoader().Load();
        var fixtures = new ProtocolDesignFixtureCorpusLoader().Load() with
        {
            MatrixVersion = "unexpected-version",
        };
        var adversarial = new AdversarialQueryCorpusLoader().Load();

        var error = Assert.Throws<InvalidOperationException>(() =>
            new StructuralEvaluationSnapshotBuilder().Build(coverage, fixtures, adversarial));

        Assert.Contains("fixture matrix version", error.Message, StringComparison.Ordinal);
        Assert.Contains("unexpected-version", error.Message, StringComparison.Ordinal);
    }
}
