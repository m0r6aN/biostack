namespace BioStack.KnowledgeWorker.Discovery;

using System.Text;
using System.Text.RegularExpressions;

public enum ScreenVerdict
{
    /// <summary>Looks like a substance, therapy or product name.</summary>
    Accept,
    /// <summary>Might be a substance but might not be (a material, a generic class word, a very long phrase). A draft is still made, marked for a name check.</summary>
    Flag,
    /// <summary>Certainly not a substance: a procedure, a classification code, a known excipient code, or an empty name. No draft is made.</summary>
    Reject,
}

public sealed record ScreenResult(ScreenVerdict Verdict, string? Code = null, string? Reason = null)
{
    public static readonly ScreenResult Ok = new(ScreenVerdict.Accept);
}

/// <summary>
/// Offline, deterministic junk screening for discovery names (owner direction t101u, 2026-10-30). Discovery
/// pulls names out of trial interventions and paper titles, so it also picks up procedures ("blood sampling"),
/// ATC classification codes ("L01A") and formulation excipients ("F-127"). Only a name that is certainly not a
/// substance is rejected; anything uncertain is flagged for the council to look at instead (prefer a flag to a
/// hard reject). Oncology is deliberately NOT screened: a person with cancer may track any drug, including
/// rituximab, so oncology drugs go through council review like every other draft.
/// </summary>
public static partial class CandidateScreening
{
    /// <summary>First letters of the 14 ATC anatomical main groups.</summary>
    private const string AtcMainGroups = "ABCDGHJLMNPRSV";

    // ATC level 3-5 shapes: L01A, L01AA, L01AA01 (one letter, two digits, one or two letters, optional two digits).
    [GeneratedRegex(@"^[A-Z]\d{2}[A-Z]{1,2}(\d{2})?$")]
    private static partial Regex AtcCodeRegex();

    [GeneratedRegex(@"^[\p{P}\p{S}\s]+|[\p{P}\p{S}\s]+$")]
    private static partial Regex EdgePunctuationRegex();

    /// <summary>Procedures, diagnostics, devices, behaviours and trial-arm words. Whole-word, case-insensitive.</summary>
    private static readonly string[] NonSubstancePhrases =
    [
        "blood sampling", "blood draw", "blood collection", "venipuncture", "biopsy", "biopsies", "surgery", "surgical",
        "radiotherapy", "radiation therapy", "physiotherapy", "physical therapy", "acupuncture", "counseling", "counselling",
        "questionnaire", "education", "training program", "ultrasound", "imaging", "screening", "assessment", "monitoring",
        "observation", "standard of care", "usual care", "placebo", "sham", "control group", "exercise", "diet", "lifestyle",
        "sampling", "catheter", "stent", "implant", "device", "mri", "ct scan", "echocardiography", "ecg", "endoscopy",
        "no intervention", "waitlist", "wait list",
    ];

    /// <summary>Formulation excipients and codes that are certainly not the product being studied.</summary>
    private static readonly string[] ExcipientRejects =
    [
        "f-127", "f127", "pluronic", "pluronic f-127", "pluronic f127", "hpmc", "carboxymethylcellulose",
        "magnesium stearate", "microcrystalline cellulose",
    ];

    /// <summary>Whole name equals one of these: usually an excipient or a class word, but several are also real drugs, so flagged and never rejected.</summary>
    private static readonly string[] FlagExactNames =
    [
        "pvp", "cmc", "dmso", "mannitol", "lactose", "polysorbate 80", "tween 80",
        "postbiotic", "probiotic", "prebiotic", "synbiotic", "supplement", "vitamin", "herbal", "extract", "formula", "complex",
    ];

    /// <summary>Materials and scaffolds that may or may not be a substance: flagged when contained in the name, never rejected.</summary>
    private static readonly string[] FlagTerms =
    [
        "poloxamer", "carbomer", "methylcellulose", "cellulose", "hydrogel", "scaffold", "matrigel", "rada16",
        "vehicle", "excipient", "buffer", "emulsion",
    ];

    private static readonly Regex NonSubstanceRegex = BuildWordRegex(NonSubstancePhrases);
    private static readonly Regex FlagRegex = BuildWordRegex(FlagTerms);

    private static Regex BuildWordRegex(IEnumerable<string> phrases)
        => new(@"(?<![\p{L}\p{N}])(" + string.Join("|", phrases.OrderByDescending(p => p.Length).Select(Regex.Escape)) + @")(?![\p{L}\p{N}])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Display cleanup: trademark symbols and edge punctuation removed, whitespace collapsed
    /// ("EYELEA ®" becomes "EYELEA"). Never changes the letters.
    /// </summary>
    public static string CleanName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var sb = new StringBuilder(name.Length);
        foreach (var c in name) if (c is not ('®' or '™' or '©' or '℠')) sb.Append(c);
        var collapsed = Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        return EdgePunctuationRegex().Replace(collapsed, string.Empty);
    }

    public static ScreenResult Screen(string? name)
    {
        var cleaned = CleanName(name);
        var key = CandidateNames.Normalize(cleaned);
        if (key.Length < 3) return new ScreenResult(ScreenVerdict.Reject, "too-short", "The name has fewer than three letters or digits.");
        if (key.All(char.IsDigit)) return new ScreenResult(ScreenVerdict.Reject, "number-only", "The name is only digits.");

        var compact = cleaned.Replace(" ", string.Empty);
        if (AtcCodeRegex().IsMatch(compact) && AtcMainGroups.Contains(compact[0]))
            return new ScreenResult(ScreenVerdict.Reject, "atc-code", "The name has the shape of an ATC classification code, which names a drug class and not a substance.");

        if (ExcipientRejects.Contains(cleaned, StringComparer.OrdinalIgnoreCase) || ExcipientRejects.Contains(cleaned.Replace(" ", string.Empty), StringComparer.OrdinalIgnoreCase))
            return new ScreenResult(ScreenVerdict.Reject, "excipient-code", "The name is a formulation excipient or excipient code, not the product being studied.");

        var nonSubstance = NonSubstanceRegex.Match(cleaned);
        if (nonSubstance.Success)
            return new ScreenResult(ScreenVerdict.Reject, "non-substance", $"The name contains \"{nonSubstance.Value}\", which describes a procedure, device, diagnostic or trial arm and not a substance.");

        if (FlagExactNames.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
            return new ScreenResult(ScreenVerdict.Flag, "material-or-class-term", $"\"{cleaned}\" is an excipient, a generic class word or an ambiguous name. Check that it names a real substance before it is graded.");

        var flag = FlagRegex.Match(cleaned);
        if (flag.Success)
            return new ScreenResult(ScreenVerdict.Flag, "material-or-class-term", $"The name contains \"{flag.Value}\", which may be a material or scaffold. Check that it names a real substance before it is graded.");

        if (cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 7)
            return new ScreenResult(ScreenVerdict.Flag, "long-phrase", "The name is a long phrase and may be a study descriptor rather than a substance.");

        return ScreenResult.Ok;
    }

    /// <summary>Damerau-Levenshtein (optimal string alignment) distance, case-insensitive.</summary>
    public static int EditDistance(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }
        return d[a.Length, b.Length];
    }
}
