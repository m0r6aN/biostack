namespace BioStack.Application.Tests.Evidence;

using BioStack.Application.Evidence;
using BioStack.Domain.Entities;
using BioStack.Domain.Evidence;
using Xunit;

public sealed class KnowledgeEntryExposureProfileBuilderTests
{
    [Fact]
    public void TryParseAmount_parses_range_and_unit()
    {
        Assert.True(KnowledgeEntryExposureProfileBuilder.TryParseAmount(
            "0.5-1.0 mg weekly",
            out var min,
            out var max,
            out var unit));
        Assert.Equal(0.5m, min);
        Assert.Equal(1.0m, max);
        Assert.Equal("mg", unit);
    }

    [Fact]
    public void TryBuild_from_standard_range_enables_comparison_profile()
    {
        var entry = new KnowledgeEntry
        {
            CanonicalName = "retatrutide",
            StandardDosageRange = "0.5-1.0 mg",
            MaxReportedDose = "12 mg",
            Frequency = "weekly",
            SourceReferences = ["PMID:illustrative"],
        };

        var profile = KnowledgeEntryExposureProfileBuilder.TryBuild(entry);
        Assert.NotNull(profile);
        Assert.Single(profile!.Regimens);
        Assert.Equal(0.5m, profile.Regimens[0].InitiationAmountMin);
        Assert.Equal(1.0m, profile.Regimens[0].InitiationAmountMax);
    }

    [Fact]
    public void ProtocolComparer_flags_twelve_mg_against_half_to_one_range()
    {
        var comparer = new ProtocolEvidenceContextComparer(new EvidenceContextComparisonService());
        var knowledge = new Dictionary<string, KnowledgeEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["retatrutide"] = new KnowledgeEntry
            {
                CanonicalName = "retatrutide",
                StandardDosageRange = "0.5-1.0 mg",
                Frequency = "weekly",
                SourceReferences = ["Reviewed trial packet"],
            },
        };

        var protocol = new List<BioStack.Contracts.Responses.ProtocolEntryResponse>
        {
            new("retatrutide", 12, "mg", "weekly", string.Empty, Recognized: true),
        };

        var comparisons = comparer.CompareProtocolEntries(protocol, knowledge);
        Assert.Single(comparisons);
        Assert.True(comparisons[0].ComparisonAvailable);
        Assert.Contains(EvidenceComparisonSignals.AboveReviewedInitiationRange, comparisons[0].RiskSignals);

        var issues = ProtocolEvidenceContextComparer.ToIssues(comparisons);
        Assert.Contains(issues, i => i.Type == "evidence_context");
    }
}
