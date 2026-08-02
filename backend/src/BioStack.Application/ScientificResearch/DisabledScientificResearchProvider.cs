namespace BioStack.Application.ScientificResearch;

using BioStack.Application.Abstractions.ScientificResearch;

/// <summary>
/// Fail-closed provider used when the research sidecar is not enabled.
/// Does not invent research results.
/// </summary>
public sealed class DisabledScientificResearchProvider : IScientificResearchProvider
{
    public Task<ResearchJobHandle> SubmitAsync(
        ScientificResearchRequest request,
        CancellationToken cancellationToken = default)
        => throw new ScientificResearchProviderDisabledException();

    public Task<ResearchJobStatus> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default)
        => throw new ScientificResearchProviderDisabledException();

    public Task<ScientificResearchArtifact> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
        => throw new ScientificResearchProviderDisabledException();

    public Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default)
        => throw new ScientificResearchProviderDisabledException();
}

public sealed class ScientificResearchProviderDisabledException : InvalidOperationException
{
    public ScientificResearchProviderDisabledException()
        : base(
            "Scientific research sidecar is disabled. " +
            "Set ScientificResearchSidecar:Enabled=true and configure BaseUrl after the Python sidecar is deployed.")
    {
    }
}

public sealed class ScientificResearchProviderException : InvalidOperationException
{
    public string? ErrorCode { get; }

    public ScientificResearchProviderException(string message, string? errorCode = null, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
