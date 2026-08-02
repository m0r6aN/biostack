namespace BioStack.Contracts.Requests;

public sealed record AdminSubmitScientificResearchRequest(
    string SubjectName,
    string Workflow,
    string? CorrelationId = null,
    string? Purpose = null,
    Dictionary<string, string>? KnownIdentifiers = null,
    List<string>? EvidenceCategories = null);

public sealed record AdminStageScientificResearchRequest(
    string JobId);
