namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
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
        new object[] { "source-authorization-decision.schema.json", "source-authorization-decision-batch" },
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

    [Fact]
    public void Source_Authorization_Decision_Schema_Is_Registered_And_Bundled()
    {
        var descriptor = ResearchArtifactSchemas.Get(ResearchArtifactKind.SourceAuthorizationDecisionBatch);

        Assert.Equal("source-authorization-decision.schema.json", descriptor.SchemaFileName);
        Assert.Equal("source-authorization-decision-batch", descriptor.RecordType);
        Assert.True(
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Schemas", descriptor.SchemaFileName)),
            $"Expected bundled schema '{descriptor.SchemaFileName}' under the test output Schemas directory.");
    }
}
