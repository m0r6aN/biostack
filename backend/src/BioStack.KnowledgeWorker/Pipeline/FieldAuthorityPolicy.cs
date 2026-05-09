namespace BioStack.KnowledgeWorker.Pipeline;

public static class FieldAuthorityPolicy
{
    private static readonly HashSet<string> AuthoritativeTiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "A1",
        "A2",
    };

    private static readonly HashSet<string> SafetyCriticalClaimTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "regulatory",
        "approved-indication",
        "dose-context",
        "formulation",
        "storage-reconstitution",
        "contraindication",
        "warning",
        "monitoring",
        "interaction",
    };

    public static bool IsAuthoritativeTier(string? authorityTier)
        => !string.IsNullOrWhiteSpace(authorityTier) && AuthoritativeTiers.Contains(authorityTier);

    public static bool RequiresAuthoritativeSupport(string? claimType, bool fieldAuthorityRequired)
        => fieldAuthorityRequired || SafetyCriticalClaimTypes.Contains(claimType ?? string.Empty);
}