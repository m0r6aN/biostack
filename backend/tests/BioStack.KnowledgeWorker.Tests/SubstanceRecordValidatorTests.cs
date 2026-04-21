namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class SubstanceRecordValidatorTests
{
    private static readonly string SchemaPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "substance-record.schema.json");

    private static readonly string SeedPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "substances-seed.json");

    private static SubstanceRecordValidator CreateValidator()
        => SubstanceRecordValidator.LoadFromFile(SchemaPath);

    /// <summary>
    /// The seed file shipped next to the worker is the canonical example of a
    /// schema-compliant record. If this fails, the production seed is drifting
    /// from the schema — fail loudly.
    /// </summary>
    [Fact]
    public void Validator_Accepts_The_Production_Seed_Records()
    {
        var validator = CreateValidator();
        var array     = (JsonArray)JsonNode.Parse(File.ReadAllText(SeedPath))!;

        Assert.NotEmpty(array);
        foreach (var node in array)
        {
            var result = validator.Validate(node!);
            Assert.True(
                result.IsValid,
                $"Seed record failed schema validation: {result.Summary()}");
        }
    }

    [Fact]
    public void Validator_Rejects_Record_Missing_Required_Identity_CanonicalName()
    {
        var node = LoadFirstSeedRecord();
        node["identity"]!.AsObject().Remove("canonicalName");

        var result = CreateValidator().Validate(node);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Keyword.Equals("required", StringComparison.OrdinalIgnoreCase)
            || e.Message.Contains("canonicalName", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_Rejects_Record_With_Invalid_RecordType_Enum()
    {
        var node = LoadFirstSeedRecord();
        node["recordType"] = "not-a-real-record-type";

        var result = CreateValidator().Validate(node);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validator_Surfaces_Instance_Location_And_Keyword_For_Each_Error()
    {
        var node = LoadFirstSeedRecord();
        node["identity"]!.AsObject().Remove("canonicalName");
        node["recordType"] = "invalid";

        var result = CreateValidator().Validate(node);

        Assert.False(result.IsValid);
        Assert.All(result.Errors, err =>
        {
            Assert.False(string.IsNullOrWhiteSpace(err.Location));
            Assert.False(string.IsNullOrWhiteSpace(err.Keyword));
        });
    }

    private static JsonNode LoadFirstSeedRecord()
    {
        var array = (JsonArray)JsonNode.Parse(File.ReadAllText(SeedPath))!;
        // Return a fresh parse each time so tests don't mutate each other.
        var first = array[0]!.ToJsonString();
        return JsonNode.Parse(first)!;
    }
}
