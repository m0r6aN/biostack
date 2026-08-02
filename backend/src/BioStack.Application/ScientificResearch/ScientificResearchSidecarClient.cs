namespace BioStack.Application.ScientificResearch;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Application.Abstractions.ScientificResearch;
using Microsoft.Extensions.Logging;

/// <summary>
/// HTTP client for the BioStack Python scientific research sidecar.
/// Maps BioStack-owned contracts; does not expose ToolUniverse types.
/// </summary>
public sealed class ScientificResearchSidecarClient(
    IHttpClientFactory httpClientFactory,
    ScientificResearchSidecarOptions options,
    ILogger<ScientificResearchSidecarClient> logger) : IScientificResearchProvider
{
    public const string HttpClientName = "biostack-research-sidecar";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(Math.Max(1_000, options.TimeoutMs));

    public async Task<ResearchJobHandle> SubmitAsync(
        ScientificResearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsurePublicScientificPayload(request);

        using var http = CreateClient();
        using var response = await http.PostAsJsonAsync(
            "internal/v1/research/jobs",
            ToSidecarRequest(request),
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "[ScientificResearch] Submit failed HTTP {Status}: {Body}",
                (int)response.StatusCode,
                Truncate(body));
            throw new ScientificResearchProviderException(
                $"Sidecar submit failed with HTTP {(int)response.StatusCode}.",
                errorCode: "submit_failed");
        }

        var dto = await response.Content.ReadFromJsonAsync<SidecarJobHandleDto>(JsonOptions, cancellationToken)
                  ?? throw new ScientificResearchProviderException("Sidecar returned null job handle.", "null_handle");

        return new ResearchJobHandle(
            JobId: dto.JobId,
            ResearchRequestId: dto.ResearchRequestId,
            Workflow: dto.Workflow,
            Status: MapStatus(dto.Status),
            SubmittedAtUtc: dto.SubmittedAtUtc,
            CorrelationId: dto.CorrelationId);
    }

    public async Task<ResearchJobStatus> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        using var http = CreateClient();
        using var response = await http.GetAsync(
            $"internal/v1/research/jobs/{Uri.EscapeDataString(jobId)}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ScientificResearchProviderException($"Job '{jobId}' was not found.", "job_not_found");
        }

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SidecarJobStatusDto>(JsonOptions, cancellationToken)
                  ?? throw new ScientificResearchProviderException("Sidecar returned null job status.", "null_status");

        return new ResearchJobStatus(
            JobId: dto.JobId,
            ResearchRequestId: dto.ResearchRequestId,
            Workflow: dto.Workflow,
            Status: MapStatus(dto.Status),
            ProgressMessage: dto.ProgressMessage,
            Partial: dto.Partial,
            ErrorCode: dto.ErrorCode,
            ErrorMessage: dto.ErrorMessage,
            SubmittedAtUtc: dto.SubmittedAtUtc,
            UpdatedAtUtc: dto.UpdatedAtUtc,
            FinishedAtUtc: dto.FinishedAtUtc,
            CorrelationId: dto.CorrelationId);
    }

    public async Task<ScientificResearchArtifact> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        using var http = CreateClient();
        using var response = await http.GetAsync(
            $"internal/v1/research/jobs/{Uri.EscapeDataString(jobId)}/result",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ScientificResearchProviderException($"Job '{jobId}' was not found.", "job_not_found");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new ScientificResearchProviderException(
                $"Result for job '{jobId}' is not ready.",
                "result_not_ready");
        }

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SidecarArtifactDto>(JsonOptions, cancellationToken)
                  ?? throw new ScientificResearchProviderException("Sidecar returned null artifact.", "null_artifact");

        return new ScientificResearchArtifact(
            ResearchArtifactId: dto.ResearchArtifactId,
            JobId: dto.JobId,
            ResearchRequestId: dto.ResearchRequestId,
            Provider: dto.Provider,
            ProviderVersion: dto.ProviderVersion,
            Workflow: dto.Workflow,
            WorkflowVersion: dto.WorkflowVersion,
            ToolUniverseVersion: dto.TooluniverseVersion,
            Status: MapStatus(dto.Status),
            Partial: dto.Partial,
            StartedAtUtc: dto.StartedAtUtc,
            FinishedAtUtc: dto.FinishedAtUtc,
            ToolsInvoked: dto.ToolsInvoked ?? Array.Empty<string>(),
            Warnings: dto.Warnings ?? Array.Empty<string>(),
            FailureDetails: dto.FailureDetails,
            ExecutionDevice: dto.ExecutionDevice,
            Provenance: FlattenProvenance(dto.Provenance));
    }

    public async Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        using var http = CreateClient();
        using var response = await http.PostAsync(
            $"internal/v1/research/jobs/{Uri.EscapeDataString(jobId)}/cancel",
            content: null,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new ScientificResearchProviderException($"Job '{jobId}' was not found.", "job_not_found");
        }

        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient()
    {
        var http = httpClientFactory.CreateClient(HttpClientName);
        http.Timeout = _timeout;
        return http;
    }

    private static void EnsurePublicScientificPayload(ScientificResearchRequest request)
    {
        if (!string.Equals(request.DataClassification, "public_scientific", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.DataClassification, "public_metadata", StringComparison.OrdinalIgnoreCase))
        {
            throw new ScientificResearchProviderException(
                "Only public_scientific or public_metadata classifications may be sent to the sidecar.",
                "data_classification_rejected");
        }
    }

    private static object ToSidecarRequest(ScientificResearchRequest request) => new
    {
        research_request_id = request.ResearchRequestId,
        research_subject_type = request.ResearchSubjectType,
        subject_name = request.SubjectName,
        known_identifiers = request.KnownIdentifiers,
        workflow = request.Workflow,
        evidence_categories = request.EvidenceCategories,
        source_allowlist = request.SourceAllowlist,
        maximum_source_age_days = request.MaximumSourceAgeDays,
        maximum_execution_time_seconds = (int)Math.Max(1, request.MaximumExecutionTime.TotalSeconds),
        maximum_source_count = request.MaximumSourceCount,
        correlation_id = request.CorrelationId,
        requested_by_actor = request.RequestedByActor,
        purpose = request.Purpose,
        data_classification = request.DataClassification,
        task_class = request.TaskClass,
        evidence_risk_class = request.EvidenceRiskClass switch
        {
            EvidenceRiskClass.Low => "low",
            EvidenceRiskClass.High => "high",
            _ => "medium",
        },
        local_inference_permitted = request.LocalInferencePermitted,
        hosted_inference_permitted = request.HostedInferencePermitted,
        compression_permitted = request.CompressionPermitted,
        cross_check_required = request.CrossCheckRequired,
        execution = new
        {
            mode = request.Execution.Mode switch
            {
                ScientificExecutionMode.GpuPreferred => "gpu_preferred",
                ScientificExecutionMode.GpuRequired => "gpu_required",
                ScientificExecutionMode.CpuOnly => "cpu_only",
                ScientificExecutionMode.HostedFallbackAllowed => "hosted_fallback_allowed",
                _ => "auto",
            },
            allow_gpu = request.Execution.AllowGpu,
            allow_cpu_fallback = request.Execution.AllowCpuFallback,
            allow_hosted_fallback = request.Execution.AllowHostedFallback,
            maximum_gpu_memory_bytes = request.Execution.MaximumGpuMemoryBytes,
            maximum_execution_duration_seconds = (int)Math.Max(
                1,
                request.Execution.MaximumExecutionDuration.TotalSeconds),
            approved_model_profile = request.Execution.ApprovedModelProfile,
        },
    };

    private static ResearchJobStatusCode MapStatus(string? status) => status switch
    {
        "queued" => ResearchJobStatusCode.Queued,
        "resolving_identity" => ResearchJobStatusCode.ResolvingIdentity,
        "gathering_evidence" => ResearchJobStatusCode.GatheringEvidence,
        "normalizing" => ResearchJobStatusCode.Normalizing,
        "pending_review" => ResearchJobStatusCode.PendingReview,
        "completed" => ResearchJobStatusCode.Completed,
        "failed" => ResearchJobStatusCode.Failed,
        "cancelled" => ResearchJobStatusCode.Cancelled,
        "partial" => ResearchJobStatusCode.Partial,
        "rejected_by_policy" => ResearchJobStatusCode.RejectedByPolicy,
        _ => ResearchJobStatusCode.Failed,
    };

    private static IReadOnlyDictionary<string, string> FlattenProvenance(JsonElement? provenance)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (provenance is null || provenance.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return map;
        }

        if (provenance.Value.ValueKind != JsonValueKind.Object)
        {
            map["value"] = provenance.Value.ToString();
            return map;
        }

        foreach (var property in provenance.Value.EnumerateObject())
        {
            map[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => property.Value.GetRawText(),
            };
        }

        return map;
    }

    private static string Truncate(string value)
        => value.Length <= 500 ? value : value[..500] + "…";

    private sealed record SidecarJobHandleDto(
        string JobId,
        string ResearchRequestId,
        string Workflow,
        string Status,
        DateTimeOffset SubmittedAtUtc,
        string CorrelationId);

    private sealed record SidecarJobStatusDto(
        string JobId,
        string ResearchRequestId,
        string Workflow,
        string Status,
        string? ProgressMessage,
        bool Partial,
        string? ErrorCode,
        string? ErrorMessage,
        DateTimeOffset SubmittedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        DateTimeOffset? FinishedAtUtc,
        string CorrelationId);

    private sealed record SidecarArtifactDto(
        string ResearchArtifactId,
        string JobId,
        string ResearchRequestId,
        string Provider,
        string ProviderVersion,
        string Workflow,
        string WorkflowVersion,
        string? TooluniverseVersion,
        string Status,
        bool Partial,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset? FinishedAtUtc,
        IReadOnlyList<string>? ToolsInvoked,
        IReadOnlyList<string>? Warnings,
        string? FailureDetails,
        string ExecutionDevice,
        JsonElement? Provenance);
}
