namespace BioStack.KnowledgeWorker.Pipeline;

public sealed record ResearchTaskQueue(
    string QueueVersion,
    DateTimeOffset GeneratedAtUtc,
    ResearchTaskQueueCounts Counts,
    IReadOnlyList<ResearchTaskQueueItem> Items,
    IReadOnlyList<ResearchTaskQueueResolvedItem> ResolvedItems);

public sealed record ResearchTaskQueueCounts(
    int TotalItems,
    int Urgent,
    int High,
    int Normal,
    int Low,
    int ResolvedItems);

public sealed record ResearchTaskQueueItem(
    string TaskId,
    string TaskType,
    string CompoundName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Categories,
    string Classification,
    string Priority,
    IReadOnlyList<string> RequestIds,
    IReadOnlyList<string> RequesterIds,
    DateTimeOffset FirstRequestedAtUtc,
    DateTimeOffset LatestRequestedAtUtc,
    IReadOnlyList<string> Rationales,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> SuggestedResearchDirectives,
    string TargetEvidencePath,
    string RequiredSchema);

public sealed record ResearchTaskQueueResolvedItem(
    string TaskId,
    string CompoundName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> Categories,
    string Classification,
    string Priority,
    IReadOnlyList<string> RequestIds,
    IReadOnlyList<string> RequesterIds,
    DateTimeOffset FirstRequestedAtUtc,
    DateTimeOffset LatestRequestedAtUtc,
    DateTimeOffset ResolvedAtUtc,
    string CurrentReadiness,
    string Resolution,
    string ResolutionReason,
    string TargetEvidencePath);

public interface IResearchTaskQueueBuilder
{
    ResearchTaskQueue Build(ResearchSummary summary, ResearchRequestIndex researchRequests, string targetEvidenceDirectory);
}

public sealed class ResearchTaskQueueBuilder : IResearchTaskQueueBuilder
{
    private const string DefaultTargetEvidenceDirectory = "research/input/evidence";
    private const string EvidencePacketSchema = "evidence-packet.schema.json";

