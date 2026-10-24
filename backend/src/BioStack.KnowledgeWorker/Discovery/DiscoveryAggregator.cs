namespace BioStack.KnowledgeWorker.Discovery;

/// <summary>
/// Merges raw hits into candidates (by any shared normalized name or alias), scores them by
/// independent source support, and checks each against the corpus.
/// </summary>
public static class DiscoveryAggregator
{
    public static readonly IReadOnlyDictionary<string, int> SourceWeights = new Dictionary<string, int>
    {
        [ClinicalTrialsGovDiscoverySource.Id] = 3,
        [OpenFdaDiscoverySource.Id] = 3,
        [PubMedDiscoverySource.Id] = 2,
        [PreprintDiscoverySource.Id] = 1,
    };

    private static readonly string[] GenericClasses = ["Research Compound", "Other", "Pharmaceutical"];

    public static IReadOnlyList<DiscoveredCandidate> Aggregate(IEnumerable<DiscoveryHit> hits, CorpusNameIndex corpus, IReadOnlyCollection<string> codePrefixDenylist)
    {
        var groups = new List<List<DiscoveryHit>>();
        var keyToGroup = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var hit in hits)
        {
            var keys = new[] { hit.Name }.Concat(hit.Aliases).Select(CandidateNames.Normalize).Where(k => k.Length >= 3).Distinct().ToList();
            if (keys.Count == 0) continue;
            var matches = keys.Where(keyToGroup.ContainsKey).Select(k => keyToGroup[k]).Distinct().OrderBy(i => i).ToList();
            int target;
            if (matches.Count == 0)
            {
                groups.Add(new List<DiscoveryHit>());
                target = groups.Count - 1;
            }
            else
            {
                target = matches[0];
                foreach (var other in matches.Skip(1))
                {
                    groups[target].AddRange(groups[other]);
                    groups[other].Clear();
                    foreach (var k in keyToGroup.Where(kv => kv.Value == other).Select(kv => kv.Key).ToList()) keyToGroup[k] = target;
                }
            }
            groups[target].Add(hit);
            foreach (var k in keys) keyToGroup[k] = target;
        }

        var candidates = new List<DiscoveredCandidate>();
        foreach (var group in groups.Where(g => g.Count > 0))
        {
            var allNames = group.Select(h => h.Name).Concat(group.SelectMany(h => h.Aliases)).ToList();
            // Prefer a real nonproprietary (INN-like) name, then the most-cited name.
            var name = group.Select(h => h.Name)
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => !CandidateNames.IsCodeName(g.Key, codePrefixDenylist) && !g.Key.Contains(' '))
                .ThenByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.Ordinal)
                .First().Key;
            var aliases = allNames
                .Where(a => CandidateNames.Normalize(a) != CandidateNames.Normalize(name))
                .GroupBy(CandidateNames.Normalize).Select(g => g.First())
                .OrderBy(a => a, StringComparer.Ordinal)
                .ToList();
            var classification = group.Select(h => h.Classification)
                .GroupBy(c => c)
                .OrderByDescending(g => !GenericClasses.Contains(g.Key))
                .ThenByDescending(g => g.Count())
                .First().Key;
            var evidence = group.Select(h => h.Evidence)
                .GroupBy(e => (e.SourceId, e.Identifier)).Select(g => g.First())
                .OrderByDescending(e => e.Date).ToList();
            var sources = evidence.Select(e => e.SourceId).Distinct().ToList();
            var score = sources.Sum(s => SourceWeights.TryGetValue(s, out var w) ? w : 1) + (evidence.Count >= 3 ? 1 : 0);

            var record = corpus.FindRecord(allNames);
            var status = record is not null ? CorpusMatchStatus.InCorpus : CorpusMatchStatus.New;
            if (record is null && (record = corpus.FindPossibleAliasOwner(allNames)) is not null) status = CorpusMatchStatus.PossibleAlias;

            candidates.Add(new DiscoveredCandidate(
                name, aliases, classification,
                group.Select(h => h.AreaId).Distinct().ToList(),
                evidence,
                group.Select(h => h.Phase).Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).Distinct().ToList(),
                group.Select(h => h.Sponsor).Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).Distinct().ToList(),
                group.SelectMany(h => h.Conditions ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList(),
                score, status, record));
        }

        return candidates
            .OrderByDescending(c => c.Status == CorpusMatchStatus.New)
            .ThenByDescending(c => c.Score)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();
    }
}
