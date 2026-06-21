using Xunit;

namespace BioStack.KnowledgeWorker.Tests;

public sealed class KnowledgeEngineDocumentationTests
{
    [Fact]
    public void RequiredArchitectureDocumentsExist()
    {
        var root = RepositoryRoot();
        var requiredFiles = new[]
        {
            "docs/architecture/adr-biostack-source-first-knowledge-engine.md",
            "docs/architecture/model-data-asset-utilization-matrix.md",
            "docs/product/knowledge-engine-model-data-roadmap.md",
            "docs/architecture/source-registry-schema.md",
            "docs/product/knowledge-engine-capability-map.md",
            "docs/testing/knowledge-engine-evaluation-harness.md",
            "docs/knowledge-engine/protocol-intelligence-safety-guardrails.md",
            "research/protocol-intelligence/biostack-ai-model-data-asset-research-memo.md"
        };

        foreach (var relativePath in requiredFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, relativePath)), $"Missing required documentation file: {relativePath}");
        }
    }

    [Fact]
    public void UtilizationMatrixCoversRequiredAssetsAndStatuses()
    {
        var matrix = ReadDoc("docs/architecture/model-data-asset-utilization-matrix.md");
        var requiredAssets = new[]
        {
            "PubMed/PMC",
            "MedCPT",
            "DailyMed/openFDA drug labels",
            "openFDA/FAERS",
            "ClinicalTrials.gov",
            "PubTator3",
            "RxNorm/RxNav/RxClass/MED-RT",
            "ChEMBL",
            "Reactome",
            "Open Targets",
            "PubChem",
            "NIH ODS Fact Sheets",
            "NIH DSLD",
            "WADA Prohibited List",
            "OPSS",
            "DrugBank",
            "NatMed",
            "SapBERT",
            "BiomedBERT/PubMedBERT",
            "BioLinkBERT",
            "SPECTER2",
            "scispaCy",
            "Stanza biomedical models",
            "HunFlair2",
            "BioMistral",
            "Meditron",
            "BioGPT",
            "GatorTron",
            "MIMIC-IV/PhysioNet",
            "SemMedDB",
            "SIDER",
            "OffSIDES/TwoSIDES/nSIDES",
            "Global DRO"
        };

        foreach (var asset in requiredAssets)
        {
            Assert.Contains(asset, matrix, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var status in new[] { "Use now", "Test next", "License review", "Benchmark only", "Avoid" })
        {
            Assert.Contains(status, matrix, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CapabilityMapCoversRequiredCapabilities()
    {
        var map = ReadDoc("docs/product/knowledge-engine-capability-map.md");
        var capabilities = new[]
        {
            "Evidence Confidence Overlay",
            "Phase-Aware Protocol Graph",
            "Side-Effect Ambiguity Detector",
            "Source Quality Tracker",
            "GLP-1 Observability Pack",
            "High-Risk Category Guardrails",
            "Research Gap Alerts",
            "Biomarker Prompt Generator",
            "Protocol Complexity Score",
            "Regulatory Status Awareness",
            "Human Review Queue"
        };

        foreach (var capability in capabilities)
        {
            Assert.Contains(capability, map, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SourceRegistryExamplesIncludeLicenseAndUseBoundaries()
    {
        var schema = ReadDoc("docs/architecture/source-registry-schema.md");
        var examples = new[]
        {
            "PubMed",
            "DailyMed",
            "openFDA drug label",
            "FAERS/openFDA drug event",
            "ClinicalTrials.gov",
            "NIH ODS Fact Sheets",
            "WADA Prohibited List",
            "DrugBank",
            "NatMed",
            "ChEMBL",
            "Reactome"
        };

        foreach (var example in examples)
        {
            Assert.Contains(example, schema, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var requiredField in new[] { "\"license\"", "\"allowedUse\"", "\"disallowedUse\"", "\"redistributionConstraints\"" })
        {
            Assert.Contains(requiredField, schema, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EvaluationHarnessRequiresRefusalUnsafeContentAndLicenseTests()
    {
        var harness = ReadDoc("docs/testing/knowledge-engine-evaluation-harness.md");

        foreach (var phrase in new[] { "Refusal behavior", "Unsafe content suppression", "License boundary enforcement" })
        {
            Assert.Contains(phrase, harness, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ArchitecturePositionsBioStackAsEvidenceGuidedProtocolIntelligence()
    {
        var adr = ReadDoc("docs/architecture/adr-biostack-source-first-knowledge-engine.md");
        var matrix = ReadDoc("docs/architecture/model-data-asset-utilization-matrix.md");
        var roadmap = ReadDoc("docs/product/knowledge-engine-model-data-roadmap.md");
        var harness = ReadDoc("docs/testing/knowledge-engine-evaluation-harness.md");

        foreach (var document in new[] { adr, matrix, roadmap, harness })
        {
            Assert.Contains("Evidence-Guided Protocol Intelligence Engine", document, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("evidence-informed educational guidance", adr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("risk-aware recommendations", adr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("decision support", adr, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rejects autonomous medical authority, prescribing, diagnosis, and treatment planning", adr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GuardrailsAllowEducationalGuidanceButBlockMedicalAuthority()
    {
        var guardrails = ReadDoc("docs/knowledge-engine/protocol-intelligence-safety-guardrails.md");
        var allowedGuidance = new[]
        {
            "Evidence-context recommendations",
            "Risk-aware educational guidance",
            "Tracking and baseline recommendations",
            "Source-quality warnings",
            "Regulatory-status warnings",
            "Protocol complexity warnings",
            "Side-effect ambiguity analysis",
            "Research-gap alerts",
            "Clinician-escalation suggestions",
            "Safer decision pathways"
        };

        foreach (var phrase in allowedGuidance)
        {
            Assert.Contains(phrase, guardrails, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var phrase in new[] { "Medical authority", "Prescribing recommendations", "Start, stop, taper, or escalation instructions" })
        {
            Assert.Contains(phrase, guardrails, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RetatrutideHandlingIsInvestigationalAndNotInstructional()
    {
        var adr = ReadDoc("docs/architecture/adr-biostack-source-first-knowledge-engine.md");
        var guardrails = ReadDoc("docs/knowledge-engine/protocol-intelligence-safety-guardrails.md");
        var harness = ReadDoc("docs/testing/knowledge-engine-evaluation-harness.md");

        foreach (var document in new[] { adr, guardrails, harness })
        {
            Assert.Contains("Retatrutide", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("investigational", document, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("not FDA-approved for public use", guardrails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Trial exposure data may be cited as research context, not user instructions", guardrails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gray-market products must trigger source-quality and identity-risk warnings", guardrails, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Influencer claims must be classified as market signal only", guardrails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UserFacingWordingRulesPreferBioStackGuidanceAndAvoidAiInstructionPhrases()
    {
        var guardrails = ReadDoc("docs/knowledge-engine/protocol-intelligence-safety-guardrails.md");
        var harness = ReadDoc("docs/testing/knowledge-engine-evaluation-harness.md");

        foreach (var phrase in new[] { "BioStack guidance", "Evidence context", "Risk signal", "What to track", "What changed", "What is uncertain", "Discuss with a qualified clinician" })
        {
            Assert.Contains(phrase, guardrails, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var phrase in new[] { "AI recommends", "AI says you should", "AI's best guess", "The best dose for you", "You should take", "Start this", "Increase this", "Stop this" })
        {
            Assert.Contains(phrase, guardrails, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(phrase, harness, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GuardrailDocsProhibitUnsafeMedicalAndHighRiskGuidance()
    {
        var guardrails = ReadDoc("docs/knowledge-engine/protocol-intelligence-safety-guardrails.md");
        var prohibited = new[]
        {
            "Individualized medical dosing",
            "Diagnosis",
            "Treatment plans",
            "SARM cycle",
            "SERM cycle",
            "Post-cycle therapy",
            "Peptide injection instructions",
            "Sourcing guidance"
        };

        foreach (var phrase in prohibited)
        {
            Assert.Contains(phrase, guardrails, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReadDoc(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) &&
                Directory.Exists(Path.Combine(directory.FullName, "research")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the BioStack repository root.");
    }
}
