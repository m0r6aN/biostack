namespace BioStack.KnowledgeWorker.Models;

using System.Text.Json.Serialization;

public sealed class BlendingRule
{
    [JsonPropertyName("ruleType")] public string       RuleType { get; set; } = string.Empty;
    [JsonPropertyName("target")]   public string       Target   { get; set; } = string.Empty;
    [JsonPropertyName("status")]   public string       Status   { get; set; } = string.Empty;
    [JsonPropertyName("message")]  public string       Message  { get; set; } = string.Empty;
    [JsonPropertyName("sources")]  public List<string> Sources  { get; set; } = new();
}

public sealed class Compatibility
{
    [JsonPropertyName("vialCompatibilitySummary")] public string?            VialCompatibilitySummary { get; set; }
    [JsonPropertyName("blendingRules")]            public List<BlendingRule> BlendingRules            { get; set; } = new();
    [JsonPropertyName("compatibleBlends")]         public List<string>       CompatibleBlends         { get; set; } = new();
    [JsonPropertyName("incompatibleBlends")]       public List<string>       IncompatibleBlends       { get; set; } = new();
    [JsonPropertyName("unknownCompatibility")]     public List<string>       UnknownCompatibility     { get; set; } = new();
}

public sealed class SafetyItem
{
    [JsonPropertyName("item")]     public string       Item     { get; set; } = string.Empty;
    [JsonPropertyName("severity")] public string       Severity { get; set; } = string.Empty;
    [JsonPropertyName("notes")]    public string?      Notes    { get; set; }
    [JsonPropertyName("sources")]  public List<string> Sources  { get; set; } = new();
}

public sealed class PrecautionItem
{
    [JsonPropertyName("item")]    public string       Item    { get; set; } = string.Empty;
    [JsonPropertyName("notes")]   public string?      Notes   { get; set; }
    [JsonPropertyName("sources")] public List<string> Sources { get; set; } = new();
}

public sealed class MonitoringItem
{
    [JsonPropertyName("parameter")] public string       Parameter { get; set; } = string.Empty;
    [JsonPropertyName("reason")]    public string       Reason    { get; set; } = string.Empty;
    [JsonPropertyName("frequency")] public string?      Frequency { get; set; }
    [JsonPropertyName("sources")]   public List<string> Sources   { get; set; } = new();
}

public sealed class AdverseEffects
{
    [JsonPropertyName("common")]  public List<string> Common  { get; set; } = new();
    [JsonPropertyName("serious")] public List<string> Serious { get; set; } = new();
}

public sealed class Safety
{
    [JsonPropertyName("contraindications")] public List<SafetyItem>     Contraindications { get; set; } = new();
    [JsonPropertyName("warnings")]          public List<SafetyItem>     Warnings          { get; set; } = new();
    [JsonPropertyName("precautions")]       public List<PrecautionItem> Precautions       { get; set; } = new();
    [JsonPropertyName("adverseEffects")]    public AdverseEffects       AdverseEffects    { get; set; } = new();
    [JsonPropertyName("monitoring")]        public List<MonitoringItem> Monitoring        { get; set; } = new();
}

public sealed class Interaction
{
    [JsonPropertyName("interactionId")]     public string       InteractionId     { get; set; } = string.Empty;
    [JsonPropertyName("type")]              public string       Type              { get; set; } = string.Empty;
    [JsonPropertyName("target")]            public string       Target            { get; set; } = string.Empty;
    [JsonPropertyName("severity")]          public string       Severity          { get; set; } = string.Empty;
    [JsonPropertyName("mechanism")]         public string?      Mechanism         { get; set; }
    [JsonPropertyName("effect")]            public string?      Effect            { get; set; }
    [JsonPropertyName("recommendedAction")] public string       RecommendedAction { get; set; } = string.Empty;
    [JsonPropertyName("sources")]           public List<string> Sources           { get; set; } = new();
}

public sealed class PairRule
{
    [JsonPropertyName("target")]    public string       Target    { get; set; } = string.Empty;
    [JsonPropertyName("context")]   public string?      Context   { get; set; }
    [JsonPropertyName("rationale")] public string?      Rationale { get; set; }
    [JsonPropertyName("sources")]   public List<string> Sources   { get; set; } = new();
}

public sealed class AvoidRule
{
    [JsonPropertyName("target")]   public string       Target   { get; set; } = string.Empty;
    [JsonPropertyName("reason")]   public string?      Reason   { get; set; }
    [JsonPropertyName("severity")] public string       Severity { get; set; } = string.Empty;
    [JsonPropertyName("sources")]  public List<string> Sources  { get; set; } = new();
}

