namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class TrustGateTests
{
    private static readonly ITrustGate Gate = new TrustGate();

    // ── ClassA: authoritative. Nothing is stripped. ─────────────────────────────

    [Fact]
    public void ClassA_Record_Is_Passed_Through_Unchanged()
    {
        var record = SubstanceRecordFactory.ClassAWithSafetyCanon();

        var result = Gate.Apply(record);

        Assert.Equal(TrustClass.A, result.RecordClass);
        Assert.Empty(result.StrippedFields);
        Assert.Empty(result.ReviewReasons);
        Assert.False(record.Ops.NeedsReview);
    }

    [Fact]
    public void ClassA_Keeps_Regulatory_Safety_And_ProductSpecific_Dosing()
    {
        var record = SubstanceRecordFactory.ClassAWithSafetyCanon();

        Gate.Apply(record);

        Assert.Equal("approved",         record.Regulatory.RegulatoryStatus);
        Assert.NotEmpty(record.Regulatory.ApprovedIndications);
        Assert.NotEmpty(record.Regulatory.LabelStatusByUseCase);
        Assert.NotEmpty(record.Safety.Contraindications);
        Assert.NotEmpty(record.Safety.Warnings);
        Assert.NotEmpty(record.Safety.Monitoring);
        Assert.Contains(record.DosingGuidance, d => d.ProductSpecific);
        Assert.NotEmpty(record.Compatibility.CompatibleBlends);
    }

    // ── ClassB: enrichment-only. Canonical truth is stripped. ───────────────────

    [Fact]
    public void ClassB_Strips_Regulatory_Canon_And_Flags_For_Review()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.Equal(TrustClass.B, result.RecordClass);
        Assert.Equal(string.Empty, record.Regulatory.RegulatoryStatus);
        Assert.Empty(record.Regulatory.ApprovedIndications);
        Assert.Empty(record.Regulatory.LabelStatusByUseCase);
        Assert.True(record.Ops.NeedsReview);
        Assert.Contains("classB-only", record.Ops.QualityFlags);
        Assert.Contains(result.StrippedFields, f => f.StartsWith("regulatory."));
    }

    [Fact]
    public void ClassB_Strips_Safety_Canon_Contraindications_Warnings_And_Monitoring()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.Empty(record.Safety.Contraindications);
        Assert.Empty(record.Safety.Warnings);
        Assert.Empty(record.Safety.Monitoring);
        Assert.Contains("safety.contraindications", result.StrippedFields);
        Assert.Contains("safety.warnings",          result.StrippedFields);
        Assert.Contains("safety.monitoring",        result.StrippedFields);
    }

    [Fact]
    public void ClassB_Strips_ProductSpecific_Dosing_Guidance()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.DoesNotContain(record.DosingGuidance, d => d.ProductSpecific);
        Assert.Contains(result.StrippedFields, f => f.StartsWith("dosingGuidance.productSpecific"));
    }

    [Fact]
    public void ClassB_Strips_Allowed_Blending_Rules_And_CompatibleBlends()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.DoesNotContain(record.Compatibility.BlendingRules,
            br => br.Status.Equals("allowed", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(record.Compatibility.CompatibleBlends);
        Assert.Contains(result.StrippedFields, f => f.StartsWith("compatibility."));
    }

    [Fact]
    public void ClassB_With_Mixed_Sources_Is_Promoted_To_ClassA_If_One_Source_Is_ClassA()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();
        record.Provenance.SourceRecords.Add(new BioStack.KnowledgeWorker.Models.SourceRecord
        {
            SourceType = "manufacturer",
            Title      = "Manufacturer label (authoritative)",
        });

        var result = Gate.Apply(record);

        Assert.Equal(TrustClass.A, result.RecordClass);
        Assert.Empty(result.StrippedFields);
    }

    [Fact]
    public void Record_With_No_Provenance_Sources_Defaults_To_ClassB()
    {
        var record = SubstanceRecordFactory.ClassAWithSafetyCanon();
        record.Provenance.SourceRecords.Clear();

        var result = Gate.Apply(record);

        Assert.Equal(TrustClass.B, result.RecordClass);
        Assert.True(record.Ops.NeedsReview);
    }
}
