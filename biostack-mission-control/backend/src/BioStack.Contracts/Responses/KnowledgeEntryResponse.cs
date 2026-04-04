namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record KnowledgeEntryResponse(
    string CanonicalName,
    List<string> Aliases,
    CompoundCategory Classification,
    string RegulatoryStatus,
    string MechanismSummary,
    EvidenceTier EvidenceTier,
    List<string> SourceReferences,
    string Notes,
    List<string> Pathways,
    List<string> Benefits,
    List<string> PairsWellWith,
    List<string> AvoidWith,
    List<string> CompatibleBlends,
    string RecommendedDosage,
    string Frequency,
    string PreferredTimeOfDay,
    List<string> WeeklyDosageSchedule,
    string DrugInteractions,
    string OptimizationProtein,
    string OptimizationCarbs,
    string OptimizationSupplements,
    string OptimizationSleep,
    string OptimizationExercise
);
