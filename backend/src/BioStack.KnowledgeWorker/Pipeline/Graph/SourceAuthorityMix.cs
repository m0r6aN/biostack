namespace BioStack.KnowledgeWorker.Pipeline.Graph;

public sealed record SourceAuthorityMix(IReadOnlyList<string> AuthorityTiers)
{
    public bool IsLowAuthorityOnly => AuthorityTiers.Count > 0
        && AuthorityTiers.All(t => string.Equals(t, "D", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(t, "X", StringComparison.OrdinalIgnoreCase));

    public bool IsRegulatoryGrade => AuthorityTiers.Any(t =>
        string.Equals(t, "A1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(t, "A2", StringComparison.OrdinalIgnoreCase));

    public static SourceAuthorityMix Empty { get; } = new(Array.Empty<string>());
}
