namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class SubstanceCanonicalizerClassificationTests
{
    private static readonly JsonSerializerOptions EnumJson = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Theory]
    [InlineData("Amino Acid", CompoundCategory.AminoAcid)]
    [InlineData("  Amino Acid  ", CompoundCategory.AminoAcid)]
    [InlineData("Research Compound", CompoundCategory.ResearchCompound)]
    [InlineData("  Research Compound  ", CompoundCategory.ResearchCompound)]
    public void MapClassification_PersistsApprovedSchemaStrings(string schemaValue, CompoundCategory expected)
    {
        var entry = Canonicalize(schemaValue);

        Assert.Equal(expected, entry.Classification);
        Assert.Equal(expected == CompoundCategory.AminoAcid ? 11 : 12, (int)entry.Classification);
    }

    [Theory]
    [InlineData("Small Molecule", CompoundCategory.SmallMolecule)]
    [InlineData("Biologic", CompoundCategory.Biologic)]
    [InlineData("Vitamin", CompoundCategory.Vitamin)]
    [InlineData("Mineral", CompoundCategory.Mineral)]
    [InlineData("Other", CompoundCategory.Other)]
    public void MapClassification_MapsEverySchemaClassification(string schemaValue, CompoundCategory expected)
    {
        Assert.Equal(expected, Canonicalize(schemaValue).Classification);
    }

    [Theory]
    [InlineData("small molecule")]
    [InlineData("amino acid")]
    [InlineData("research compound")]
    [InlineData("")]
    public void MapClassification_LeavesUnmappedSchemaStringsUnknown(string schemaValue)
    {
        var entry = Canonicalize(schemaValue);

        Assert.Equal(CompoundCategory.Unknown, entry.Classification);
        Assert.Equal(0, (int)entry.Classification);
    }

    [Fact]
    public void ExistingMembers_KeepIntegersZeroThroughTen_AndSerializeByName()
    {
        Assert.Equal(0, (int)CompoundCategory.Unknown);
        Assert.Equal(1, (int)CompoundCategory.Peptide);
        Assert.Equal(2, (int)CompoundCategory.Coenzyme);
        Assert.Equal(3, (int)CompoundCategory.Pharmaceutical);
        Assert.Equal(4, (int)CompoundCategory.Supplement);
        Assert.Equal(5, (int)CompoundCategory.Compound);
        Assert.Equal(6, (int)CompoundCategory.Sarm);
        Assert.Equal(7, (int)CompoundCategory.Serm);
        Assert.Equal(8, (int)CompoundCategory.Hormone);
        Assert.Equal(9, (int)CompoundCategory.Nutraceutical);
        Assert.Equal(10, (int)CompoundCategory.Other);
        Assert.Equal(11, (int)CompoundCategory.AminoAcid);
        Assert.Equal(12, (int)CompoundCategory.ResearchCompound);

        Assert.Equal("\"AminoAcid\"", JsonSerializer.Serialize(CompoundCategory.AminoAcid, EnumJson));
        Assert.Equal("\"ResearchCompound\"", JsonSerializer.Serialize(CompoundCategory.ResearchCompound, EnumJson));
        Assert.Equal("\"Sarm\"", JsonSerializer.Serialize(CompoundCategory.Sarm, EnumJson));
        Assert.Equal("\"Serm\"", JsonSerializer.Serialize(CompoundCategory.Serm, EnumJson));
    }

    [Theory]
    [InlineData("SARM", CompoundCategory.Sarm)]
    [InlineData("SERM", CompoundCategory.Serm)]
    public void MapClassification_KeepsExistingSchemaNames(string schemaValue, CompoundCategory expected)
    {
        Assert.Equal(expected, Canonicalize(schemaValue).Classification);
    }

    private static KnowledgeEntry Canonicalize(string schemaValue)
    {
        var record = SubstanceRecordFactory.Minimal("Glycine");
        record.Identity.Classification = schemaValue;
        return new SubstanceCanonicalizer().ToKnowledgeEntry(record);
    }
}
