namespace BioStack.Contracts.Responses;

using BioStack.Domain.Entities;
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
    string VialCompatibility,
    string RecommendedDosage,
    string StandardDosageRange,
    string MaxReportedDose,
    string Frequency,
    string PreferredTimeOfDay,
    List<string> WeeklyDosageSchedule,
    List<string> IncrementalEscalationSteps,
    TieredDosingData? TieredDosing,
    List<string> DrugInteractions,
    string OptimizationProtein,
    string OptimizationCarbs,
    List<string> OptimizationSupplements,
    string OptimizationSleep,
    string OptimizationExercise
);
