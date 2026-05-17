namespace BioStack.KnowledgeWorker.Pipeline.Graph;

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public sealed record CompoundGraphNode(
    string NodeId,
    CompoundGraphNodeType NodeType,
    string Label,
    IReadOnlyList<string> Aliases,
    IReadOnlyDictionary<string, JsonNode?> Metadata);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompoundGraphNodeType
{
    Compound,
    Category,
    Mechanism,
    Pathway,
    Target,
    EffectDomain,
    RiskDomain,
    SourceFamily,
    Claim,
}
