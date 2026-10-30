namespace BioStack.KnowledgeWorker.Jobs;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Discovery;
using BioStack.KnowledgeWorker.Pipeline;

public interface IDiscoveryJob : IIngestionJob { }

/// <summary>
/// DB-free open discovery (RunMode=Discovery). Queries ClinicalTrials.gov v2, PubMed
/// E-utilities, openFDA Drugs@FDA and bioRxiv/medRxiv for compounds and therapies in the scope
/// taxonomy, matches every name and code name against the corpus, and writes:
/// <list type="bullet">
/// <item>a discovery report (every candidate, its citations, corpus status, queries and errors);</item>
/// <item>a schema-valid research-request batch for new candidates and possible missing aliases;</item>
/// <item>schema-valid draft records for new candidates, which Refresh and CouncilReview pick up.</item>
/// </list>
/// No human approval sits between discovery and the AI council.
/// </summary>
public sealed class DiscoveryJob : IDiscoveryJob
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly WorkerOptions _worker;
    private readonly DiscoveryOptions _options;
    private readonly IResearchArtifactValidator _artifactValidator;
    private readonly ISubstanceRecordValidator _recordValidator;
    private readonly IReadOnlyList<IDiscoverySource> _sources;
    private readonly TimeProvider _clock;
    private readonly IDrugNameVerifier? _nameVerifier;

    public DiscoveryJob(
        WorkerOptions worker,
        DiscoveryOptions options,
        IResearchArtifactValidator artifactValidator,
        ISubstanceRecordValidator recordValidator,
        IReadOnlyList<IDiscoverySource> sources,
        TimeProvider? clock = null,
        IDrugNameVerifier? nameVerifier = null)
    {
        _nameVerifier = nameVerifier;
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _artifactValidator = artifactValidator ?? throw new ArgumentNullException(nameof(artifactValidator));
        _recordValidator = recordValidator ?? throw new ArgumentNullException(nameof(recordValidator));
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        var seedPath = Resolve(_worker.SeedFilePath);
        if (!File.Exists(seedPath)) return JobRunResult.Failure($"Seed not found at '{seedPath}'.");
        var scopePath = Resolve(_options.ScopePath);
        if (!File.Exists(scopePath)) return JobRunResult.Failure($"Discovery scope not found at '{scopePath}'.");

        var seed = JsonNode.Parse(await File.ReadAllTextAsync(seedPath, cancellationToken)) as JsonArray
                   ?? throw new InvalidOperationException($"Expected seed JSON array at '{seedPath}'.");
        var corpus = DiscoveredRecordStore.Combine(seed, DiscoveredDirectories(_worker));
        var scope = DiscoveryScope.Load(scopePath);
        var now = _clock.GetUtcNow();

        var output = await new DiscoveryRunner(_sources, _options, _nameVerifier).RunAsync(scope, corpus, now, cancellationToken);
        foreach (var r in output.SourceResults)
        {
            context.Logger.LogInformation("[Discovery] {Source}: {Queries} request(s), {Hits} hit(s), {Errors} error(s).",
                r.SourceId, r.Queries.Count, r.Hits.Count, r.Errors.Count);
            foreach (var e in r.Errors) context.Logger.LogWarning("[Discovery] {Source}: {Error}", r.SourceId, e);
        }
        for (var i = 0; i < output.Candidates.Count; i++) context.IncrementScanned();

        if (output.ResearchRequestBatch is not null)
        {
            var validation = _artifactValidator.Validate(ResearchArtifactKind.ResearchRequestBatch, output.ResearchRequestBatch);
            if (!validation.IsValid)
                return JobRunResult.Failure("Discovery research-request batch failed schema validation: "
                                            + string.Join("; ", validation.Errors.Select(e => e.ToString())));
        }

        var drafts = new JsonArray();
        foreach (var draft in output.DraftRecords.OfType<JsonObject>())
        {
            var result = _recordValidator.Validate(draft);
            if (result.IsValid) drafts.Add(draft.DeepClone());
            else
            {
                context.IncrementFailed();
                context.Logger.LogWarning("[Discovery] Draft for {Name} failed schema validation: {Summary}",
                    draft["identity"]?["canonicalName"]?.GetValue<string>(), result.Summary());
            }
        }
        for (var i = 0; i < drafts.Count; i++) context.IncrementCreated();

        var stamp = now.UtcDateTime.ToString("yyyyMMdd'T'HHmm'Z'", CultureInfo.InvariantCulture);
        var report = BuildReport(output, now, drafts.Count);
        if (context.DryRun)
        {
            context.Logger.LogInformation("[Discovery] DryRun — {New} new candidate(s), {Drafts} draft(s); nothing written.",
                output.Candidates.Count(c => c.Status == CorpusMatchStatus.New), drafts.Count);
            return JobRunResult.FromContext(context);
        }

        var outDir = Resolve(_options.OutputDirectory);
        Directory.CreateDirectory(outDir);
        await File.WriteAllTextAsync(Path.Combine(outDir, $"discovery-report-{stamp}.json"), report.ToJsonString(JsonOptions), cancellationToken);

        if (output.ResearchRequestBatch is not null)
        {
            var reqDir = Resolve(_worker.ResearchPlanningOutputDirectory);
            Directory.CreateDirectory(reqDir);
            await File.WriteAllTextAsync(Path.Combine(reqDir, $"research-request-discovery-{stamp}.json"),
                output.ResearchRequestBatch.ToJsonString(JsonOptions), cancellationToken);
        }

        if (drafts.Count > 0)
        {
            var draftDir = Resolve(_worker.DiscoveredRecordDirectory ?? "research/discovered-records");
            Directory.CreateDirectory(draftDir);
            await File.WriteAllTextAsync(Path.Combine(draftDir, $"discovered-{stamp}.json"), drafts.ToJsonString(JsonOptions), cancellationToken);
        }

        context.Logger.LogInformation("[Discovery] Wrote report, {Requests} research request(s) and {Drafts} draft record(s) at {Now}.",
            (output.ResearchRequestBatch?["requests"] as JsonArray)?.Count ?? 0, drafts.Count, now.ToString("O", CultureInfo.InvariantCulture));
        return JobRunResult.FromContext(context);
    }

    public static JsonObject BuildReport(DiscoveryRunOutput output, DateTimeOffset now, int draftsWritten)
    {
        JsonArray Arr(IEnumerable<string> s) => new(s.Select(x => (JsonNode?)x).ToArray());
        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "discovery-report",
            ["generatedAt"] = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            ["summary"] = new JsonObject
            {
                ["candidates"] = output.Candidates.Count,
                ["new"] = output.Candidates.Count(c => c.Status == CorpusMatchStatus.New),
                ["possibleAlias"] = output.Candidates.Count(c => c.Status == CorpusMatchStatus.PossibleAlias),
                ["inCorpus"] = output.Candidates.Count(c => c.Status == CorpusMatchStatus.InCorpus),
                ["draftsWritten"] = draftsWritten,
                ["screenedOut"] = output.Screening?.Count(n => n.Action == "rejected") ?? 0,
                ["flagged"] = output.Screening?.Count(n => n.Action == "flagged") ?? 0,
                ["renamedOrMerged"] = output.Screening?.Count(n => n.Action is "renamed" or "merged") ?? 0,
            },
            // Names that were dropped (procedure, ATC code, excipient code), flagged for a name check, or corrected
            // to the RxNorm spelling, with the reason. Nothing here becomes a public record on its own.
            ["screening"] = new JsonArray((output.Screening ?? Array.Empty<ScreeningNote>()).Select(n => (JsonNode?)new JsonObject
            {
                ["name"] = n.Name,
                ["action"] = n.Action,
                ["code"] = n.Code,
                ["reason"] = n.Reason,
                ["replacement"] = n.Replacement,
            }).ToArray()),
            ["sources"] = new JsonArray(output.SourceResults.Select(r => (JsonNode?)new JsonObject
            {
                ["sourceId"] = r.SourceId,
                ["requests"] = r.Queries.Count,
                ["hits"] = r.Hits.Count,
                ["queries"] = Arr(r.Queries.Select(DiscoveryHttp.Redact)),
                ["errors"] = Arr(r.Errors),
            }).ToArray()),
            ["candidates"] = new JsonArray(output.Candidates.Select(c => (JsonNode?)new JsonObject
            {
                ["name"] = c.Name,
                ["status"] = c.Status.ToString(),
                ["matchedRecord"] = c.MatchedRecord,
                ["classification"] = c.Classification,
                ["score"] = c.Score,
                ["aliases"] = Arr(c.Aliases),
                ["areas"] = Arr(c.AreaIds),
                ["phases"] = Arr(c.Phases),
                ["sponsors"] = Arr(c.Sponsors),
                ["evidence"] = new JsonArray(c.Evidence.Select(e => (JsonNode?)new JsonObject
                {
                    ["sourceId"] = e.SourceId,
                    ["identifier"] = e.Identifier,
                    ["title"] = e.Title,
                    ["url"] = e.Url,
                    ["date"] = e.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                }).ToArray()),
            }).ToArray()),
        };
    }

    public static IEnumerable<string> DiscoveredDirectories(WorkerOptions worker)
    {
        if (!string.IsNullOrWhiteSpace(worker.CuratedDiscoveredRecordDirectory)) yield return Resolve(worker.CuratedDiscoveredRecordDirectory!);
        if (!string.IsNullOrWhiteSpace(worker.DiscoveredRecordDirectory)) yield return Resolve(worker.DiscoveredRecordDirectory!);
    }

    private static string Resolve(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}
