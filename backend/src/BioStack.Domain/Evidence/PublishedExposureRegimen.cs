namespace BioStack.Domain.Evidence;

/// <summary>
/// First-class published exposure context from a reviewed source.
/// Initiation, escalation, maintenance, and maximum are kept distinct.
/// </summary>
public sealed record PublishedExposureRegimen(
    string StudyArm,
    string Substance,
    decimal? InitiationAmountMin,
    decimal? InitiationAmountMax,
    decimal? MaintenanceAmountMin,
    decimal? MaintenanceAmountMax,
    decimal? MaximumStudiedAmount,
    string Unit,
    string? Route,
    string? Frequency,
    string SourceCitation,
    string? SourceLocation = null,
    string EvidenceClass = "controlled_human_trial",
    string? PopulationSummary = null);
