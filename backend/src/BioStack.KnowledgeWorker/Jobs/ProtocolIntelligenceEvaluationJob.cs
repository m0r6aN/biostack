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

    // Report schema version. Bumped to 1.1.0 for the additive Summary section and the
    // operator-facing per-candidate fields (Status, Warnings, FailureDetails). Existing
    // consumers that key by field name remain compatible — no fields were removed or renamed.
    private const string ReportVersion = "1.1.0";

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
            var noCandidates = Array.Empty<CandidateEvaluationResult>();
            var failed = new ProtocolIntelligenceEvaluationReport(
                ReportVersion: ReportVersion,
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                Summary: BuildSummary(
                    promotionTargets: 0,
                    structuralViolations: 1,
                    candidates: noCandidates,
                    canPromoteAll: false),
                CorpusLoaded: false,
                CorpusError: ex.Message,
                ArtifactVersions: new Dictionary<string, string>(),
                PromotionTargets: Array.Empty<PromotionTargetSummary>(),
                StructuralViolations: new[] { $"corpus_load_failed: {ex.Message}" },
                CandidatesEvaluated: 0,
                CandidateResults: noCandidates,
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
            candidateResults.Add(BuildCandidateResult(source, request, result));

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

        var orderedViolations = structuralViolations
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToArray();
        var canPromoteAll = structuralViolations.Count == 0 && blockedCandidates == 0;

        var report = new ProtocolIntelligenceEvaluationReport(
            ReportVersion: ReportVersion,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Summary: BuildSummary(
                promotionTargets: targets.Length,
                structuralViolations: orderedViolations.Length,
                candidates: candidateResults,
                canPromoteAll: canPromoteAll),
            CorpusLoaded: true,
            CorpusError: null,
            ArtifactVersions: new SortedDictionary<string, string>(
                corpus.ArtifactVersions.ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal),
            PromotionTargets: targets,
            StructuralViolations: orderedViolations,
            CandidatesEvaluated: candidateResults.Count,
            CandidateResults: candidateResults,
            BlockedCandidates: blockedCandidates,
            CanPromoteAll: canPromoteAll);

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

    // Build the operator-facing per-candidate record from a gate result. Adds a plain pass/fail
    // status, soft warnings (e.g. promotable but still pending human review), and human-readable
    // fix hints derived purely from the gate's own reason codes and field paths — no content is
    // re-scanned and no forbidden-phrase logic lives here.
    private static CandidateEvaluationResult BuildCandidateResult(
        string source, PromotionGateRequest request, PromotionGateResult result)
    {
        var warnings = new List<string>();
        if (result.CanPromote && result.RequiresHumanReview)
        {
            warnings.Add("requires_human_review_before_promotion");
        }

        return new CandidateEvaluationResult(
            Source: source,
            ArtifactType: request.ArtifactType,
            Status: result.CanPromote ? "passed" : "failed",
            CanPromote: result.CanPromote,
            BlockingReasons: Ordered(result.BlockingReasons),
            RequiredFieldsMissing: Ordered(result.RequiredFieldsMissing),
            DoctrineViolationFields: Ordered(result.DoctrineViolationFields),
            RequiresHumanReview: result.RequiresHumanReview,
            Warnings: Ordered(warnings),
            FailureDetails: BuildFailureDetails(request.ArtifactType, result));
    }

    // Translate each blocking reason code into an actionable sentence so an operator can fix the
    // artifact without reading test output. Deterministic: derived from sorted gate fields and
    // sorted ordinally before return.
    private static string[] BuildFailureDetails(string artifactType, PromotionGateResult result)
    {
        if (result.CanPromote)
        {
            return Array.Empty<string>();
        }

        var details = new List<string>();
        foreach (var reason in result.BlockingReasons)
        {
            details.Add(reason switch
            {
                GateReasons.UnknownArtifactType =>
                    $"unknown_artifact_type: '{artifactType}' is not a registered promotion target.",
                GateReasons.ReviewStatusNotApproved =>
                    "review_status_not_approved: set artifact.reviewStatus to 'approved' after review.",
                GateReasons.HumanReviewRequired =>
                    "human_review_required: this artifact class needs a human reviewer before promotion.",
                GateReasons.RequiredFieldsMissing =>
                    $"required_fields_missing: populate {string.Join(", ", Ordered(result.RequiredFieldsMissing))}.",
                GateReasons.DoctrineViolation =>
                    $"doctrine_violation: revise user-facing text in {string.Join(", ", Ordered(result.DoctrineViolationFields))} "
                        + "to remove imperative/medical phrasing.",
                _ => reason,
            });
        }

        return details.OrderBy(detail => detail, StringComparer.Ordinal).ToArray();
    }

    // Roll the per-candidate results into a concise top-of-report summary. Warnings are reported
    // only when present (null otherwise), per the report convention.
    private static EvaluationSummary BuildSummary(
        int promotionTargets,
        int structuralViolations,
        IReadOnlyList<CandidateEvaluationResult> candidates,
        bool canPromoteAll)
    {
        var passed = candidates.Count(candidate => candidate.CanPromote);
        var failed = candidates.Count - passed;
        var withWarnings = candidates.Count(candidate => candidate.Warnings.Count > 0);

        return new EvaluationSummary(
            SchemaVersion: ReportVersion,
            PromotionTargetsEvaluated: promotionTargets,
            StructuralViolationCount: structuralViolations,
            CandidatesEvaluated: candidates.Count,
            CandidatesPassed: passed,
            CandidatesFailed: failed,
            CandidatesWithWarnings: withWarnings > 0 ? withWarnings : null,
            CanPromoteAll: canPromoteAll);
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
    // Concise at-a-glance rollup. Placed first so a reviewer reads the verdict before the detail.
    EvaluationSummary Summary,
    bool CorpusLoaded,
    string? CorpusError,
    IReadOnlyDictionary<string, string> ArtifactVersions,
    IReadOnlyList<PromotionTargetSummary> PromotionTargets,
    IReadOnlyList<string> StructuralViolations,
    int CandidatesEvaluated,
    IReadOnlyList<CandidateEvaluationResult> CandidateResults,
    int BlockedCandidates,
    bool CanPromoteAll);

/// <summary>
/// At-a-glance counts for the run. Every field is derived deterministically from the detailed
/// results below it; nothing here adds new judgement. <see cref="CandidatesWithWarnings"/> is
/// null when there are no warnings, so a clean run carries no warning noise.
/// </summary>
public sealed record EvaluationSummary(
    string SchemaVersion,
    int PromotionTargetsEvaluated,
    int StructuralViolationCount,
    int CandidatesEvaluated,
    int CandidatesPassed,
    int CandidatesFailed,
    int? CandidatesWithWarnings,
    bool CanPromoteAll);

public sealed record PromotionTargetSummary(
    string Id,
    int RequiredFieldCount,
    string ReviewGate,
    bool ForbiddenOutputScanRequired);

public sealed record CandidateEvaluationResult(
    string Source,
    string ArtifactType,
    // Plain pass/fail verdict for quick scanning; mirrors <see cref="CanPromote"/>.
    string Status,
    bool CanPromote,
    IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> RequiredFieldsMissing,
    IReadOnlyList<string> DoctrineViolationFields,
    bool RequiresHumanReview,
    // Non-blocking advisories (e.g. promotable but still pending human review).
    IReadOnlyList<string> Warnings,
    // Human-readable, actionable fix hints derived from the blocking reason codes.
    IReadOnlyList<string> FailureDetails);
