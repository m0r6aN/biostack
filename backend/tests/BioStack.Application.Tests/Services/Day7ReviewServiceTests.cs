namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using Xunit;

public class Day7ReviewServiceTests
{
    private readonly Day7ReviewService _service = new();

    [Fact]
    public void Evaluate_WithImprovingTrend_ReturnsClearContinueSignal()
    {
        var checkIns = CreateCheckIns(
            sleep: new[] { 4, 4, 5, 6, 7 },
            energy: new[] { 4, 5, 5, 7, 8 },
            recovery: new[] { 5, 5, 6, 7, 8 });

        var review = _service.Evaluate(checkIns);

        Assert.Equal("improving", review.SleepTrend);
        Assert.Equal("improving", review.EnergyTrend);
        Assert.Equal("improving", review.RecoveryTrend);
        Assert.Equal("clear", review.SignalStrength);
        Assert.Equal("continue", review.NextStep);
    }

    [Fact]
    public void Evaluate_WithFlatTrend_ReturnsWeakTrackLongerSignal()
    {
        var checkIns = CreateCheckIns(
            sleep: new[] { 6, 6, 6, 6, 6 },
            energy: new[] { 5, 5, 6, 5, 5 },
            recovery: new[] { 7, 7, 7, 7, 7 });

        var review = _service.Evaluate(checkIns);

        Assert.Equal("flat", review.SleepTrend);
        Assert.Equal("flat", review.EnergyTrend);
        Assert.Equal("flat", review.RecoveryTrend);
        Assert.Equal("weak", review.SignalStrength);
        Assert.Equal("track_longer", review.NextStep);
    }

    [Fact]
    public void Evaluate_WithDecliningTrend_ReturnsClearReassessSignal()
    {
        var checkIns = CreateCheckIns(
            sleep: new[] { 8, 7, 6, 5, 4 },
            energy: new[] { 8, 8, 7, 6, 5 },
            recovery: new[] { 7, 7, 6, 5, 4 });

        var review = _service.Evaluate(checkIns);

        Assert.Equal("declining", review.SleepTrend);
        Assert.Equal("declining", review.EnergyTrend);
        Assert.Equal("declining", review.RecoveryTrend);
        Assert.Equal("clear", review.SignalStrength);
        Assert.Equal("reassess", review.NextStep);
    }

    [Fact]
    public void Evaluate_WithInsufficientData_ReturnsFirstClassInsufficientState()
    {
        var checkIns = CreateCheckIns(
            sleep: new[] { 5, 6, 7, 8 },
            energy: new[] { 5, 6, 7, 8 },
            recovery: new[] { 5, 6, 7, 8 });

        var review = _service.Evaluate(checkIns);

        Assert.Equal("insufficient_data", review.SleepTrend);
        Assert.Equal("insufficient_data", review.EnergyTrend);
        Assert.Equal("insufficient_data", review.RecoveryTrend);
        Assert.Equal("weak", review.SignalStrength);
        Assert.Equal("track_longer", review.NextStep);
    }

    [Fact]
    public void Evaluate_WithMixedTrend_ReturnsWeakUnclearPattern()
    {
        var checkIns = CreateCheckIns(
            sleep: new[] { 4, 5, 6, 7, 8 },
            energy: new[] { 8, 7, 6, 5, 4 },
            recovery: new[] { 6, 6, 6, 6, 6 });

        var review = _service.Evaluate(checkIns);

        Assert.Equal("improving", review.SleepTrend);
        Assert.Equal("declining", review.EnergyTrend);
        Assert.Equal("flat", review.RecoveryTrend);
        Assert.Equal("weak", review.SignalStrength);
        Assert.Equal("track_longer", review.NextStep);
        Assert.Contains("mixed", review.TrendSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_DefaultsAlignmentToUnclearWithoutGroundedExpectedWindow()
    {
        var checkIns = CreateCheckIns(
            sleep: new[] { 4, 4, 5, 6, 7 },
            energy: new[] { 4, 5, 5, 7, 8 },
            recovery: new[] { 5, 5, 6, 7, 8 });

        var review = _service.Evaluate(checkIns);

        Assert.Equal("unclear", review.AlignmentWithExpected);
        Assert.DoesNotContain("caused", review.TrendSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("working", review.TrendSummary, StringComparison.OrdinalIgnoreCase);
    }

    private static List<CheckIn> CreateCheckIns(int[] sleep, int[] energy, int[] recovery)
    {
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        return sleep.Select((sleepValue, index) => new CheckIn
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            Date = start.AddDays(index),
            Weight = 0,
            SleepQuality = sleepValue,
            Energy = energy[index],
            Appetite = 5,
            Recovery = recovery[index]
        }).ToList();
    }
}
