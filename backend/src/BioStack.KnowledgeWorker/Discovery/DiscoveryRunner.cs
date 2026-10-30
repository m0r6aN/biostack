namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;

/// <summary>Runs every enabled source, aggregates, and builds the run's outputs. No I/O beyond the sources.</summary>
public sealed class DiscoveryRunner
{
    private readonly IReadOnlyList<IDiscoverySource> _sources;
    private readonly DiscoveryOptions _options;
    private readonly IDrugNameVerifier? _nameVerifier;

    public DiscoveryRunner(IReadOnlyList<IDiscoverySource> sources, DiscoveryOptions options, IDrugNameVerifier? nameVerifier = null)
    {
        _sources = sources;
        _options = options;
        _nameVerifier = options.NameCheckEnabled ? nameVerifier : null;
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
        var aggregated = DiscoveryAggregator.Aggregate(results.SelectMany(r => r.Hits), index, scope.CodeNamePrefixDenylist);
        // Screen out procedures, classification codes and excipient codes, and correct misspellings, before any draft exists.
        var screening = await CandidateScreener.ScreenAsync(aggregated, index, _nameVerifier, _options.MinScore, _options.MaxNameChecksPerRun, ct);
        var candidates = screening.Kept;
        var requests = DiscoveryOutputBuilder.BuildResearchRequests(candidates, now, _options.MinScore, _options.MaxCandidatesPerRun);
        var drafts = DiscoveryOutputBuilder.BuildDraftRecords(candidates, now, _options.MinScore, _options.MaxCandidatesPerRun);
        return new DiscoveryRunOutput(candidates, results, requests, drafts, screening.Notes);
    }

    public static IDrugNameVerifier? CreateNameVerifier(DiscoveryOptions options, Func<HttpClient> clientFactory)
        => options.NameCheckEnabled
            ? new RxNormNameVerifier(new DiscoveryHttp(clientFactory(), TimeSpan.FromMilliseconds(Math.Max(0, options.RequestDelayMilliseconds)), options.UserAgent))
            : null;

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
