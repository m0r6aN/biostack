namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using Xunit;

public class ResearchSchemaFilesTests
{
    public static IEnumerable<object[]> ResearchSchemas => new[]
    {
        new object[] { "compound-candidate.schema.json", "compound-candidate-batch" },
        new object[] { "source-registry.schema.json", "source-registry" },
        new object[] { "evidence-packet.schema.json", "compound-evidence-packet" },
        new object[] { "review-decision.schema.json", "review-decision-batch" },
        new object[] { "research-request.schema.json", "research-request-batch" },
    };

    [Theory]
    [MemberData(nameof(ResearchSchemas))]
    public void Research_Schema_File_Is_Valid_Json_With_RecordType_Const(string fileName, string recordType)
    {
        var path = Path.Combine(TestPaths.WorkerSchemaDirectory(), fileName);

        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", (string?)root["$schema"]);
        Assert.Equal(recordType, (string?)root["properties"]!["recordType"]!["const"]);
    }
}