    public ResearchTaskQueue Build(ResearchSummary summary, ResearchRequestIndex researchRequests, string targetEvidenceDirectory)
    {
        if (summary is null) throw new ArgumentNullException(nameof(summary));
        if (researchRequests is null) throw new ArgumentNullException(nameof(researchRequests));

        var generatedAtUtc = DateTimeOffset.UtcNow;
        var resolvedTargetDirectory = NormalizePath(string.IsNullOrWhiteSpace(targetEvidenceDirectory)
            ? DefaultTargetEvidenceDirectory
            : targetEvidenceDirectory);
        var compoundsByName = summary.Compounds.ToDictionary(compound => compound.Name, StringComparer.OrdinalIgnoreCase);

        var items = summary.Compounds
            .Where(compound => compound.PromotionReadiness.Equals("research-requested", StringComparison.OrdinalIgnoreCase))
            .Where(compound => compound.HasResearchRequest)
            .Select(compound => ToItem(compound, researchRequests.ForCompound(compound.Name), resolvedTargetDirectory))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => PriorityRank(item.Priority))
            .ThenByDescending(item => item.LatestRequestedAtUtc)
            .ThenBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resolvedItems = researchRequests.All()
            .GroupBy(request => request.CompoundName, StringComparer.OrdinalIgnoreCase)
            .Select(group => ToResolvedItem(group.Key, group.ToList(), compoundsByName, generatedAtUtc, resolvedTargetDirectory))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.ResolvedAtUtc)
            .ThenBy(item => item.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResearchTaskQueue(
            QueueVersion: "1.0.0",
            GeneratedAtUtc: generatedAtUtc,
            Counts: new ResearchTaskQueueCounts(
                TotalItems: items.Count,
                Urgent: items.Count(item => item.Priority.Equals("urgent", StringComparison.OrdinalIgnoreCase)),
                High: items.Count(item => item.Priority.Equals("high", StringComparison.OrdinalIgnoreCase)),
                Normal: items.Count(item => item.Priority.Equals("normal", StringComparison.OrdinalIgnoreCase)),
                Low: items.Count(item => item.Priority.Equals("low", StringComparison.OrdinalIgnoreCase)),
                ResolvedItems: resolvedItems.Count),
            Items: items,
            ResolvedItems: resolvedItems);
    }

    private static ResearchTaskQueueItem? ToItem(
        ResearchSummaryCompound compound,
        IReadOnlyList<ResearchRequestInfo> requests,
        string targetEvidenceDirectory)
    {
        if (requests.Count == 0) return null;

        var slug = SubstanceRecordNormalizer.Slugify(compound.Name);
        if (slug.Length == 0) return null;

        var ordered = requests
            .OrderBy(request => request.RequestedAt)
            .ThenBy(request => request.RequestId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var latest = ordered[^1];
        var priority = ordered
            .Select(request => NormalizePriority(request.Priority))
            .OrderBy(PriorityRank)
            .First();
        var aliases = ordered
            .SelectMany(request => request.Aliases)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categories = AggregateCategories(ordered);
        var requestIds = ordered
            .Select(request => request.RequestId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var requesterIds = ordered
            .Select(request => request.RequesterId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rationales = ordered
            .Select(request => request.Rationale)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var notes = ordered
            .SelectMany(request => request.Notes)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResearchTaskQueueItem(
            TaskId: $"{slug}-initial-research",
            TaskType: "generate-evidence-packet",
            CompoundName: compound.Name,
            Aliases: aliases,
            Categories: categories,
            Classification: compound.Classification,
            Priority: priority,
            RequestIds: requestIds,
            RequesterIds: requesterIds,
            FirstRequestedAtUtc: ordered[0].RequestedAt,
            LatestRequestedAtUtc: latest.RequestedAt,
            Rationales: rationales,
            Notes: notes,
            SuggestedResearchDirectives: SuggestedResearchDirectives(compound.Name, categories, latest.Rationale, notes),
            TargetEvidencePath: NormalizePath(Path.Combine(targetEvidenceDirectory, $"{slug}.evidence.json")),
            RequiredSchema: EvidencePacketSchema);
    }

    private static ResearchTaskQueueResolvedItem? ToResolvedItem(
        string compoundName,
        IReadOnlyList<ResearchRequestInfo> requests,
        IReadOnlyDictionary<string, ResearchSummaryCompound> compoundsByName,
        DateTimeOffset generatedAtUtc,
        string targetEvidenceDirectory)
    {
        if (!compoundsByName.TryGetValue(compoundName, out var compound)) return null;
        if (!compound.HasResearchRequest) return null;
        if (compound.PromotionReadiness.Equals("research-requested", StringComparison.OrdinalIgnoreCase)) return null;

        var ordered = requests
            .OrderBy(request => request.RequestedAt)
            .ThenBy(request => request.RequestId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ordered.Count == 0) return null;

        var slug = SubstanceRecordNormalizer.Slugify(compoundName);
        if (slug.Length == 0) return null;

        var aliases = ordered
            .SelectMany(request => request.Aliases)
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(alias => alias, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categories = AggregateCategories(ordered);
        var requestIds = ordered
            .Select(request => request.RequestId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var priority = ordered
            .Select(request => NormalizePriority(request.Priority))
            .OrderBy(PriorityRank)
            .First();
        var requesterIds = ordered
            .Select(request => request.RequesterId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ResearchTaskQueueResolvedItem(
            TaskId: $"{slug}-initial-research",
            CompoundName: compoundName,
            Aliases: aliases,
            Categories: categories,
            Classification: compound.Classification,
            Priority: priority,
            RequestIds: requestIds,
            RequesterIds: requesterIds,
            FirstRequestedAtUtc: ordered[0].RequestedAt,
            LatestRequestedAtUtc: ordered[^1].RequestedAt,
            ResolvedAtUtc: generatedAtUtc,
            CurrentReadiness: compound.PromotionReadiness,
            Resolution: "evidence-detected",
            ResolutionReason: $"Initial research request consumed on this run because pipeline evidence now exists and the compound moved to '{compound.PromotionReadiness}'.",
            TargetEvidencePath: NormalizePath(Path.Combine(targetEvidenceDirectory, $"{slug}.evidence.json")));
    }

    private static IReadOnlyList<string> SuggestedResearchDirectives(
        string compoundName,
        IReadOnlyList<string> categories,
        string latestRationale,
        IReadOnlyList<string> notes)
    {
        var directives = new List<string>
        {
            $"Generate a compound evidence packet for {compoundName} that matches {EvidencePacketSchema}.",
            "Use research/directives/02-source-registry-agent.md to validate or extend source coverage before claim extraction.",
            "Use research/directives/03-category-evidence-agent.md to extract claim-level evidence while preserving route, formulation, alias, and regulatory distinctions.",
            "Use research/directives/04-preprocess-compile-review.md after packet generation so the next worker pass can preprocess, compile, and review the result.",
            "Do not fabricate evidence; leave unsupported claims out of the packet until a source-backed record exists."
        };

        if (categories.Count > 0)
        {
            directives.Add($"Treat the operator categories as routing hints: {string.Join(", ", categories)}.");
        }

        if (!string.IsNullOrWhiteSpace(latestRationale))
        {
            directives.Add($"Prioritize the operator rationale: {latestRationale.Trim()}");
        }

        directives.AddRange(notes.Select(note => $"Operator note: {note.Trim()}"));

        return directives
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> AggregateCategories(IReadOnlyList<ResearchRequestInfo> requests)
        => requests
            .SelectMany(request => request.Categories)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizePriority(string priority)
        => priority.Trim().ToLowerInvariant() switch
        {
            "urgent" => "urgent",
            "high" => "high",
            "low" => "low",
            _ => "normal"
        };

    private static int PriorityRank(string priority)
        => NormalizePriority(priority) switch
        {
            "urgent" => 0,
            "high" => 1,
            "normal" => 2,
            "low" => 3,
            _ => 2
        };

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimEnd('/');
}