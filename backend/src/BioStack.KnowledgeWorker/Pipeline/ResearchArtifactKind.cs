namespace BioStack.KnowledgeWorker.Pipeline;

public enum ResearchArtifactKind
{
    CompoundCandidateBatch = 1,
    SourceRegistry = 2,
    EvidencePacket = 3,
    ReviewDecisionBatch = 4,
}

public sealed record ResearchArtifactSchemaDescriptor(
    ResearchArtifactKind Kind,
    string SchemaFileName,
    string RecordType);

public static class ResearchArtifactSchemas
{
    public static IReadOnlyList<ResearchArtifactSchemaDescriptor> All { get; } = new[]
    {
        new ResearchArtifactSchemaDescriptor(
            ResearchArtifactKind.CompoundCandidateBatch,
            "compound-candidate.schema.json",
            "compound-candidate-batch"),
        new ResearchArtifactSchemaDescriptor(
            ResearchArtifactKind.SourceRegistry,
            "source-registry.schema.json",
            "source-registry"),
        new ResearchArtifactSchemaDescriptor(
            ResearchArtifactKind.EvidencePacket,
            "evidence-packet.schema.json",
            "compound-evidence-packet"),
        new ResearchArtifactSchemaDescriptor(
            ResearchArtifactKind.ReviewDecisionBatch,
            "review-decision.schema.json",
            "review-decision-batch"),
    };

    public static ResearchArtifactSchemaDescriptor Get(ResearchArtifactKind kind)
        => All.FirstOrDefault(d => d.Kind == kind)
           ?? throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown research artifact kind.");
}