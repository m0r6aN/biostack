namespace BioStack.Domain.Entities;

/// <summary>
/// Everything the reviewed substance record carries that the flat
/// <see cref="KnowledgeEntry"/> columns cannot hold: safety, regulatory detail,
/// claim-level evidence, gaps, controversies, studied uses, dose reports from both
/// sides (studies vs online reports), full interactions, titled sources and field
/// authority. Stored as one JSON document so the public page can show everything
/// gathered, marking what is known, unknown and in between.
/// </summary>
public sealed class KnowledgeEntryDetails
{
    public string CompoundFamily { get; set; } = string.Empty;
    public bool? RequiresPrescription { get; set; }
    public string Jurisdiction { get; set; } = string.Empty;
    public List<string> ApprovedIndications { get; set; } = new();
    public List<KnowledgeLabelStatus> LabelStatusByUseCase { get; set; } = new();
    public List<string> OffLabelNotes { get; set; } = new();

    public List<KnowledgeStudiedUse> StudiedUses { get; set; } = new();
    public KnowledgeSafety Safety { get; set; } = new();
    public List<KnowledgeDoseReport> DoseReports { get; set; } = new();
    public List<KnowledgeLabelDose> LabelDosing { get; set; } = new();
    public List<KnowledgeClaim> Claims { get; set; } = new();

    /// <summary>
    /// Claims as they circulate online, each with a plain-language verdict derived from
    /// its 7-tier grade (growth spec 02). Verdict, note and Hype Gap band stay
    /// NOT YET GRADED / empty until a reviewer signs off and the claim is published.
    /// </summary>
    public List<KnowledgePublicClaim> PublicClaims { get; set; } = new();
    public List<string> EvidenceGaps { get; set; } = new();
    public List<string> Controversies { get; set; } = new();
    public List<KnowledgeInteraction> Interactions { get; set; } = new();
    public List<KnowledgeFormulation> Formulations { get; set; } = new();
    public List<KnowledgeSourceRecord> Sources { get; set; } = new();

    /// <summary>"A" (official source present) or "B" (only non-official sources).</summary>
    public string RecordClass { get; set; } = string.Empty;

    /// <summary>Fields whose values come only from non-official sources; shown with a label, never hidden.</summary>
    public List<string> UnofficialSourceFields { get; set; } = new();

    /// <summary>pending-review, reviewed, changes-requested, escalated-to-owner, ai-council-reviewed.</summary>
    public string ReviewStatus { get; set; } = string.Empty;
    public DateTime? LastReviewedAt { get; set; }

    /// <summary>Pipeline review reasons. Internal context; the UI never prints these raw.</summary>
    public List<string> ReviewReasons { get; set; } = new();
}

public sealed class KnowledgeLabelStatus
{
    public string UseCase { get; set; } = string.Empty;
    public string LabelStatus { get; set; } = string.Empty;
}

public sealed class KnowledgeStudiedUse
{
    public string UseCase { get; set; } = string.Empty;
    public string LabelStatus { get; set; } = string.Empty;
    public string Population { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string EvidenceTier { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
}

public sealed class KnowledgeSafety
{
    public List<KnowledgeSafetyItem> Contraindications { get; set; } = new();
    public List<KnowledgeSafetyItem> Warnings { get; set; } = new();
    public List<KnowledgeSafetyItem> Precautions { get; set; } = new();
    public List<string> AdverseEffectsCommon { get; set; } = new();
    public List<string> AdverseEffectsSerious { get; set; } = new();
    public List<KnowledgeMonitoringItem> Monitoring { get; set; } = new();
}

public sealed class KnowledgeSafetyItem
{
    public string Item { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
}

public sealed class KnowledgeMonitoringItem
{
    public string Parameter { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
}

/// <summary>
/// One dose as found in one source. <see cref="Side"/> is "study" (trials, studies,
/// labels, guidelines, other professional sources) or "reported" (Reddit, Facebook,
/// Instagram, forums, blogs). Every row carries its source and a grade. A found dose is
/// a fact about a source, never a recommendation or direction.
/// </summary>
public sealed class KnowledgeDoseReport
{
    public string Side { get; set; } = "study";
    public string Context { get; set; } = string.Empty;
    public double? Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Population { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime? RetrievedAt { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>Label or guideline dosing text carried by the record (legacy dosingGuidance).</summary>
public sealed class KnowledgeLabelDose
{
    public string Context { get; set; } = string.Empty;
    public string Population { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public double? Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string ScheduleText { get; set; } = string.Empty;
    public bool ProductSpecific { get; set; }
    public List<string> Notes { get; set; } = new();
    public List<string> Sources { get; set; } = new();
}

public sealed class KnowledgeClaim
{
    public string Claim { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
}

public sealed class KnowledgePublicClaim
{
    /// <summary>Stable id, e.g. "bpc-157-heals-tendons". Never reused.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>The claim as people say it online. Never contains a dose.</summary>
    public string Statement { get; set; } = string.Empty;
    public string Population { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    /// <summary>Canonical 7-tier grade for this claim.</summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>Public verdict; NOT YET GRADED until published.</summary>
    public string Verdict { get; set; } = string.Empty;
    /// <summary>"In [population], for [outcome], under trial conditions." (PROVEN IN HUMANS only).</summary>
    public string Qualifier { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    /// <summary>Low · Moderate · High · Extreme, or empty when not scored.</summary>
    public string HypeBand { get; set; } = string.Empty;
    public string HypeMethodNote { get; set; } = string.Empty;
    public List<KnowledgeClaimSource> Sources { get; set; } = new();
    public int Version { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? RecheckDueAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool Published { get; set; }
    /// <summary>published · pending-council · changes-requested · escalated-to-owner.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>"ai-council" or "owner" once published.</summary>
    public string ReviewedBy { get; set; } = string.Empty;
    /// <summary>Council reviewer model ids (provider/model), or the owner id, once published.</summary>
    public List<string> Reviewers { get; set; } = new();
    /// <summary>Review-decision id that carried the sign-off.</summary>
    public string CouncilDecisionId { get; set; } = string.Empty;
    /// <summary>Signed-off versions only, oldest first.</summary>
    public List<KnowledgeClaimChange> Changelog { get; set; } = new();
}

public sealed class KnowledgeClaimSource
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
}

public sealed class KnowledgeClaimChange
{
    public int Version { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    /// <summary>Council reviewer model ids (provider/model) or the owner id.</summary>
    public List<string> Reviewers { get; set; } = new();
}

public sealed class KnowledgeInteraction
{
    public string Type { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Mechanism { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
}

public sealed class KnowledgeFormulation
{
    public string ProductName { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
}

public sealed class KnowledgeSourceRecord
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
}