public sealed class StackRule
{
    [JsonPropertyName("ruleId")]   public string       RuleId   { get; set; } = string.Empty;
    [JsonPropertyName("ruleType")] public string       RuleType { get; set; } = string.Empty;
    [JsonPropertyName("tag")]      public string       Tag      { get; set; } = string.Empty;
    [JsonPropertyName("severity")] public string       Severity { get; set; } = string.Empty;
    [JsonPropertyName("message")]  public string       Message  { get; set; } = string.Empty;
    [JsonPropertyName("action")]   public string       Action   { get; set; } = string.Empty;
    [JsonPropertyName("sources")]  public List<string> Sources  { get; set; } = new();
}

public sealed class StackIntelligence
{
    [JsonPropertyName("pairsWellWith")]    public List<PairRule>  PairsWellWith    { get; set; } = new();
    [JsonPropertyName("avoidWith")]        public List<AvoidRule> AvoidWith        { get; set; } = new();
    [JsonPropertyName("overlapRules")]     public List<StackRule> OverlapRules     { get; set; } = new();
    [JsonPropertyName("synergyRules")]     public List<StackRule> SynergyRules     { get; set; } = new();
    [JsonPropertyName("conflictRules")]    public List<StackRule> ConflictRules    { get; set; } = new();
    [JsonPropertyName("redundancyRules")]  public List<StackRule> RedundancyRules  { get; set; } = new();
    [JsonPropertyName("timingRules")]      public List<StackRule> TimingRules      { get; set; } = new();
}

public sealed class GuidanceItem
{
    [JsonPropertyName("topic")]    public string Topic    { get; set; } = string.Empty;
    [JsonPropertyName("guidance")] public string Guidance { get; set; } = string.Empty;
}

public sealed class SimpleGuidanceItem
{
    [JsonPropertyName("guidance")] public string Guidance { get; set; } = string.Empty;
}

public sealed class SupportiveGuidance
{
    [JsonPropertyName("nutrition")]          public List<GuidanceItem>       Nutrition          { get; set; } = new();
    [JsonPropertyName("supplements")]        public List<GuidanceItem>       Supplements        { get; set; } = new();
    [JsonPropertyName("sleep")]              public List<SimpleGuidanceItem> Sleep              { get; set; } = new();
    [JsonPropertyName("exercise")]           public List<SimpleGuidanceItem> Exercise           { get; set; } = new();
    [JsonPropertyName("applicabilityNotes")] public List<string>             ApplicabilityNotes { get; set; } = new();
    [JsonPropertyName("sources")]            public List<string>             Sources            { get; set; } = new();
}

public sealed class ClaimEvidence
{
    [JsonPropertyName("claim")]      public string       Claim      { get; set; } = string.Empty;
    [JsonPropertyName("tier")]       public string       Tier       { get; set; } = string.Empty;
    [JsonPropertyName("confidence")] public string       Confidence { get; set; } = string.Empty;
    [JsonPropertyName("sources")]    public List<string> Sources    { get; set; } = new();
    /// <summary>Grade before the 7-tier regrade (Strong/Moderate/Limited/Insufficient/Unknown).</summary>
    [JsonPropertyName("legacyTier")]     public string? LegacyTier     { get; set; }
    [JsonPropertyName("gradeRationale")] public string? GradeRationale { get; set; }
}

public sealed class Evidence
{
    [JsonPropertyName("overallTier")]           public string              OverallTier           { get; set; } = string.Empty;
    [JsonPropertyName("claimSpecificEvidence")] public List<ClaimEvidence> ClaimSpecificEvidence { get; set; } = new();
    [JsonPropertyName("evidenceGaps")]          public List<string>        EvidenceGaps          { get; set; } = new();
    [JsonPropertyName("controversies")]         public List<string>        Controversies         { get; set; } = new();
    /// <summary>Claims as they circulate online, graded on the 7-tier scale and versioned (growth spec 02).</summary>
    [JsonPropertyName("publicClaims")]          public List<PublicClaim>   PublicClaims          { get; set; } = new();
}

/// <summary>A claim as people state it online, with its grade, sources and version history.</summary>
public sealed class PublicClaim
{
    [JsonPropertyName("id")]            public string       Id            { get; set; } = string.Empty;
    [JsonPropertyName("statement")]     public string       Statement     { get; set; } = string.Empty;
    [JsonPropertyName("population")]    public string       Population    { get; set; } = string.Empty;
    [JsonPropertyName("outcome")]       public string       Outcome       { get; set; } = string.Empty;
    [JsonPropertyName("tier")]          public string       Tier          { get; set; } = string.Empty;
    /// <summary>Required when tier is Hype Exceeds Evidence: the tier the evidence itself earns.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] [JsonPropertyName("evidenceBasis")] public string?      EvidenceBasis { get; set; }
    /// <summary>Trial population and outcome for the PROVEN IN HUMANS qualifier.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] [JsonPropertyName("qualifier")]     public ClaimQualifier? Qualifier  { get; set; }
    [JsonPropertyName("note")]          public string       Note          { get; set; } = string.Empty;
    [JsonPropertyName("sources")]       public List<string> Sources       { get; set; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] [JsonPropertyName("hype")]          public HypeAssessment? Hype       { get; set; }
    [JsonPropertyName("versions")]      public List<PublicClaimVersion> Versions { get; set; } = new();
}

