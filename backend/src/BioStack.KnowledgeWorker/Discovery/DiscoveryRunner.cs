namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;

/// <summary>Runs every enabled source, aggregates, and builds the run's outputs. No I/O beyond the sources.</summary>
public sealed class DiscoveryRunner
{
    private readonly IReadOnlyList<IDiscoverySource> _sources;
    private readonly DiscoveryOptions _options;

    public DiscoveryRunner(IReadOnlyList<IDiscoverySource> sources, DiscoveryOptions options)
    {
        _sources = sources;
        _options = options;
    }

    public async Task<DiscoveryRunOutput> RunAsync(DiscoveryScope scope, JsonArray corpus, DateTimeOffset now, CancellationToken ct)
    {
        var to = DateOnly.FromDateTime(now.UtcDateTime);
        var window = new DiscoveryWindow(to.AddDays(-Math.Max(1, _options.LookbackDays)), to);
        var preprintWindow = new DiscoveryWindow(to.AddDays(-Math.Max(1, _options.PreprintLookbackDays)), to);

        var results = new List<DiscoverySourceResult>();
        foreach (var source in _sources)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                results.Add(await source.DiscoverAsync(scope, source.SourceId == PreprintDiscoverySource.Id ? preprintWindow : window, ct));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new DiscoverySourceResult(source.SourceId, Array.Empty<DiscoveryHit>(), Array.Empty<string>(), new[] { ex.Message }));
            }
        }

        var index = CorpusNameIndex.Build(corpus);
        var candidates = DiscoveryAggregator.Aggregate(results.SelectMany(r => r.Hits), index, scope.CodeNamePrefixDenylist);
        var requests = DiscoveryOutputBuilder.BuildResearchRequests(candidates, now, _options.MinScore, _options.MaxCandidatesPerRun);
        var drafts = DiscoveryOutputBuilder.BuildDraftRecords(candidates, now, _options.MinScore, _options.MaxCandidatesPerRun);
        return new DiscoveryRunOutput(candidates, results, requests, drafts);
    }

    public static IReadOnlyList<IDiscoverySource> CreateSources(DiscoveryOptions options, Func<HttpClient> clientFactory)
    {
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, options.RequestDelayMilliseconds));
        var ncbiDelay = string.IsNullOrWhiteSpace(options.NcbiApiKey) ? TimeSpan.FromMilliseconds(Math.Max(options.RequestDelayMilliseconds, 350)) : TimeSpan.FromMilliseconds(120);
        if (options.RequestDelayMilliseconds == 0) ncbiDelay = TimeSpan.Zero;
        var sources = new List<IDiscoverySource>();
        if (options.ClinicalTrialsEnabled) sources.Add(new ClinicalTrialsGovDiscoverySource(new DiscoveryHttp(clientFactory(), delay, options.UserAgent), options));
        if (options.PubMedEnabled) sources.Add(new PubMedDiscoverySource(new DiscoveryHttp(clientFactory(), ncbiDelay, options.UserAgent), options));
        if (options.OpenFdaEnabled) sources.Add(new OpenFdaDiscoverySource(new DiscoveryHttp(clientFactory(), delay, options.UserAgent), options));
        if (options.PreprintsEnabled) sources.Add(new PreprintDiscoverySource(new DiscoveryHttp(clientFactory(), delay, options.UserAgent), options));
        return sources;
    }
}
