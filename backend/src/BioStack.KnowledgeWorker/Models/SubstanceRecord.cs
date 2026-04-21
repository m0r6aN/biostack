namespace BioStack.KnowledgeWorker.Models;

using System.Text.Json.Serialization;

/// <summary>
/// C# projection of the BioStack substance record schema
/// (see <c>Schemas/substance-record.schema.json</c>, $id https://biostack.cc/schemas/substance-record.schema.json).
/// Shape must stay in lockstep with the schema; fields are mandatory unless marked nullable in the schema.
/// </summary>
public sealed class SubstanceRecord
{
    [JsonPropertyName("schemaVersion")]       public string              SchemaVersion       { get; set; } = string.Empty;
    [JsonPropertyName("recordType")]          public string              RecordType          { get; set; } = "substance";
    [JsonPropertyName("identity")]            public Identity            Identity            { get; set; } = new();
    [JsonPropertyName("regulatory")]          public Regulatory          Regulatory          { get; set; } = new();
    [JsonPropertyName("mechanism")]           public Mechanism           Mechanism           { get; set; } = new();
    [JsonPropertyName("formulations")]        public List<Formulation>   Formulations        { get; set; } = new();
    [JsonPropertyName("indications")]         public List<Indication>    Indications         { get; set; } = new();
    [JsonPropertyName("dosingGuidance")]      public List<DosingGuidance> DosingGuidance     { get; set; } = new();
    [JsonPropertyName("compatibility")]       public Compatibility       Compatibility       { get; set; } = new();
    [JsonPropertyName("safety")]              public Safety              Safety              { get; set; } = new();
    [JsonPropertyName("interactions")]        public List<Interaction>   Interactions        { get; set; } = new();
    [JsonPropertyName("stackIntelligence")]   public StackIntelligence   StackIntelligence   { get; set; } = new();
    [JsonPropertyName("supportiveGuidance")]  public SupportiveGuidance  SupportiveGuidance  { get; set; } = new();
    [JsonPropertyName("evidence")]            public Evidence            Evidence            { get; set; } = new();
    [JsonPropertyName("provenance")]          public Provenance          Provenance          { get; set; } = new();
    [JsonPropertyName("ops")]                 public Ops                 Ops                 { get; set; } = new();
}

public sealed class Identity
{
    [JsonPropertyName("canonicalId")]          public string              CanonicalId          { get; set; } = string.Empty;
    [JsonPropertyName("canonicalName")]        public string              CanonicalName        { get; set; } = string.Empty;
    [JsonPropertyName("slug")]                 public string              Slug                 { get; set; } = string.Empty;
    [JsonPropertyName("aliases")]              public List<string>        Aliases              { get; set; } = new();
    [JsonPropertyName("brandNames")]           public List<string>        BrandNames           { get; set; } = new();
    [JsonPropertyName("synonyms")]             public List<string>        Synonyms             { get; set; } = new();
    [JsonPropertyName("classification")]       public string              Classification       { get; set; } = string.Empty;
    [JsonPropertyName("compoundFamily")]       public string              CompoundFamily       { get; set; } = string.Empty;
    [JsonPropertyName("isCombinationProduct")] public bool                IsCombinationProduct { get; set; }
    [JsonPropertyName("activeMoieties")]       public List<string>        ActiveMoieties       { get; set; } = new();
    [JsonPropertyName("externalIdentifiers")]  public ExternalIdentifiers ExternalIdentifiers  { get; set; } = new();
}

public sealed class ExternalIdentifiers
{
    [JsonPropertyName("rxnorm")]    public string? Rxnorm    { get; set; }
    [JsonPropertyName("unii")]      public string? Unii      { get; set; }
    [JsonPropertyName("casNumber")] public string? CasNumber { get; set; }
    [JsonPropertyName("drugbank")]  public string? Drugbank  { get; set; }
    [JsonPropertyName("pubchem")]   public string? Pubchem   { get; set; }
}

public sealed class Regulatory
{
    [JsonPropertyName("requiresPrescription")] public bool                           RequiresPrescription { get; set; }
    [JsonPropertyName("regulatoryStatus")]     public string                         RegulatoryStatus     { get; set; } = string.Empty;
    [JsonPropertyName("labelStatusByUseCase")] public List<LabelStatusByUseCaseItem> LabelStatusByUseCase { get; set; } = new();
    [JsonPropertyName("jurisdiction")]         public string                         Jurisdiction         { get; set; } = string.Empty;
    [JsonPropertyName("approvedIndications")]  public List<string>                   ApprovedIndications  { get; set; } = new();
    [JsonPropertyName("offLabelNotes")]        public List<string>                   OffLabelNotes        { get; set; } = new();
}

public sealed class LabelStatusByUseCaseItem
{
    [JsonPropertyName("useCase")]     public string UseCase     { get; set; } = string.Empty;
    [JsonPropertyName("labelStatus")] public string LabelStatus { get; set; } = string.Empty;
}

