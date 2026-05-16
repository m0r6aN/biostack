namespace BioStack.KnowledgeWorker.Pipeline.Graph;

public sealed record CompoundGraph(
    string GraphVersion,
    DateTimeOffset GeneratedAtUtc,
    CompoundGraphCounts Counts,
    IReadOnlyList<CompoundGraphNode> Nodes,
    IReadOnlyList<CompoundGraphEdge> Edges,
    IReadOnlyList<CompoundGraphReviewFinding> ReviewFindings);

public sealed record CompoundGraphCounts(
    int Nodes,
    int Edges,
    int ReviewRequiredEdges,
    int CommunitySignalEdges,
    int ConflictEdges);
