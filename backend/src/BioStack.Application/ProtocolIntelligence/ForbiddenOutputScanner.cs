namespace BioStack.Application.ProtocolIntelligence;

using System.Text.RegularExpressions;

public interface IForbiddenOutputScanner
{
    IReadOnlyList<string> Scan(string? text);
}

public sealed class ForbiddenOutputScanner : IForbiddenOutputScanner
{
    private readonly IProtocolIntelligenceArtifactLoader _loader;

    public ForbiddenOutputScanner(IProtocolIntelligenceArtifactLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<string> Scan(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalized = text.ToLowerInvariant();
        var matches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in Rules())
        {
            if (rule.Pattern.IsMatch(normalized))
            {
                matches.Add(rule.RuleId);
            }
        }

        var knownIds = _loader.Load().AllBlockedOutputs;
        return matches.Where(knownIds.Contains).OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<ForbiddenOutputRule> Rules()
    {
        yield return Rule("recommend_start_stop_taper_combine_or_escalate", "you should start");
        yield return Rule("recommend_start_stop_taper_combine_or_escalate", "you should stop");
        yield return Rule("recommend_start_stop_taper_combine_or_escalate", "run this cycle");
        yield return Rule("recommend_start_stop_taper_combine_or_escalate", @"\btake\b", regex: true);
        yield return Rule("injection_instructions", @"\binject\b", regex: true);
        yield return Rule("post_cycle_therapy_instructions", "post-cycle therapy");
        yield return Rule("post_cycle_therapy_instructions", @"\bpct\b", regex: true);
        yield return Rule("sourcing_guidance", "best source");
        yield return Rule("sourcing_guidance", "source this");
        yield return Rule("claims_investigational_peptides_safe_or_effective", "safe and effective");
        yield return Rule("claims_community_anecdotes_prove_efficacy", "proven by user reports");
    }

    private static ForbiddenOutputRule Rule(string ruleId, string pattern, bool regex = false)
        => new(ruleId, new Regex(regex ? pattern : Regex.Escape(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

    private sealed record ForbiddenOutputRule(string RuleId, Regex Pattern);
}
