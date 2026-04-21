namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Test builders for <see cref="SubstanceRecord"/>. Produces minimal schema-valid
/// records so unit tests do not need to hand-roll large JSON blobs.
/// </summary>
internal static class SubstanceRecordFactory
{
    public static SubstanceRecord ClassAWithSafetyCanon(string canonicalName = "Tesamorelin")
    {
        var rec = Minimal(canonicalName);

        rec.Regulatory.RequiresPrescription = true;
        rec.Regulatory.RegulatoryStatus     = "approved";
        rec.Regulatory.Jurisdiction         = "US";
        rec.Regulatory.ApprovedIndications.Add("HIV-associated lipodystrophy");
        rec.Regulatory.LabelStatusByUseCase.Add(new LabelStatusByUseCaseItem
        {
            UseCase     = "HIV-associated lipodystrophy",
            LabelStatus = "on-label",
        });

        rec.Safety.Contraindications.Add(new SafetyItem
        {
            Item     = "Active malignancy",
            Severity = "absolute",
            Sources  = { "label:egrifta" },
        });
        rec.Safety.Warnings.Add(new SafetyItem
        {
            Item     = "Glucose intolerance",
            Severity = "high",
            Sources  = { "label:egrifta" },
        });
        rec.Safety.Monitoring.Add(new MonitoringItem
        {
            Parameter = "IGF-1",
            Reason    = "Titrate to safe exposure",
            Sources   = { "label:egrifta" },
        });

        rec.DosingGuidance.Add(new DosingGuidance
        {
            GuidanceId      = "tesamorelin-egrifta-standard",
            Context         = new DoseContext { UseCase = "HIV-lipodystrophy", Population = "adult", Route = "sc" },
            Dose            = new Dose { Amount = 2, Unit = "mg", Frequency = "qd", ScheduleText = "Once daily before bed" },
            ProductSpecific = true,
        });

        rec.Compatibility.BlendingRules.Add(new BlendingRule
        {
            RuleType = "reconstitution",
            Target   = "bacteriostatic-water",
            Status   = "allowed",
            Message  = "Approved diluent per label",
        });
        rec.Compatibility.CompatibleBlends.Add("bacteriostatic-water");

        rec.Provenance.SourceRecords.Add(new SourceRecord
        {
            SourceType = "manufacturer",
            Title      = "Egrifta SV Prescribing Information",
            Publisher  = "Theratechnologies",
            Url        = "https://example.invalid/egrifta-label.pdf",
        });

        return rec;
    }

    public static SubstanceRecord ClassBAttemptingSafetyCanon(string canonicalName = "Tesamorelin")
    {
        var rec = ClassAWithSafetyCanon(canonicalName);
        rec.Provenance.SourceRecords.Clear();
        rec.Provenance.SourceRecords.Add(new SourceRecord
        {
            SourceType = "paper",
            Title      = "Observational review of GHRH analogs",
            Publisher  = "Third-party journal",
            Url        = "https://example.invalid/paper-001.pdf",
        });
        return rec;
    }

    public static SubstanceRecord Minimal(string canonicalName)
    {
        return new SubstanceRecord
        {
            SchemaVersion = "1.0.0",
            RecordType    = "substance",
            Identity      = new Identity
            {
                CanonicalName  = canonicalName,
                Classification = "Peptide",
                CompoundFamily = "Test family",
            },
            Ops = new Ops
            {
                IngestionSource = "test-harness",
                IsActive        = true,
                Completeness    = "partial",
            },
        };
    }
}
