namespace BioStack.KnowledgeWorker.Pipeline;

public enum SourceAcquisitionBatchStatus
{
    Completed = 0,
    NoMatch = 1,
    RateLimited = 2,
    BackPressure = 3,
}

public sealed record SourceAcquisitionCandidate(
    string RequestId,
    string CompoundName,
    string SourceRegistryId,
    string SourceItemId,
    string SourceUrl,
    string? QueryUrl,
    string SourcePublicationOrUpdateDate,
    DateTimeOffset RetrievedAtUtc,
    string RightsReviewStatusAtRetrieval,
    string RegistryBindingSha256,
    string TransformationPipelineVersion,
    string HumanReviewStatus,
    IReadOnlyList<string> EvidenceLimitations,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Fields)
{
    public IReadOnlyList<string> AuthorizedFieldUses { get; init; } = [];

    public IReadOnlyDictionary<string, SourceProvenanceValue> SourceSpecificProvenance
    {
        get;
        init;
    } = new Dictionary<string, SourceProvenanceValue>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SourceRightsAttribution> RightsAttributions { get; init; } = [];

    public IReadOnlyList<SourceDocumentProvenance> DocumentProvenance { get; init; } = [];

    public SourceReuseBoundary ReuseBoundary { get; init; } =
        SourceReuseBoundary.Unspecified;

    public SourceManualCaptureAudit? ManualCaptureAudit { get; init; }
}

public sealed record SourceRightsAttribution(
    string Scope,
    string Provider,
    string SourceUrl,
    string TermsUrl,
    string RightsStatus,
    IReadOnlyList<string> CoveredFields);

public sealed record SourceProvenanceValue(
    string Availability,
    IReadOnlyList<string> Values,
    string UnavailableReason)
{
    public static SourceProvenanceValue Present(params string[] values)
        => new("present", values, string.Empty);

    public static SourceProvenanceValue NotProvided(string reason)
        => new("not-provided", [], reason);

    public static SourceProvenanceValue NotApplicable(string reason)
        => new("not-applicable", [], reason);
}

public sealed record SourceDocumentProvenance(
    string Title,
    string Section,
    string PublishedDate,
    string UpdatedDate);

public sealed record SourceReuseBoundary(
    string Acknowledgement,
    IReadOnlyList<string> ExcludedContentClasses,
    bool NonEndorsementRequired)
{
    public static SourceReuseBoundary Unspecified { get; } =
        new(string.Empty, [], NonEndorsementRequired: false);
}

public sealed record SourceManualCaptureAudit(
    string OperatorId,
    DateTimeOffset CapturedAtUtc,
    string? ReviewerId,
    DateTimeOffset? ReviewedAtUtc,
    string Decision,
    IReadOnlyList<string> Notes,
    SourceManualCaptureAttestations Attestations);

public sealed record SourceManualCaptureAttestations(
    bool SourceAuthoredTextOnly,
    bool ExcludedRestrictedThirdPartyContent,
    bool AcknowledgementRetained,
    bool NoEndorsementImplication,
    bool NoIndividualizedAdviceOrDosingDirection,
    bool NoRegulatoryClaim,
    bool NoSafetyCriticalConclusion)
{
    public bool AllSatisfied =>
        SourceAuthoredTextOnly
        && ExcludedRestrictedThirdPartyContent
        && AcknowledgementRetained
        && NoEndorsementImplication
        && NoIndividualizedAdviceOrDosingDirection
        && NoRegulatoryClaim
        && NoSafetyCriticalConclusion;
}

public sealed record SourceAcquisitionBatch(
    SourceAcquisitionBatchStatus Status,
    IReadOnlyList<SourceAcquisitionCandidate> Candidates,
    bool Truncated,
    string? RetryAfter);

public interface ISourceAcquisitionAdapter
{
    string SourceId { get; }
    string AdapterId { get; }

    Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed class SourceAcquisitionException(string code, string message)
    : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
