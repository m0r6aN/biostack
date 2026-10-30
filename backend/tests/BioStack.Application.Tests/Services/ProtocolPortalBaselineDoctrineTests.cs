namespace BioStack.Application.Tests.Services;

using System.Text.RegularExpressions;
using BioStack.Application.Services;
using Xunit;

/// <summary>
/// Standing rule: BioStack never suggests, prefills or computes a dose, and never tells anyone to change
/// an amount. The Protocol Portal baseline is BioStack-authored copy shown to every user, so none of it may
/// carry dose direction ("reduce the dose", "increase L-Carnitine", "cut by 50%", "titrate", "taper").
/// Observational wording such as "record any changes and review them with your clinician" is what belongs here.
/// </summary>
public sealed class ProtocolPortalBaselineDoctrineTests
{
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    /// <summary>Phrases that point a change in a dose, amount or schedule at the reader.</summary>
    public static readonly IReadOnlyList<Regex> DoseDirectionPhrases =
    [
        new(@"\b(reduc(e|ing)|increas(e|ing)|lower(ing)?|rais(e|ing)|decreas(e|ing)|cut(ting)?|halv(e|ing)|doubl(e|ing)|adjust(ing)?|titrat(e|ing)|taper(ing)?|escalat(e|ing)|skip(ping)?|split(ting)?)\b[^.]{0,40}\b(dose|doses|dosage|dosing|amount|intake)\b", Opts),
        new(@"\b(dose|dosage|dosing)\s+(reduction|increase|adjustment|change|escalation)\b", Opts),
        new(@"\b(cut|reduce|lower|increase|raise)\b[^.]{0,40}\bby\s+\d+\s*%", Opts),
        new(@"\b(increase|raise|up)\b[^.]{0,30}\bto\s+\d+(\.\d+)?\s*(mg|mcg|g|iu|ml|units?)\b", Opts),
        new(@"\b(titrat\w*|taper\w*)\b", Opts),
        new(@"\b\d+\s*weeks?\s+on\b", Opts),
        new(@"\b(on|off)[- ]cycle\b", Opts),
    ];

    private static IEnumerable<string> UserFacingText()
    {
        var baseline = new ProtocolPortalBaseline();
        yield return baseline.Diet.Title;
        yield return baseline.Diet.Summary;
        yield return baseline.Diet.Rationale;
        foreach (var target in baseline.Diet.Targets) { yield return target.Label; yield return target.Value; }
        foreach (var line in baseline.Diet.Lifestyle) yield return line;

        yield return baseline.Monitoring.BaselineCompleted;
        yield return baseline.Monitoring.RecurringCadence;
        foreach (var lab in baseline.Monitoring.RecurringLabs) yield return lab;
        foreach (var rule in baseline.Monitoring.AdjustmentRules) { yield return rule.Trigger; yield return rule.Action; }

        foreach (var milestone in baseline.Milestones) { yield return milestone.Period; yield return milestone.Detail; }
        foreach (var resource in baseline.Resources) { yield return resource.Heading; yield return resource.Body; }
    }

    [Fact]
    public void Baseline_copy_carries_no_dose_direction()
    {
        var offenders = UserFacingText()
            .SelectMany(text => DoseDirectionPhrases.Where(p => p.IsMatch(text)).Select(p => $"\"{text}\" matches {p}"))
            .ToList();

        Assert.True(offenders.Count == 0, "Dose-direction wording in Protocol Portal baseline:\n" + string.Join("\n", offenders));
    }

    [Theory]
    [InlineData("Discuss reducing the primary compound dose and reassess in 5–6 weeks.")]
    [InlineData("Discuss increasing L-Carnitine and protein, and reducing the primary compound dose.")]
    [InlineData("Cut Retatrutide dose by 50%. Reassess in 5–6 weeks.")]
    [InlineData("Increase L-Carnitine to 4 g + protein. Cut Retatrutide by 50%.")]
    [InlineData("Titrate up slowly.")]
    [InlineData("8 weeks on, 8 weeks off")]
    public void Dose_direction_patterns_catch_the_wording_that_was_removed(string text)
        => Assert.Contains(DoseDirectionPhrases, p => p.IsMatch(text));

    [Theory]
    [InlineData("Record any changes and review them with your clinician. Compare with your next scheduled labs in 5–6 weeks.")]
    [InlineData("Record any changes, along with your diet and supplement log, and review them with your clinician.")]
    public void Observational_wording_is_allowed(string text)
        => Assert.DoesNotContain(DoseDirectionPhrases, p => p.IsMatch(text));

    [Fact]
    public void Monitoring_triggers_keep_their_labs_and_each_rule_points_to_a_clinician()
    {
        var rules = new ProtocolPortalBaseline().Monitoring.AdjustmentRules;
        Assert.Equal(2, rules.Count);
        Assert.All(rules, rule => Assert.Contains("clinician", rule.Action, StringComparison.OrdinalIgnoreCase));
    }
}
