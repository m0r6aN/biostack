namespace BioStack.KnowledgeWorker.Pipeline;

public static class RecommendedOfficialSourcePlanningAdapters
{
    public static IReadOnlyList<string> SourceIds { get; } =
    [
        "fda",
        "pubchem",
        "pubmed",
        "clinicaltrials",
        "dailymed",
        "nih-ods",
        "nih-nccih",
    ];

    public static IReadOnlyList<ISourcePlanningAdapter> All { get; } = SourceIds
        .Select(sourceId => (ISourcePlanningAdapter)new PlanningOnlyAdapter(sourceId))
        .ToList();

    private sealed class PlanningOnlyAdapter(string sourceId) : ISourcePlanningAdapter
    {
        public string SourceId { get; } = sourceId;

        public SourcePlanningAdapterResult Plan(SourceAcquisitionTarget target)
        {
            if (target is null) throw new ArgumentNullException(nameof(target));

            var terms = new[] { target.CompoundName }
                .Concat(target.Aliases)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new SourcePlanningAdapterResult(
                AdapterId: $"{SourceId}-planning-v1",
                SearchTerms: terms);
        }
    }
}
