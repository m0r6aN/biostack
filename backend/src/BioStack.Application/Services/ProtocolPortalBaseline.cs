namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;

/// <summary>
/// Versioned, in-code curated baseline ("knowledge_baseline" source) for the
/// Protocol Portal. Supplies ONLY educational / narrative sections — diet
/// framework, supplement education, monitoring rules, milestone expectations and
/// resources. It NEVER supplies operational data (stats, today/week schedule,
/// labs, weight, adherence); those must come from real entities or honest
/// empty-states. Content is reference copy adapted from the frontend mock.
/// </summary>
public interface IProtocolPortalBaseline
{
    string Version { get; }
    DietFrameworkResponse Diet { get; }
    SupplementPlanResponse Supplements { get; }
    MonitoringProtocolResponse Monitoring { get; }
    IReadOnlyList<ResourceEntryResponse> Resources { get; }

    /// <summary>
    /// Baseline milestone expectations. These are educational period descriptions;
    /// the composer marks which milestone is current from real phase/progress data.
    /// </summary>
    IReadOnlyList<MilestoneResponse> Milestones { get; }
}

public sealed class ProtocolPortalBaseline : IProtocolPortalBaseline
{
    public string Version => "2026.06.0";

    public DietFrameworkResponse Diet { get; } = new(
        Title: "Diet & Lifestyle Framework",
        Summary: "High-protein, anti-inflammatory, nutrient-dense foundation",
        Targets: new List<DietTargetResponse>
        {
            new("Protein", "1.8–2.2 g/kg ideal body weight", null),
            new("Fiber", "35–45 g", null),
            new("Hydration", "3.5–4.5 L", null),
            new("Alcohol", "Minimize / Eliminate", true),
        },
        Rationale:
            "Preserves lean mass during incretin-driven fat loss, supports liver detoxification, "
            + "reduces inflammation (enhancing mitochondrial and NAD+ effectiveness), and provides "
            + "stable energy.",
        Lifestyle: new List<string>
        {
            "Resistance training 3–4× per week",
            "Zone 2 cardio (supports natural mitochondrial signaling)",
            "7–9 hours quality sleep nightly",
            "Stress management & circadian alignment",
        });

    public SupplementPlanResponse Supplements { get; } = new(
        Title: "Daily Supplementation Guide",
        Summary: "Foundational educational support — general guidance, not a prescription",
        Entries: new List<SupplementEntryResponse>
        {
            new("L-Carnitine", "3 g daily", "Fatty acid transport · synergizes with mitochondrial peptides", null),
            new("Phosphatidylcholine", "3 g daily", "Cell membrane integrity & liver fat export", null),
            new("Vitamin E", "400 IU daily", "Protects mitochondrial & liver membranes", null),
            new("Selenium", "200 mcg daily", "Glutathione peroxidase support", null),
            new("TUDCA", "500 mg with 2 largest meals (1,000 mg total)",
                "Commonly highlighted for biliary protection on incretin therapy", true),
            new("NAC", "1,000 mg daily", "Glutathione replenishment & oxidative stress reduction", null),
            new("Methyl B-Complex + B12 + Folate", "daily", "Liver methylation & detoxification", null),
            new("Magnesium Glycinate", "500 mg (evening)", "Sleep quality + enzymatic cofactor", null),
            new("Zinc", "30 mg daily", "Liver detoxification enzyme support", null),
        },
        Additional: new List<string> { "Omega-3 (2–4 g)", "Creatine 5 g", "CoQ10 200–300 mg" });

    public MonitoringProtocolResponse Monitoring { get; } = new(
        BaselineCompleted:
            "Full CMP, lipid panel, HbA1c, HOMA-IR, GGT, ferritin, Vitamin D, and liver imaging "
            + "are typical baseline references for this class of protocol.",
        RecurringCadence: "Every 5–6 weeks",
        RecurringLabs: new List<string>
        {
            "Fasting insulin + glucose + HOMA-IR",
            "GGT, ALT, AST",
            "Triglycerides & TG/HDL ratio",
        },
        AdjustmentRules: new List<AdjustmentRuleResponse>
        {
            new("GGT rising", "Discuss reducing the primary compound dose and reassess in 5–6 weeks."),
            new("TG/HDL ratio worsening",
                "Discuss increasing L-Carnitine and protein, and reducing the primary compound dose."),
        });

    public IReadOnlyList<MilestoneResponse> Milestones { get; } = new List<MilestoneResponse>
    {
        new(1, "Weeks 1–4",
            "Appetite regulation, early fat loss, stabilizing liver markers, improved recovery.", null),
        new(2, "Weeks 5–12",
            "More pronounced fat loss, visible recomposition, muscle preservation, improved insulin sensitivity.", null),
        new(3, "Month 4+",
            "Optimized body composition, sustainable metabolic health, foundational longevity support.", null),
    };

    public IReadOnlyList<ResourceEntryResponse> Resources { get; } = new List<ResourceEntryResponse>
    {
        new("About Emerging Compounds",
            "Research peptides such as BRP (a BRINP2-related peptide discovered in 2025) target "
            + "appetite via hypothalamic pathways distinct from GLP-1 agonists. Early data is "
            + "promising but these remain investigational and are not part of an active protocol."),
        new("Protocol Philosophy",
            "A well-designed system is deliberately layered and governed. Each compound either "
            + "protects the system (e.g. liver support) or complements the others. Observations are "
            + "compared by phase rather than assuming a single compound explains any change."),
    };
}
