namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;

public sealed class LocalKnowledgeSource : IKnowledgeSource
{
    private readonly List<KnowledgeEntry> _knowledgeBase;

    public LocalKnowledgeSource()
    {
        _knowledgeBase = InitializeKnowledgeBase();
    }

    public Task<KnowledgeEntry?> GetCompoundAsync(string name, CancellationToken cancellationToken = default)
    {
        var entry = _knowledgeBase.FirstOrDefault(k =>
            k.CanonicalName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            k.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(entry);
    }

    public Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<KnowledgeEntry>(_knowledgeBase));
    }

    public Task<List<KnowledgeEntry>> SearchCompoundsByPathwayAsync(string pathway, CancellationToken cancellationToken = default)
    {
        var results = _knowledgeBase
            .Where(k => k.Pathways.Any(p => p.Equals(pathway, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult(results);
    }

    public Task UpsertCompoundAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = _knowledgeBase.FirstOrDefault(k => k.CanonicalName == entry.CanonicalName);
        if (existing != null) _knowledgeBase.Remove(existing);
        _knowledgeBase.Add(entry);
        return Task.CompletedTask;
    }

    public Task<int> IngestBulkAsync(List<KnowledgeEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            var existing = _knowledgeBase.FirstOrDefault(k => k.CanonicalName == entry.CanonicalName);
            if (existing != null) _knowledgeBase.Remove(existing);
            _knowledgeBase.Add(entry);
        }
        return Task.FromResult(entries.Count);
    }

    private static List<KnowledgeEntry> InitializeKnowledgeBase()
    {
        return new List<KnowledgeEntry>
        {
            new KnowledgeEntry
            {
                CanonicalName = "BPC-157",
                Aliases = new List<string> { "Body Protection Compound-157", "PL-14736" },
                Classification = CompoundCategory.Peptide,
                RegulatoryStatus = "Research-grade peptide",
                MechanismSummary = "Gastric pentadecapeptide implicated in tissue repair and protective mechanisms across multiple organ systems",
                EvidenceTier = EvidenceTier.Moderate,
                SourceReferences = new List<string>
                {
                    "Sikiric et al. - Pentadecapeptide BPC-157 research",
                    "Multiple in-vitro and animal model studies"
                },
                Notes = "Educational reference only. Not a therapeutic recommendation.",
                Pathways = new List<string> { "tissue-repair", "gi-protective", "angiogenesis" },
                Benefits = new List<string> { "improved healing", "gut health", "injury recovery", "cognitive enhancement" },
                PairsWellWith = new List<string> { "TB-500", "Growth Hormone Secretagogues" },
                RecommendedDosage = "250-500 mcg",
                Frequency = "Twice daily",
                PreferredTimeOfDay = "Morning and Evening",
                WeeklyDosageSchedule = new List<string> { "Week 1-8: 500mcg daily", "Week 9-10: Break" },
                OptimizationProtein = "1.2g per lb body weight; sources: Whey, Beef, Eggs",
                OptimizationSleep = "7-9 hours; crucial for tissue repair",
                OptimizationExercise = "Isometric holds or light mobility; 5,000-8,000 steps"
            },
            new KnowledgeEntry
            {
                CanonicalName = "TB-500",
                Aliases = new List<string> { "Thymosin Beta-4 Fragment", "Tβ4" },
                Classification = CompoundCategory.Peptide,
                RegulatoryStatus = "Research-grade peptide",
                MechanismSummary = "Synthetic fragment of naturally-occurring thymosin beta-4, associated with tissue repair and inflammatory modulation",
                EvidenceTier = EvidenceTier.Moderate,
                SourceReferences = new List<string>
                {
                    "Thymosin research literature",
                    "Tissue repair mechanism studies"
                },
                Notes = "Educational reference only. Not a therapeutic recommendation.",
                Pathways = new List<string> { "tissue-repair", "anti-inflammatory", "cell-migration" },
                Benefits = new List<string> { "improved healing", "reduced inflammation", "tissue repair", "anti-aging" },
                PairsWellWith = new List<string> { "BPC-157" },
                RecommendedDosage = "2-5 mg",
                Frequency = "Twice weekly",
                WeeklyDosageSchedule = new List<string> { "Week 1-4: 5mg (Loading)", "Week 5-8: 2mg (Maintenance)" }
            },
            new KnowledgeEntry
            {
                CanonicalName = "MOTS-C",
                Aliases = new List<string> { "Mitochondrial Open Reading Frame of the Twelve S rRNA-c" },
                Classification = CompoundCategory.Peptide,
                RegulatoryStatus = "Research-grade peptide",
                MechanismSummary = "Mitochondrial-derived peptide with proposed metabolic and mitochondrial function effects",
                EvidenceTier = EvidenceTier.Limited,
                SourceReferences = new List<string>
                {
                    "Lee et al. - Mitochondrial peptide research",
                    "Metabolic regulation studies"
                },
                Notes = "Educational reference only. Not a therapeutic recommendation.",
                Pathways = new List<string> { "metabolic-regulation", "insulin-sensitivity", "exercise-mimetic" },
                Benefits = new List<string> { "metabolic health", "energy", "insulin sensitivity", "anti-aging", "cognitive enhancement" },
                PairsWellWith = new List<string> { "NAD+", "Aicar" },
                AvoidWith = new List<string> { "High sugar intake" },
                CompatibleBlends = new List<string> { "Bacteriostatic Water" },
                RecommendedDosage = "5 mg",
                Frequency = "Three times per week",
                PreferredTimeOfDay = "Morning, pre-exercise",
                WeeklyDosageSchedule = new List<string> { "Week 1-4: 5mg 3x/week", "Week 5: Break" },
                OptimizationProtein = "1.0g per lb body weight; sources: Chicken, Fish, Plant-based",
                OptimizationCarbs = "0.8g per lb body weight; sources: Rice, Sweet Potato, Fruit",
                OptimizationExercise = "Resistance training (45 mins) + 8,000-10,000 steps",
                OptimizationSupplements = new List<string> { "Creatine (5g)", "CoQ10 (200mg)" }
            },
            new KnowledgeEntry
            {
                CanonicalName = "NAD+",
                Aliases = new List<string> { "Nicotinamide Adenine Dinucleotide", "Nicotinamide mononucleotide" },
                Classification = CompoundCategory.Coenzyme,
                RegulatoryStatus = "Endogenous coenzyme",
                MechanismSummary = "Essential coenzyme involved in cellular energy metabolism, DNA repair, and sirtuin activation pathways",
                EvidenceTier = EvidenceTier.Strong,
                SourceReferences = new List<string>
                {
                    "Imai & Guarente - NAD+ biology",
                    "Cellular energetics literature"
                },
                Notes = "Educational reference only. Not a therapeutic recommendation.",
                Pathways = new List<string> { "cellular-energy", "dna-repair", "sirtuin-activation" },
                Benefits = new List<string> { "anti-aging", "cellular energy", "DNA repair" },
                PairsWellWith = new List<string> { "MOTS-C", "Resveratrol" },
                RecommendedDosage = "100-500 mg",
                Frequency = "Daily",
                PreferredTimeOfDay = "Morning",
                OptimizationSupplements = new List<string> { "Quercetin", "Folate (400mcg)", "Vitamin D (5000 IU)" }
            },
            new KnowledgeEntry
            {
                CanonicalName = "Retatrutide",
                Aliases = new List<string> { "Triple GLP-1/GIP/Glucagon agonist", "LY3437943" },
                Classification = CompoundCategory.Pharmaceutical,
                RegulatoryStatus = "Investigational drug",
                MechanismSummary = "Triple receptor agonist targeting GLP-1, GIP, and glucagon receptors for metabolic effects",
                EvidenceTier = EvidenceTier.Limited,
                SourceReferences = new List<string>
                {
                    "Eli Lilly clinical research",
                    "Metabolic syndrome studies"
                },
                Notes = "Educational reference only. Not a therapeutic recommendation. Clinical studies ongoing.",
                Pathways = new List<string> { "glucose-regulation", "appetite-regulation", "metabolic" },
                Benefits = new List<string> { "weight loss", "blood sugar control", "metabolic health" },
                RecommendedDosage = "2 mg",
                Frequency = "Weekly",
                WeeklyDosageSchedule = new List<string> { "Week 1-4: 2mg", "Week 5-8: 4mg", "Week 9+: Up to 12mg" },
                OptimizationProtein = "1.5g per lb body weight; crucial to prevent muscle loss",
                OptimizationExercise = "Heavy resistance training; 10,000 steps"
            }
        };
    }
}