public sealed class Mechanism
{
    [JsonPropertyName("mechanismSummary")]  public string       MechanismSummary  { get; set; } = string.Empty;
    [JsonPropertyName("primaryMechanisms")] public List<string> PrimaryMechanisms { get; set; } = new();
    [JsonPropertyName("pathways")]          public List<string> Pathways          { get; set; } = new();
    [JsonPropertyName("targets")]           public List<string> Targets           { get; set; } = new();
    [JsonPropertyName("effectTags")]        public List<string> EffectTags        { get; set; } = new();
    [JsonPropertyName("goalTags")]          public List<string> GoalTags          { get; set; } = new();
}

public sealed class Formulation
{
    [JsonPropertyName("formulationId")]     public string         FormulationId     { get; set; } = string.Empty;
    [JsonPropertyName("productName")]       public string         ProductName       { get; set; } = string.Empty;
    [JsonPropertyName("manufacturer")]      public string?        Manufacturer      { get; set; }
    [JsonPropertyName("route")]             public string         Route             { get; set; } = string.Empty;
    [JsonPropertyName("dosageForm")]        public string         DosageForm        { get; set; } = string.Empty;
    [JsonPropertyName("strength")]          public string?        Strength          { get; set; }
    [JsonPropertyName("concentration")]     public double?        Concentration     { get; set; }
    [JsonPropertyName("unit")]              public string?        Unit              { get; set; }
    [JsonPropertyName("substitutableWith")] public List<string>   SubstitutableWith { get; set; } = new();
    [JsonPropertyName("storage")]           public Storage        Storage           { get; set; } = new();
    [JsonPropertyName("reconstitution")]    public Reconstitution Reconstitution    { get; set; } = new();
}

public sealed class Storage
{
    [JsonPropertyName("beforeReconstitution")] public string? BeforeReconstitution { get; set; }
    [JsonPropertyName("afterReconstitution")]  public string? AfterReconstitution  { get; set; }
    [JsonPropertyName("temperatureNotes")]     public string? TemperatureNotes     { get; set; }
    [JsonPropertyName("lightSensitivity")]     public bool?   LightSensitivity     { get; set; }
    [JsonPropertyName("stabilityWindow")]      public string? StabilityWindow      { get; set; }
}

public sealed class Reconstitution
{
    [JsonPropertyName("required")]        public bool    Required        { get; set; }
    [JsonPropertyName("diluent")]         public string? Diluent         { get; set; }
    [JsonPropertyName("diluentVolume")]   public double? DiluentVolume   { get; set; }
    [JsonPropertyName("instructions")]    public string? Instructions    { get; set; }
    [JsonPropertyName("productSpecific")] public bool    ProductSpecific { get; set; }
}

public sealed class Indication
{
    [JsonPropertyName("useCase")]        public string       UseCase        { get; set; } = string.Empty;
    [JsonPropertyName("labelStatus")]    public string       LabelStatus    { get; set; } = string.Empty;
    [JsonPropertyName("population")]     public string       Population     { get; set; } = string.Empty;
    [JsonPropertyName("benefitSummary")] public string       BenefitSummary { get; set; } = string.Empty;
    [JsonPropertyName("evidenceTier")]   public string       EvidenceTier   { get; set; } = string.Empty;
    [JsonPropertyName("sources")]        public List<string> Sources        { get; set; } = new();
}

public sealed class DoseContext
{
    [JsonPropertyName("useCase")]     public string  UseCase     { get; set; } = string.Empty;
    [JsonPropertyName("population")]  public string  Population  { get; set; } = string.Empty;
    [JsonPropertyName("formulation")] public string? Formulation { get; set; }
    [JsonPropertyName("route")]       public string  Route       { get; set; } = string.Empty;
}

public sealed class Dose
{
    [JsonPropertyName("amount")]             public double? Amount             { get; set; }
    [JsonPropertyName("unit")]               public string  Unit               { get; set; } = string.Empty;
    [JsonPropertyName("frequency")]          public string  Frequency          { get; set; } = string.Empty;
    [JsonPropertyName("scheduleText")]       public string  ScheduleText       { get; set; } = string.Empty;
    [JsonPropertyName("preferredTimeOfDay")] public string? PreferredTimeOfDay { get; set; }
}

public sealed class DosingGuidance
{
    [JsonPropertyName("guidanceId")]      public string       GuidanceId      { get; set; } = string.Empty;
    [JsonPropertyName("context")]         public DoseContext  Context         { get; set; } = new();
    [JsonPropertyName("dose")]            public Dose         Dose            { get; set; } = new();
    [JsonPropertyName("notes")]           public List<string> Notes           { get; set; } = new();
    [JsonPropertyName("productSpecific")] public bool         ProductSpecific { get; set; }
    [JsonPropertyName("sources")]         public List<string> Sources         { get; set; } = new();
}