public sealed class ClaimQualifier
{
    [JsonPropertyName("population")] public string Population { get; set; } = string.Empty;
    [JsonPropertyName("outcome")]    public string Outcome    { get; set; } = string.Empty;
}

/// <summary>Hype Gap v0 editorial score: rubric level (Rare/Niche/Common/Everywhere/Viral) and intensity 0.8–1.3.</summary>
public sealed class HypeAssessment
{
    [JsonPropertyName("rubric")]    public string    Rubric    { get; set; } = string.Empty;
    [JsonPropertyName("intensity")] public double    Intensity { get; set; } = 1.0;
    [JsonPropertyName("scoredAt")]  public DateTime? ScoredAt  { get; set; }
    [JsonPropertyName("scoredBy")]  public string    ScoredBy  { get; set; } = string.Empty;
    [JsonPropertyName("rationale")] public string    Rationale { get; set; } = string.Empty;
}

/// <summary>One entry of a claim's version history. Old versions are never deleted.</summary>
public sealed class PublicClaimVersion
{
    [JsonPropertyName("version")]      public int          Version      { get; set; }
    /// <summary>initial, update, correction or edit (edit = typo fix, no bump).</summary>
    [JsonPropertyName("kind")]         public string       Kind         { get; set; } = string.Empty;
    [JsonPropertyName("tier")]         public string       Tier         { get; set; } = string.Empty;
    [JsonPropertyName("verdict")]      public string       Verdict      { get; set; } = string.Empty;
    [JsonPropertyName("sources")]      public List<string> Sources      { get; set; } = new();
    [JsonPropertyName("verifiedAt")]   public DateTime?    VerifiedAt   { get; set; }
    [JsonPropertyName("verifiedBy")]   public string       VerifiedBy   { get; set; } = string.Empty;
    [JsonPropertyName("reviewers")]    public List<string> Reviewers    { get; set; } = new();
    [JsonPropertyName("changeReason")] public string       ChangeReason { get; set; } = string.Empty;
    [JsonPropertyName("publishedAt")]  public DateTime?    PublishedAt  { get; set; }
}

public sealed class SourceRecord
{
    /// <summary>Optional id that claim and public-claim "sources" arrays point at.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] [JsonPropertyName("id")]            public string?   Id            { get; set; }
    [JsonPropertyName("sourceType")]    public string    SourceType    { get; set; } = string.Empty;
    [JsonPropertyName("title")]         public string    Title         { get; set; } = string.Empty;
    [JsonPropertyName("publisher")]     public string?   Publisher     { get; set; }
    [JsonPropertyName("url")]           public string?   Url           { get; set; }
    [JsonPropertyName("publishedAt")]   public DateTime? PublishedAt   { get; set; }
    [JsonPropertyName("lastCheckedAt")] public DateTime? LastCheckedAt { get; set; }
}

public sealed class Provenance
{
    [JsonPropertyName("sourceRecords")]   public List<SourceRecord> SourceRecords   { get; set; } = new();
    [JsonPropertyName("curationNotes")]   public List<string>       CurationNotes   { get; set; } = new();
    [JsonPropertyName("lastReviewedAt")]  public DateTime?          LastReviewedAt  { get; set; }
    [JsonPropertyName("reviewStatus")]    public string             ReviewStatus    { get; set; } = string.Empty;
}

public sealed class Ops
{
    [JsonPropertyName("recordVersion")]    public int          RecordVersion    { get; set; } = 1;
    [JsonPropertyName("contentHash")]      public string?      ContentHash      { get; set; }
    [JsonPropertyName("ingestionSource")]  public string       IngestionSource  { get; set; } = string.Empty;
    [JsonPropertyName("ingestedAt")]       public DateTime?    IngestedAt       { get; set; }
    [JsonPropertyName("updatedAt")]        public DateTime?    UpdatedAt        { get; set; }
    [JsonPropertyName("lastChangeType")]   public string       LastChangeType   { get; set; } = string.Empty;
    [JsonPropertyName("isActive")]         public bool         IsActive         { get; set; } = true;
    [JsonPropertyName("completeness")]     public string       Completeness     { get; set; } = string.Empty;
    [JsonPropertyName("needsReview")]      public bool         NeedsReview      { get; set; }
    [JsonPropertyName("reviewReasons")]    public List<string> ReviewReasons    { get; set; } = new();
    [JsonPropertyName("qualityFlags")]     public List<string> QualityFlags     { get; set; } = new();
}
