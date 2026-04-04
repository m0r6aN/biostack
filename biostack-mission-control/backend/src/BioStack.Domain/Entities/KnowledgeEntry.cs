namespace BioStack.Domain.Entities;

using BioStack.Domain.Enums;

public sealed class KnowledgeEntry
{
    public string CanonicalName { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();
    public CompoundCategory Classification { get; set; } = CompoundCategory.Unknown;
    public string RegulatoryStatus { get; set; } = string.Empty;
    public string MechanismSummary { get; set; } = string.Empty;
    public EvidenceTier EvidenceTier { get; set; } = EvidenceTier.Unknown;
    public List<string> SourceReferences { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public List<string> Pathways { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
    
    // Synergies and Blends
    public List<string> PairsWellWith { get; set; } = new();
    public List<string> AvoidWith { get; set; } = new();
    public List<string> CompatibleBlends { get; set; } = new();

    // Guidance
    public string RecommendedDosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string PreferredTimeOfDay { get; set; } = string.Empty;
    public List<string> WeeklyDosageSchedule { get; set; } = new();
    public List<string> DrugInteractions { get; set; } = new();

    // Optimization
    public string OptimizationProtein { get; set; } = string.Empty;
    public string OptimizationCarbs { get; set; } = string.Empty;
    public string OptimizationSupplements { get; set; } = string.Empty;
    public string OptimizationSleep { get; set; } = string.Empty;
    public string OptimizationExercise { get; set; } = string.Empty;
}
