namespace BioStack.KnowledgeWorker.Discovery;

/// <summary>Why a candidate was dropped, renamed or flagged by screening. Written to the discovery report.</summary>
public sealed record ScreeningNote(string Name, string Action, string Code, string Reason, string? Replacement = null);

public sealed record ScreeningResult(IReadOnlyList<DiscoveredCandidate> Kept, IReadOnlyList<ScreeningNote> Notes);

/// <summary>
/// Applies <see cref="CandidateScreening"/> and the RxNorm spelling check to aggregated candidates before drafts
/// and research requests are built. Order: clean the name, reject certain non-substances, check the spelling of
/// candidates that would become drafts (a misspelling is corrected to the reference spelling, keeps the typed
/// form as an alias, and is merged into an existing record if that spelling is already in the corpus), then flag.
/// </summary>
public static class CandidateScreener
{
    public const string RenamedFlag = "name-corrected";
    public const string UnavailableFlag = "name-check-unavailable";

    public static async Task<ScreeningResult> ScreenAsync(
        IReadOnlyList<DiscoveredCandidate> candidates,
        CorpusNameIndex corpus,
        IDrugNameVerifier? verifier,
        int minScore,
        int maxNameChecks,
        CancellationToken ct)
    {
        var kept = new List<DiscoveredCandidate>();
        var notes = new List<ScreeningNote>();
        var checks = 0;

        foreach (var original in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var candidate = original;

            var cleaned = CandidateScreening.CleanName(candidate.Name);
            if (cleaned.Length > 0 && !string.Equals(cleaned, candidate.Name, StringComparison.Ordinal))
            {
                var aliases = candidate.Aliases;
                if (CandidateNames.Normalize(cleaned) != CandidateNames.Normalize(candidate.Name) && !aliases.Contains(candidate.Name, StringComparer.OrdinalIgnoreCase))
                    aliases = aliases.Append(candidate.Name).ToList();
                candidate = candidate with { Name = cleaned, Aliases = aliases };
            }

            var screen = CandidateScreening.Screen(candidate.Name);
            if (screen.Verdict == ScreenVerdict.Reject)
            {
                notes.Add(new ScreeningNote(original.Name, "rejected", screen.Code!, screen.Reason!));
                continue;
            }

            var flags = new List<string>(candidate.NameFlags ?? Array.Empty<string>());
            if (screen.Verdict == ScreenVerdict.Flag)
            {
                flags.Add($"name-check:{screen.Code}");
                notes.Add(new ScreeningNote(original.Name, "flagged", screen.Code!, screen.Reason!));
            }

            var wouldDraft = candidate.Status == CorpusMatchStatus.New && candidate.Score >= minScore;
            if (verifier is not null && wouldDraft && checks < Math.Max(0, maxNameChecks) && RxNormNameVerifier.IsCheckable(candidate.Name))
            {
                checks++;
                var check = await verifier.CheckAsync(candidate.Name, ct);
                switch (check.Outcome)
                {
                    case NameCheckOutcome.Misspelled when !string.IsNullOrWhiteSpace(check.Suggestion):
                    {
                        var corrected = check.Suggestion!;
                        var existing = corpus.FindRecord(new[] { corrected });
                        var aliases = candidate.Aliases.Where(a => CandidateNames.Normalize(a) != CandidateNames.Normalize(corrected)).ToList();
                        if (!aliases.Contains(candidate.Name, StringComparer.OrdinalIgnoreCase)) aliases.Add(candidate.Name);
                        notes.Add(new ScreeningNote(original.Name, existing is null ? "renamed" : "merged", "misspelling",
                            $"RxNorm does not know \"{candidate.Name}\" but suggests \"{corrected}\" (edit distance {CandidateScreening.EditDistance(candidate.Name, corrected)}).",
                            existing ?? corrected));
                        candidate = existing is null
                            ? candidate with { Name = corrected, Aliases = aliases, NameFlags = flags.Append($"{RenamedFlag}:rxnorm").ToList() }
                            : candidate with { Name = corrected, Aliases = aliases, Status = CorpusMatchStatus.InCorpus, MatchedRecord = existing, NameFlags = flags };
                        kept.Add(candidate);
                        continue;
                    }
                    case NameCheckOutcome.Unavailable:
                        flags.Add(UnavailableFlag);
                        break;
                }
            }

            kept.Add(flags.Count == 0 ? candidate : candidate with { NameFlags = flags });
        }

        return new ScreeningResult(kept, notes);
    }
}
