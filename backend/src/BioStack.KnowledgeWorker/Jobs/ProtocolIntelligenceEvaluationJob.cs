namespace BioStack.KnowledgeWorker.Jobs;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.Application.ProtocolIntelligence;
using BioStack.KnowledgeWorker.Config;

public interface IProtocolIntelligenceEvaluationJob : IIngestionJob { }

/// <summary>
/// Offline Protocol Intelligence evaluation pass. Loads the canonical
/// <c>research/protocol-intelligence/*.json</c> corpus via <see cref="IProtocolIntelligenceArtifactLoader"/>,
/// exercises the build-time <see cref="IProtocolIntelligenceGate"/>, and emits a deterministic report.
///
/// This job is build-time/offline only: it never writes a database, exposes no runtime surface, and emits
/// no user-facing narrative. Forbidden-output enforcement belongs entirely to the gate's
/// <c>DoctrineSanitizer</c> — there is no parallel scanner or phrase list here.
///
/// Every run does two things:
///  1. Corpus integrity — the seven artifacts must load and parse; each promotion target must declare
///     required fields and a review gate.
///  2. Gate fail-closed conformance — the gate must reject an unknown artifact type. This guarantees the
///     gate is actually invoked and fail-closed on every run, with no external input.
/// When candidate artifacts are supplied (<see cref="WorkerOptions.ProtocolIntelligenceCandidatePath"/> /
/// <see cref="WorkerOptions.ProtocolIntelligenceCandidateDirectory"/>), each is evaluated through the gate;
/// any candidate that cannot promote is a run failure.
/// </summary>
public sealed class ProtocolIntelligenceEvaluationJob : IProtocolIntelligenceEvaluationJob
{
    private const string ConformanceProbeType = "__conformance_probe_unknown_type__";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly WorkerOptions _options;
    private readonly IProtocolIntelligenceArtifactLoader _loader;
    private readonly IProtocolIntelligenceGate _gate;

    public ProtocolIntelligenceEvaluationJob(
        WorkerOptions options,
        IProtocolIntelligenceArtifactLoader loader,
        IProtocolIntelligenceGate gate)
    {
        _options = options;
        _loader = loader;
        _gate = gate;
    }

