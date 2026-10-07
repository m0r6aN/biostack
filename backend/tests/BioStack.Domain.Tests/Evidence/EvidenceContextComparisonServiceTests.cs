namespace BioStack.Domain.Tests.Evidence;

using BioStack.Domain.Evidence;
using Xunit;

/// <summary>
/// Class B comparison tests. The 12 mg vs 0.5–1.0 mg case is the kickoff worked example
/// of informal online initiation advice versus reviewed trial initiation ranges.
/// </summary>
public sealed class EvidenceContextComparisonServiceTests
{
    private readonly EvidenceContextComparisonService _sut = new();

    [Fact]
    public void Compare_twelve_mg_vs_half_to_one_mg_weekly_initiation_flags_outside_reviewed_context()
    {
        // Illustrative harm-reduction pattern (e.g. informal 12 mg "start" vs studied 0.5–1.0 mg weekly).
        var exposure = new ProtocolExposure(
            SubjectName: "retatrutide",
            Amount: 12m,
            Unit: "mg",
            Route: "subcutaneous",
            Frequency: "weekly");

        var evidence = new ReviewedEvidenceProfile(
            SubjectName: "retatrutide",
            Regimens:
            [
                new PublishedExposureRegimen(
                    StudyArm: "initiation",
                    Substance: "retatrutide",
                    InitiationAmountMin: 0.5m,
                    InitiationAmountMax: 1.0m,
                    MaintenanceAmountMin: null,
                    MaintenanceAmountMax: null,
                    MaximumStudiedAmount: 12m,
                    Unit: "mg",
                    Route: "subcutaneous",
                    Frequency: "weekly",
                    SourceCitation: "Reviewed trial packet (illustrative fixture)",
                    SourceLocation: "methods/dosing",
                    EvidenceClass: "controlled_human_trial",
                    PopulationSummary: "Adults with overweight or obesity"),
            ]);

        var result = _sut.Compare(exposure, evidence);

        Assert.Contains(EvidenceComparisonSignals.AboveReviewedInitiationRange, result.RiskSignals);
        Assert.Equal(0.5m, result.ClosestInitiationMin);
        Assert.Equal(1.0m, result.ClosestInitiationMax);
        Assert.Equal(12m, result.UnitNormalizedUserAmount);
        Assert.Equal(24m, result.TimesAboveInitiationMin); // 12 / 0.5
        Assert.Equal(12m, result.TimesAboveInitiationMax); // 12 / 1.0
        Assert.Contains(result.SourceReferences, s => s.Contains("Reviewed trial", StringComparison.OrdinalIgnoreCase));

        var text = string.Join(' ', result.Statements);
        Assert.Contains("0.5", text, StringComparison.Ordinal);
        Assert.Contains("1", text, StringComparison.Ordinal);
        Assert.Contains("12", text, StringComparison.Ordinal);
        Assert.Contains("times", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No reviewed trial in this evidence set initiated participants at the entered amount.", text, StringComparison.Ordinal);

        // Class D prohibitions: no personal prescription / safety claims.
        Assert.DoesNotContain("you should", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("safe for you", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("will harm you", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("start at 0.5", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compare_amount_inside_initiation_range_is_exact_match_context()
    {
        var exposure = new ProtocolExposure("compound-x", 0.75m, "mg", Frequency: "weekly");
        var evidence = new ReviewedEvidenceProfile(
            "compound-x",
            [
                new PublishedExposureRegimen(
                    "arm-a",
                    "compound-x",
                    0.5m,
                    1.0m,
                    null,
                    null,
                    2.0m,
                    "mg",
                    null,
                    "weekly",
                    "PMID:000"),
            ]);

        var result = _sut.Compare(exposure, evidence);

        Assert.Contains(EvidenceComparisonSignals.ExactMatchStudyContext, result.RiskSignals);
        Assert.DoesNotContain(EvidenceComparisonSignals.AboveReviewedInitiationRange, result.RiskSignals);
    }

    [Fact]
    public void Compare_mcg_to_mg_normalizes_units()
    {
        var exposure = new ProtocolExposure("compound-y", 1000m, "mcg", Frequency: "weekly");
        var evidence = new ReviewedEvidenceProfile(
            "compound-y",
            [
                new PublishedExposureRegimen(
                    "arm-a",
                    "compound-y",
                    0.5m,
                    1.0m,
                    null,
                    null,
                    null,
                    "mg",
                    null,
                    "weekly",
                    "PMID:001"),
            ]);

        var result = _sut.Compare(exposure, evidence);

        Assert.Equal(1.0m, result.UnitNormalizedUserAmount);
        Assert.Contains(EvidenceComparisonSignals.ExactMatchStudyContext, result.RiskSignals);
    }

    [Fact]
    public void Compare_route_not_in_reviewed_set_flags_route()
    {
        var exposure = new ProtocolExposure(
            "compound-z",
            1m,
            "mg",
            Route: "oral",
            Frequency: "weekly");
        var evidence = new ReviewedEvidenceProfile(
            "compound-z",
            [
                new PublishedExposureRegimen(
                    "arm-a",
                    "compound-z",
                    0.5m,
                    1.0m,
                    null,
                    null,
                    null,
                    "mg",
                    "subcutaneous",
                    "weekly",
                    "PMID:002"),
            ]);

        var result = _sut.Compare(exposure, evidence);

        Assert.True(result.RouteMismatch);
        Assert.Contains(EvidenceComparisonSignals.RouteNotStudied, result.RiskSignals);
    }
}
