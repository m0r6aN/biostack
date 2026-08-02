namespace BioStack.Application.Abstractions.ScientificResearch;

/// <summary>
/// Provider-neutral scientific research contract.
/// Infrastructure adapters call the Python sidecar; Domain must not depend on ToolUniverse or Ollama types.
/// Sidecar output is candidate evidence only — never direct canonical promotion.
/// </summary>
public interface IScientificResearchProvider
{
    Task<ResearchJobHandle> SubmitAsync(
        ScientificResearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ResearchJobStatus> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    Task<ScientificResearchArtifact> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
