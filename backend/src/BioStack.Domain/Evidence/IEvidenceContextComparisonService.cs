namespace BioStack.Domain.Evidence;

/// <summary>
/// Deterministic comparison of a user-recorded exposure against reviewed published evidence.
/// Implements Guidance Content Contract Class B. Never produces Class D personal dosing advice.
/// </summary>
public interface IEvidenceContextComparisonService
{
    EvidenceContextComparison Compare(
        ProtocolExposure exposure,
        ReviewedEvidenceProfile evidence);
}
