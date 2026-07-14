namespace BioStack.KnowledgeWorker.Pipeline;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>
/// Builds and persists a deterministic, offline report around the structural evaluation snapshot.
/// The report records observations and unavailable metrics; it never invents thresholds or a verdict.
/// </summary>
public sealed class StructuralEvaluationReportBuilder
{
    public const string CurrentReportVersion = "1.0.0";
    public const string ReportFileName = "biostack-structural-evaluation-report.v1.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly StructuralEvaluationSnapshotBuilder _snapshotBuilder;

    public StructuralEvaluationReportBuilder(string? repositoryRoot = null)
    {
        _snapshotBuilder = new StructuralEvaluationSnapshotBuilder(repositoryRoot);
    }

    public StructuralEvaluationReport Build()
    {
        var payload = BuildPayload();
        var canonicalPayloadJson = Serialize(payload);

        return new StructuralEvaluationReport(
            ReportVersion: CurrentReportVersion,
            ReportKind: "offline-structural-evaluation",
            Payload: payload,
            CanonicalPayloadSha256: ComputeSha256(canonicalPayloadJson));
    }

    public string BuildJson()
        => Serialize(Build());

    public string WriteReport(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        var reportPath = Path.Combine(outputDirectory, ReportFileName);
        File.WriteAllText(reportPath, BuildJson() + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return reportPath;
    }

    private StructuralEvaluationReportPayload BuildPayload()
    {
        var snapshot = _snapshotBuilder.Build();
        var metrics = new[]
        {
            Observed("structural_coverage", "deterministic_counts_and_case_set_differences_recorded"),
            NotEvaluated("citation_provenance_integrity", "requires_structured_candidate_output_and_approved_source_truth"),
            NotEvaluated("cost", "requires_pinned_runtime_telemetry_and_approved_accounting_policy"),
            NotEvaluated("evidence_tier_consistency", "requires_structured_candidate_output_and_approved_tier_rules"),
            NotEvaluated("factuality", "requires_approved_semantic_truth_sources_and_scoring_rules"),
            NotEvaluated("latency", "requires_pinned_runtime_environment_and_approved_measurement_policy"),
            NotEvaluated("math_correctness", "requires_approved_numeric_gold_fields_and_tolerances"),
            NotEvaluated("privacy_leakage", "requires_approved_sentinel_categories_and_detection_policy"),
            NotEvaluated("refusal_precision_recall", "requires_approved_positive_negative_labels_and_denominators"),
            NotEvaluated("retrieval_coverage", "requires_pinned_retrieval_results_and_approved_relevance_truth"),
            NotEvaluated("safety_quality", "requires_approved_semantic_safety_labels_and_scoring_rules"),
            NotEvaluated("uncertainty_calibration", "requires_approved_confidence_targets_and_calibration_policy"),
            NotEvaluated("unsupported_claim_rate", "requires_structured_claim_spans_and_approved_support_rules"),
        };

        return new StructuralEvaluationReportPayload(
            Scope: "offline-structural-only",
            EvaluationStatus: "partial",
            PolicyStatus: "pending-approval",
            OverallVerdict: "not_evaluated",
            Snapshot: snapshot,
            Metrics: metrics.OrderBy(metric => metric.MetricId, StringComparer.Ordinal).ToArray(),
            ModelInvoked: false,
            NetworkAccessed: false);
    }

    private static StructuralEvaluationMetricState Observed(string metricId, string reasonCode)
        => new(metricId, "observed", reasonCode);

    private static StructuralEvaluationMetricState NotEvaluated(string metricId, string reasonCode)
        => new(metricId, "not_evaluated", reasonCode);

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, SerializerOptions);

    private static string ComputeSha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record StructuralEvaluationReport(
    string ReportVersion,
    string ReportKind,
    StructuralEvaluationReportPayload Payload,
    string CanonicalPayloadSha256);

public sealed record StructuralEvaluationReportPayload(
    string Scope,
    string EvaluationStatus,
    string PolicyStatus,
    string OverallVerdict,
    StructuralEvaluationSnapshot Snapshot,
    IReadOnlyList<StructuralEvaluationMetricState> Metrics,
    bool ModelInvoked,
    bool NetworkAccessed);

public sealed record StructuralEvaluationMetricState(
    string MetricId,
    string Status,
    string ReasonCode);
