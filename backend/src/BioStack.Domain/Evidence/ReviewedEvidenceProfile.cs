namespace BioStack.Domain.Evidence;

/// <summary>
/// Reviewed published evidence used for local protocol comparison.
/// </summary>
public sealed record ReviewedEvidenceProfile(
    string SubjectName,
    IReadOnlyList<PublishedExposureRegimen> Regimens,
    bool HasHumanEvidence = true,
    bool EvidenceLimitedToAnimals = false,
    bool EvidenceLimitedToCaseReports = false,
    bool ConflictingHumanEvidence = false);