    public Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        var outputDir = ResolveOutputDirectory(_options.ProtocolIntelligenceEvaluationOutputDirectory);
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, "protocol-intelligence-evaluation-report.json");

        // ── 1. Corpus integrity ──────────────────────────────────────────────────
        ProtocolIntelligenceArtifactSet corpus;
        try
        {
            corpus = _loader.Load();
        }
        catch (Exception ex)
        {
            context.IncrementFailed();
            var failed = new ProtocolIntelligenceEvaluationReport(
                ReportVersion: "1.0.0",
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                CorpusLoaded: false,
                CorpusError: ex.Message,
                ArtifactVersions: new Dictionary<string, string>(),
                PromotionTargets: Array.Empty<PromotionTargetSummary>(),
                StructuralViolations: new[] { $"corpus_load_failed: {ex.Message}" },
                CandidatesEvaluated: 0,
                CandidateResults: Array.Empty<CandidateEvaluationResult>(),
                BlockedCandidates: 0,
                CanPromoteAll: false);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(failed, JsonOptions));
            context.LogSummary("ProtocolIntelligenceEvaluationJob");
            return Task.FromResult(JobRunResult.FromContext(context) with { ErrorMessage = ex.Message });
        }

        var structuralViolations = new List<string>();
        var targets = corpus.PromotionTargets.Values
            .OrderBy(target => target.Id, StringComparer.Ordinal)
            .Select(target =>
            {
                if (target.RequiredFields.Count == 0)
                {
                    structuralViolations.Add($"promotion_target_missing_required_fields: {target.Id}");
                }

                if (string.IsNullOrWhiteSpace(target.ReviewGate))
                {
                    structuralViolations.Add($"promotion_target_missing_review_gate: {target.Id}");
                }

                return new PromotionTargetSummary(
                    Id: target.Id,
                    RequiredFieldCount: target.RequiredFields.Count,
                    ReviewGate: target.ReviewGate,
                    ForbiddenOutputScanRequired: target.ForbiddenOutputScanRequired);
            })
            .ToArray();

        // ── 2. Gate fail-closed conformance ──────────────────────────────────────
        // The gate must reject an unknown artifact type. This invokes the gate every run and
        // proves it is fail-closed without depending on any external candidate input.
        var conformance = _gate.Evaluate(new PromotionGateRequest(ConformanceProbeType, new()));
        if (conformance.CanPromote ||
            !conformance.BlockingReasons.Contains(GateReasons.UnknownArtifactType, StringComparer.Ordinal))
        {
            structuralViolations.Add("gate_not_fail_closed: unknown artifact type was not rejected");
        }

        foreach (var _ in structuralViolations)
        {
            context.IncrementFailed();
        }

        // ── 3. Candidate evaluation (optional, offline JSON input) ───────────────
        var candidateResults = new List<CandidateEvaluationResult>();
        var blockedCandidates = 0;
        foreach (var (source, request) in LoadCandidates())
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.IncrementScanned();

            var result = _gate.Evaluate(request);
            candidateResults.Add(new CandidateEvaluationResult(
                Source: source,
                ArtifactType: request.ArtifactType,
                CanPromote: result.CanPromote,
                BlockingReasons: Ordered(result.BlockingReasons),
                RequiredFieldsMissing: Ordered(result.RequiredFieldsMissing),
                DoctrineViolationFields: Ordered(result.DoctrineViolationFields),
                RequiresHumanReview: result.RequiresHumanReview));

            if (result.CanPromote)
            {
                context.IncrementUnchanged();
            }
            else
            {
                blockedCandidates++;
                context.IncrementFailed();
            }
        }

        candidateResults = candidateResults
            .OrderBy(result => result.Source, StringComparer.Ordinal)
            .ToList();

        var report = new ProtocolIntelligenceEvaluationReport(
            ReportVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            CorpusLoaded: true,
            CorpusError: null,
            ArtifactVersions: new SortedDictionary<string, string>(
                corpus.ArtifactVersions.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal),
            PromotionTargets: targets,
            StructuralViolations: structuralViolations
                .OrderBy(violation => violation, StringComparer.Ordinal)
                .ToArray(),
            CandidatesEvaluated: candidateResults.Count,
            CandidateResults: candidateResults,
            BlockedCandidates: blockedCandidates,
            CanPromoteAll: structuralViolations.Count == 0 && blockedCandidates == 0);

        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));
        context.LogSummary("ProtocolIntelligenceEvaluationJob");
        return Task.FromResult(JobRunResult.FromContext(context));
    }

    private IEnumerable<(string Source, PromotionGateRequest Request)> LoadCandidates()
    {
        var paths = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.ProtocolIntelligenceCandidatePath))
        {
            paths.Add(ResolveInputPath(_options.ProtocolIntelligenceCandidatePath));
        }

        if (!string.IsNullOrWhiteSpace(_options.ProtocolIntelligenceCandidateDirectory))
        {
            var dir = ResolveInputPath(_options.ProtocolIntelligenceCandidateDirectory);
            if (Directory.Exists(dir))
            {
                paths.AddRange(Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly));
            }
        }

        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.Ordinal))
        {
            yield return (Path.GetFileName(path), ParseCandidate(path));
        }
    }

    private static PromotionGateRequest ParseCandidate(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Protocol Intelligence candidate not found at '{path}'.", path);
        }

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
        {
            throw new InvalidOperationException($"Protocol Intelligence candidate is not a JSON object: {path}");
        }

        var artifactType = (root["artifactType"]?.GetValue<string>())
            ?? throw new InvalidOperationException($"Candidate '{path}' is missing 'artifactType'.");

        if (root["artifact"] is not JsonObject artifactNode)
        {
            throw new InvalidOperationException($"Candidate '{path}' is missing an 'artifact' object.");
        }

        var artifact = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in artifactNode)
        {
            artifact[property.Key] = ToClrValue(property.Value);
        }

        IReadOnlyList<string>? claimTags = null;
        if (root["claimTags"] is JsonArray tags)
        {
            claimTags = tags.Where(tag => tag is not null)
                .Select(tag => tag!.GetValue<string>())
                .ToArray();
        }

        return new PromotionGateRequest(artifactType, artifact, claimTags);
    }

    // The gate only inspects strings and string-bearing enumerables, so map JSON to those shapes.
    private static object? ToClrValue(JsonNode? node) => node switch
    {
        null => null,
        JsonArray array => array.Select(item => item is null ? string.Empty : ToScalarString(item)).ToArray(),
        JsonObject obj => obj.ToJsonString(),
        _ => ToScalarString(node),
    };

    private static string ToScalarString(JsonNode node)
    {
        var value = node.AsValue();
        return value.TryGetValue<string>(out var text)
            ? text
            : value.ToJsonString();
    }

    private static string[] Ordered(IReadOnlyList<string> values)
        => values.OrderBy(value => value, StringComparer.Ordinal).ToArray();

    private static string ResolveInputPath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static string ResolveOutputDirectory(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
}

public sealed record ProtocolIntelligenceEvaluationReport(
    string ReportVersion,
    DateTimeOffset GeneratedAtUtc,
    bool CorpusLoaded,
    string? CorpusError,
    IReadOnlyDictionary<string, string> ArtifactVersions,
    IReadOnlyList<PromotionTargetSummary> PromotionTargets,
    IReadOnlyList<string> StructuralViolations,
    int CandidatesEvaluated,
    IReadOnlyList<CandidateEvaluationResult> CandidateResults,
    int BlockedCandidates,
    bool CanPromoteAll);

public sealed record PromotionTargetSummary(
    string Id,
    int RequiredFieldCount,
    string ReviewGate,
    bool ForbiddenOutputScanRequired);

public sealed record CandidateEvaluationResult(
    string Source,
    string ArtifactType,
    bool CanPromote,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredFieldsMissing,
    IReadOnlyList<string> DoctrineViolationFields,
    bool RequiresHumanReview);
