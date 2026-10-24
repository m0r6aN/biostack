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

    // ── ClassB: nothing is blanked; unofficial fields are labeled. ─────────────
    // Owner ruling 9 (2026-10-24): remove do-not-display gates.

    [Fact]
    public void ClassB_Keeps_Regulatory_Canon_Labels_It_Unofficial_And_Flags_For_Review()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.Equal(TrustClass.B, result.RecordClass);
        Assert.Equal("approved", record.Regulatory.RegulatoryStatus);
        Assert.NotEmpty(record.Regulatory.ApprovedIndications);
        Assert.NotEmpty(record.Regulatory.LabelStatusByUseCase);
        Assert.True(record.Ops.NeedsReview);
        Assert.Contains("classB-only", record.Ops.QualityFlags);
        Assert.Empty(result.StrippedFields);
        Assert.Contains("regulatoryStatus", result.UnofficialFields);
        Assert.Contains("approvedIndications", result.UnofficialFields);
    }

    [Fact]
    public void ClassB_Keeps_Safety_Canon_And_Labels_It_Unofficial()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.NotEmpty(record.Safety.Contraindications);
        Assert.NotEmpty(record.Safety.Warnings);
        Assert.NotEmpty(record.Safety.Monitoring);
        Assert.Contains("contraindications", result.UnofficialFields);
        Assert.Contains("warnings", result.UnofficialFields);
        Assert.Contains("monitoring", result.UnofficialFields);
    }

    [Fact]
    public void ClassB_Keeps_ProductSpecific_Dosing_Guidance_Labeled_Unofficial()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.Contains(record.DosingGuidance, d => d.ProductSpecific);
        Assert.Contains("labelDosing", result.UnofficialFields);
    }

    [Fact]
    public void ClassB_Keeps_Blending_Rules_And_CompatibleBlends_Labeled_Unofficial()
    {
        var record = SubstanceRecordFactory.ClassBAttemptingSafetyCanon();

        var result = Gate.Apply(record);

        Assert.Contains(record.Compatibility.BlendingRules,
            br => br.Status.Equals("allowed", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(record.Compatibility.CompatibleBlends);
        Assert.Contains("compatibility", result.UnofficialFields);
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
