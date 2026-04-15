namespace BioStack.Application.Services;

using BioStack.Domain.Entities;

public sealed class Day7ReviewService
{
    private const int WindowDays = 7;
    private const int MinimumCoveredDays = 5;
    private const decimal TrendThreshold = 0.75m;

    public Day7ReviewResult Evaluate(IEnumerable<CheckIn> checkIns)
    {
        var window = GetLatestWindow(checkIns);
        var coveredDays = window.Select(checkIn => checkIn.Date.Date).Distinct().Count();

        if (coveredDays < MinimumCoveredDays)
        {
            return new Day7ReviewResult(
                "insufficient_data",
                "insufficient_data",
                "insufficient_data",
                "Not enough check-ins yet to form a 7-day review.",
                "weak",
                "unclear",
                "track_longer",
                $"Record at least {MinimumCoveredDays} check-ins across a 7-day window before reviewing patterns.");
        }

        var sleepTrend = CalculateTrend(window.Select(checkIn => (checkIn.Date, Value: checkIn.SleepQuality)));
        var energyTrend = CalculateTrend(window.Select(checkIn => (checkIn.Date, Value: checkIn.Energy)));
        var recoveryTrend = CalculateTrend(window.Select(checkIn => (checkIn.Date, Value: checkIn.Recovery)));
        var trends = new[] { sleepTrend, energyTrend, recoveryTrend };

        var trendSummary = SummarizeTrends(sleepTrend, energyTrend, recoveryTrend);
        var signalStrength = ResolveSignalStrength(trends);
        var nextStep = ResolveNextStep(trends, signalStrength);

        return new Day7ReviewResult(
            sleepTrend,
            energyTrend,
            recoveryTrend,
            trendSummary,
            signalStrength,
            "unclear",
            nextStep,
            "This review compares simple direction across recent check-ins and stays observational.");
    }

    private static List<CheckIn> GetLatestWindow(IEnumerable<CheckIn> checkIns)
    {
        var ordered = checkIns
            .OrderByDescending(checkIn => checkIn.Date)
            .ToList();

        if (ordered.Count == 0)
        {
            return new List<CheckIn>();
        }

        var latestDay = ordered[0].Date.Date;
        var windowStart = latestDay.AddDays(-(WindowDays - 1));

        return ordered
            .Where(checkIn => checkIn.Date.Date >= windowStart && checkIn.Date.Date <= latestDay)
            .OrderBy(checkIn => checkIn.Date)
            .ToList();
    }

    private static string CalculateTrend(IEnumerable<(DateTime Date, int Value)> observations)
    {
        var ordered = observations.OrderBy(item => item.Date).ToList();
        if (ordered.Count < MinimumCoveredDays)
        {
            return "insufficient_data";
        }

        var midpoint = ordered.Count / 2;
        var earlyAverage = ordered.Take(midpoint).Average(item => item.Value);
        var laterAverage = ordered.Skip(midpoint).Average(item => item.Value);
        var delta = (decimal)(laterAverage - earlyAverage);

        if (delta >= TrendThreshold)
        {
            return "improving";
        }

        if (delta <= -TrendThreshold)
        {
            return "declining";
        }

        return "flat";
    }

    private static string SummarizeTrends(string sleepTrend, string energyTrend, string recoveryTrend)
    {
        var trends = new[] { sleepTrend, energyTrend, recoveryTrend };
        if (trends.Any(trend => trend == "insufficient_data"))
        {
            return "No clear 7-day signal yet.";
        }

        var improving = trends.Count(trend => trend == "improving");
        var declining = trends.Count(trend => trend == "declining");
        var flat = trends.Count(trend => trend == "flat");

        if (improving >= 2 && declining == 0)
        {
            return "Sleep, energy, and recovery are mostly moving upward across recent check-ins.";
        }

        if (declining >= 2 && improving == 0)
        {
            return "Sleep, energy, and recovery are mostly moving downward across recent check-ins.";
        }

        if (flat >= 2 && improving == 0 && declining == 0)
        {
            return "Sleep, energy, and recovery look mostly steady across recent check-ins.";
        }

        return "Recent check-ins show a mixed pattern without one dominant direction.";
    }

    private static string ResolveSignalStrength(IReadOnlyCollection<string> trends)
    {
        if (trends.Any(trend => trend == "insufficient_data"))
        {
            return "weak";
        }

        var improving = trends.Count(trend => trend == "improving");
        var declining = trends.Count(trend => trend == "declining");
        var moving = improving + declining;

        if ((improving >= 2 && declining == 0) || (declining >= 2 && improving == 0))
        {
            return "clear";
        }

        if (moving == 1 || (moving == 2 && improving != declining))
        {
            return "moderate";
        }

        return "weak";
    }

    private static string ResolveNextStep(IReadOnlyCollection<string> trends, string signalStrength)
    {
        if (signalStrength == "weak")
        {
            return "track_longer";
        }

        var improving = trends.Count(trend => trend == "improving");
        var declining = trends.Count(trend => trend == "declining");

        if (declining > improving)
        {
            return "reassess";
        }

        return "continue";
    }
}

public sealed record Day7ReviewResult(
    string SleepTrend,
    string EnergyTrend,
    string RecoveryTrend,
    string TrendSummary,
    string SignalStrength,
    string AlignmentWithExpected,
    string NextStep,
    string ConfidenceNote);
