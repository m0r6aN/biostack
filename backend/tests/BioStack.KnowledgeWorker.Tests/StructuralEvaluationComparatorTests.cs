namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class StructuralEvaluationComparatorTests
{
    [Fact]
    public void Build_CurrentArtifacts_RecordsExactPartialStructuralComparisonWithoutVerdict()
    {
        var comparison = new StructuralEvaluationComparator().Build();

        Assert.Equal("1.0.0", comparison.ComparisonVersion);
        Assert.Equal("offline-structural-declarations-only", comparison.Scope);
        Assert.Equal("1.0.0", comparison.ExpectedCorpusVersion);
        Assert.Equal("1.0.0", comparison.CandidateEnvelopeVersion);
        Assert.Equal(64, comparison.CandidateConfigurationSha256.Length);
        Assert.Equal("partial", comparison.CandidateCoverageStatus);
        Assert.Equal("not_evaluated", comparison.OverallVerdict);
        Assert.Equal(20, comparison.ExpectedCaseCount);
        Assert.Equal(4, comparison.CandidateCaseCount);
        Assert.Equal(16, comparison.MissingCandidateCaseIds.Count);
        Assert.Equal(4, comparison.Cases.Count);
        Assert.All(comparison.Cases, item => Assert.True(item.ExactStructuralMatch));
        Assert.Equal(8, comparison.FieldCounts.Count);
        Assert.All(comparison.FieldCounts, field =>
        {
            Assert.Equal(4, field.ComparedCaseCount);
            Assert.Equal(4, field.ExactMatchCount);
            Assert.Equal(0, field.MismatchCount);
        });
        Assert.False(comparison.CandidateDeclarationsTrusted);
        Assert.False(comparison.ModelInvokedByComparator);
        Assert.False(comparison.NetworkAccessedByComparator);
        Assert.Equal("none", comparison.EffectAuthority);
        Assert.Contains(
            comparison.Limitations,
            value => value.Contains("not semantic factuality", StringComparison.Ordinal));
        Assert.Contains(
            comparison.Limitations,
            value => value.Contains("partial candidate coverage", StringComparison.Ordinal));
    }

    [Fact]
    public void Compare_OneDeclarationMismatch_RecordsExactFieldCountsWithoutPassFail()
    {
        var expected = new AdversarialQueryCorpusLoader().Load();
        var candidate = new EvaluationCandidateOutputEnvelopeLoader().Load();
        var first = candidate.Results[0];
        var changed = candidate with
        {
            Results =
            [
                first with
                {
                    Declarations = first.Declarations with { AnswerDisposition = "unknown" },
                },
                .. candidate.Results.Skip(1),
            ],
        };

        var comparison = new StructuralEvaluationComparator().Compare(expected, changed);

        var changedCase = comparison.Cases[0];
        Assert.False(changedCase.ExactStructuralMatch);
        Assert.False(Assert.Single(
            changedCase.Fields,
            field => field.FieldId == "answer_disposition").MatchesExpected);
        Assert.All(
            changedCase.Fields.Where(field => field.FieldId != "answer_disposition"),
            field => Assert.True(field.MatchesExpected));

        var answerDisposition = Assert.Single(
            comparison.FieldCounts,
            field => field.FieldId == "answer_disposition");
        Assert.Equal(4, answerDisposition.ComparedCaseCount);
        Assert.Equal(3, answerDisposition.ExactMatchCount);
        Assert.Equal(1, answerDisposition.MismatchCount);
        Assert.Equal("not_evaluated", comparison.OverallVerdict);
        Assert.DoesNotContain(
            comparison.FieldCounts.Select(field => field.FieldId),
            fieldId => fieldId.Contains("score", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compare_DifferentOrderedIdLists_RecordsStructuralMismatch()
    {
        var expected = new AdversarialQueryCorpusLoader().Load();
        var candidate = new EvaluationCandidateOutputEnvelopeLoader().Load();
        var target = candidate.Results.Single(result =>
            result.CaseId == "adversarial-006-ambiguous-entity");
        var changed = candidate with
        {
            Results = candidate.Results
                .Select(result => result.CaseId == target.CaseId
                    ? result with
                    {
                        Declarations = result.Declarations with
                        {
                            ReceiptDecisionCodes = result.Declarations.ReceiptDecisionCodes.Reverse().ToArray(),
                        },
                    }
                    : result)
                .ToArray(),
        };

        var comparison = new StructuralEvaluationComparator().Compare(expected, changed);
        var changedCase = comparison.Cases.Single(item => item.CaseId == target.CaseId);

        Assert.False(Assert.Single(
            changedCase.Fields,
            field => field.FieldId == "receipt_decision_codes").MatchesExpected);
    }

    [Fact]
    public void Compare_UnknownCandidateCase_FailsClosed()
    {
        var expected = new AdversarialQueryCorpusLoader().Load();
        var candidate = new EvaluationCandidateOutputEnvelopeLoader().Load();
        var unknown = candidate.Results[0] with { CaseId = "adversarial-999-unknown" };
        var changed = candidate with { Results = [unknown] };

        var error = Assert.Throws<InvalidOperationException>(
            () => new StructuralEvaluationComparator().Compare(expected, changed));

        Assert.Contains("unknown expected case", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_VersionMismatch_FailsClosed()
    {
        var expected = new AdversarialQueryCorpusLoader().Load();
        var candidate = new EvaluationCandidateOutputEnvelopeLoader().Load() with
        {
            AdversarialCorpusVersion = "9.9.9",
        };

        var error = Assert.Throws<InvalidOperationException>(
            () => new StructuralEvaluationComparator().Compare(expected, candidate));

        Assert.Contains("adversarial corpus version", error.Message, StringComparison.Ordinal);
        Assert.Contains("9.9.9", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_RuntimeTruthClaim_FailsClosed()
    {
        var expected = new AdversarialQueryCorpusLoader().Load();
        var candidate = new EvaluationCandidateOutputEnvelopeLoader().Load() with
        {
            RuntimeTruth = true,
        };

        var error = Assert.Throws<InvalidOperationException>(
            () => new StructuralEvaluationComparator().Compare(expected, candidate));

        Assert.Contains("runtime truth", error.Message, StringComparison.Ordinal);
        Assert.Contains("must remain false", error.Message, StringComparison.Ordinal);
    }
}
