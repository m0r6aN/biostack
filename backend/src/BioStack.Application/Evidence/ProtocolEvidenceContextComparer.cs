namespace BioStack.Application.Evidence;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Evidence;

/// <summary>
/// Runs Class B comparisons for parsed protocol entries against knowledge-backed regimens.
/// </summary>
public sealed class ProtocolEvidenceContextComparer(IEvidenceContextComparisonService comparisonService)
{
    public IReadOnlyList<EvidenceContextComparisonResponse> CompareProtocolEntries(
        IEnumerable<ProtocolEntryResponse> protocol,
        IReadOnlyDictionary<string, KnowledgeEntry> knowledgeByCompound)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(knowledgeByCompound);

        var results = new List<EvidenceContextComparisonResponse>();
        foreach (var entry in protocol)
        {
            if (entry.Dose <= 0 || string.IsNullOrWhiteSpace(entry.Unit))
            {
                continue;
            }

            if (!knowledgeByCompound.TryGetValue(entry.CompoundName, out var knowledge))
            {
                continue;
            }

            var profile = KnowledgeEntryExposureProfileBuilder.TryBuild(knowledge);
            if (profile is null)
            {
                results.Add(new EvidenceContextComparisonResponse(
                    CompoundName: entry.CompoundName,
                    EnteredAmount: entry.Dose,
                    EnteredUnit: entry.Unit,
                    EnteredFrequency: entry.Frequency,
                    EnteredRoute: null,
                    RiskSignals: Array.Empty<string>(),
                    ReviewedInitiationMin: null,
                    ReviewedInitiationMax: null,
                    TimesAboveInitiationMin: null,
                    TimesAboveInitiationMax: null,
                    NormalizedUnit: entry.Unit,
                    Statements:
                    [
                        "Reviewed exposure ranges were not available in structured form for this compound, so a numeric evidence comparison could not be completed."
                    ],
                    UncertaintyMarkers: ["EVIDENCE_LIMITED"],
                    SourceReferences: knowledge.SourceReferences.Take(3).ToList(),
                    ComparisonAvailable: false));
                continue;
            }

            var exposure = new ProtocolExposure(
                SubjectName: entry.CompoundName,
                Amount: (decimal)entry.Dose,
                Unit: entry.Unit,
                Route: null,
                Frequency: string.IsNullOrWhiteSpace(entry.Frequency) ? null : entry.Frequency);

            var comparison = comparisonService.Compare(exposure, profile);
            results.Add(ToResponse(entry, comparison));
        }

        return results;
    }

    public static List<ProtocolIssueResponse> ToIssues(
        IReadOnlyList<EvidenceContextComparisonResponse> comparisons)
    {
        var issues = new List<ProtocolIssueResponse>();
        foreach (var comparison in comparisons.Where(c => c.ComparisonAvailable))
        {
            if (comparison.RiskSignals.Contains(EvidenceComparisonSignals.AboveReviewedInitiationRange, StringComparer.Ordinal)
                || comparison.RiskSignals.Contains(EvidenceComparisonSignals.AboveHighestReviewedExposure, StringComparer.Ordinal))
            {
                var message = comparison.Statements.FirstOrDefault(
                    s => s.Contains("times", StringComparison.OrdinalIgnoreCase))
                    ?? comparison.Statements.FirstOrDefault()
                    ?? "Entered amount is outside the reviewed initiation context.";

                issues.Add(new ProtocolIssueResponse(
                    Type: "evidence_context",
                    Message: message,
                    Compounds: [comparison.CompoundName]));
            }
        }

        return issues;
    }

    private static EvidenceContextComparisonResponse ToResponse(
        ProtocolEntryResponse entry,
        EvidenceContextComparison comparison)
        => new(
            CompoundName: entry.CompoundName,
            EnteredAmount: entry.Dose,
            EnteredUnit: entry.Unit,
            EnteredFrequency: entry.Frequency,
            EnteredRoute: null,
            RiskSignals: comparison.RiskSignals.ToList(),
            ReviewedInitiationMin: comparison.ClosestInitiationMin is null
                ? null
                : (double)comparison.ClosestInitiationMin.Value,
            ReviewedInitiationMax: comparison.ClosestInitiationMax is null
                ? null
                : (double)comparison.ClosestInitiationMax.Value,
            TimesAboveInitiationMin: comparison.TimesAboveInitiationMin is null
                ? null
                : (double)comparison.TimesAboveInitiationMin.Value,
            TimesAboveInitiationMax: comparison.TimesAboveInitiationMax is null
                ? null
                : (double)comparison.TimesAboveInitiationMax.Value,
            NormalizedUnit: comparison.NormalizedUnit,
            Statements: comparison.Statements.ToList(),
            UncertaintyMarkers: comparison.UncertaintyMarkers.ToList(),
            SourceReferences: comparison.SourceReferences.ToList(),
            ComparisonAvailable: true);
}
