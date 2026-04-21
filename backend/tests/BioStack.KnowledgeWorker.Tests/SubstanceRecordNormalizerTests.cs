namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class SubstanceRecordNormalizerTests
{
    private static readonly ISubstanceRecordNormalizer Normalizer = new SubstanceRecordNormalizer();

    [Fact]
    public void Normalize_Generates_Slug_And_CanonicalId_From_CanonicalName_When_Missing()
    {
        var rec = SubstanceRecordFactory.Minimal("BPC 157");
        rec.Identity.CanonicalId = string.Empty;
        rec.Identity.Slug        = string.Empty;

        Normalizer.Normalize(rec);

        Assert.Equal("bpc-157", rec.Identity.CanonicalId);
        Assert.Equal("bpc-157", rec.Identity.Slug);
    }

    [Fact]
    public void Normalize_Trims_Whitespace_On_CanonicalName()
    {
        var rec = SubstanceRecordFactory.Minimal("  Tesamorelin  ");

        Normalizer.Normalize(rec);

        Assert.Equal("Tesamorelin", rec.Identity.CanonicalName);
        Assert.Equal("tesamorelin", rec.Identity.CanonicalId);
    }

    [Fact]
    public void Normalize_Dedupes_Aliases_Case_Insensitively()
    {
        var rec = SubstanceRecordFactory.Minimal("Tesamorelin");
        rec.Identity.Aliases = new List<string> { "TH9507", "th9507", "Tesamorelin Acetate", "  " };

        Normalizer.Normalize(rec);

        Assert.Equal(2, rec.Identity.Aliases.Count);
        Assert.Contains("TH9507", rec.Identity.Aliases);
        Assert.Contains("Tesamorelin Acetate", rec.Identity.Aliases);
    }

    [Fact]
    public void Normalize_Removes_CanonicalName_From_Aliases_Bucket()
    {
        var rec = SubstanceRecordFactory.Minimal("Tesamorelin");
        rec.Identity.Aliases = new List<string> { "Tesamorelin", "TH9507" };

        Normalizer.Normalize(rec);

        Assert.DoesNotContain("Tesamorelin", rec.Identity.Aliases);
        Assert.Contains("TH9507", rec.Identity.Aliases);
    }

    [Fact]
    public void Normalize_Moves_Alias_To_Brand_When_Claimed_By_Both_Buckets()
    {
        var rec = SubstanceRecordFactory.Minimal("Tesamorelin");
        rec.Identity.Aliases    = new List<string> { "Egrifta", "TH9507" };
        rec.Identity.BrandNames = new List<string> { "Egrifta" };

        Normalizer.Normalize(rec);

        Assert.DoesNotContain("Egrifta", rec.Identity.Aliases);
        Assert.Contains("Egrifta",        rec.Identity.BrandNames);
        Assert.Contains("TH9507",         rec.Identity.Aliases);
    }

    [Fact]
    public void Normalize_Lowercases_Enum_Like_Strings_In_Ops()
    {
        var rec = SubstanceRecordFactory.Minimal("Tesamorelin");
        rec.Ops.Completeness   = "PARTIAL";
        rec.Ops.LastChangeType = "Seed";

        Normalizer.Normalize(rec);

        Assert.Equal("partial", rec.Ops.Completeness);
        Assert.Equal("seed",    rec.Ops.LastChangeType);
    }

    [Fact]
    public void Normalize_Replaces_Null_String_Lists_With_Empty_Lists()
    {
        var rec = SubstanceRecordFactory.Minimal("Tesamorelin");
        rec.Regulatory.OffLabelNotes = null!;
        rec.Ops.ReviewReasons        = null!;
        rec.Ops.QualityFlags         = null!;

        Normalizer.Normalize(rec);

        Assert.NotNull(rec.Regulatory.OffLabelNotes);
        Assert.Empty(rec.Regulatory.OffLabelNotes);
        Assert.NotNull(rec.Ops.ReviewReasons);
        Assert.NotNull(rec.Ops.QualityFlags);
    }

    [Fact]
    public void Normalize_Is_Idempotent()
    {
        var rec = SubstanceRecordFactory.Minimal("BPC 157");
        rec.Identity.Aliases = new List<string> { "BPC157", "bpc157", "bpc 157" };

        Normalizer.Normalize(rec);
        var firstPassSlug    = rec.Identity.Slug;
        var firstPassAliases = rec.Identity.Aliases.Count;

        Normalizer.Normalize(rec);

        Assert.Equal(firstPassSlug,    rec.Identity.Slug);
        Assert.Equal(firstPassAliases, rec.Identity.Aliases.Count);
    }
}
