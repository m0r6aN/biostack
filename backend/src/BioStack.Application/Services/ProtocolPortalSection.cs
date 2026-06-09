namespace BioStack.Application.Services;

/// <summary>Provenance status for a flattened portal section.</summary>
public static class PortalDataStatus
{
    public const string Real = "real";
    public const string CuratedBaseline = "curated_baseline";
    public const string Derived = "derived";
    public const string Unavailable = "unavailable";
    public const string Pending = "pending";
}

/// <summary>Origin of the data backing a portal section.</summary>
public static class PortalSource
{
    public const string Protocol = "protocol";
    public const string ProtocolItem = "protocol_item";
    public const string ProtocolPhase = "protocol_phase";
    public const string CheckIn = "check_in";
    public const string KnowledgeBaseline = "knowledge_baseline";
    public const string Computed = "computed";
    public const string NotCollected = "not_collected";
}

/// <summary>
/// Backend-internal provenance wrapper. NOT placed on the wire as the primary
/// contract — the composer flattens <see cref="Data"/> (or the honest
/// <see cref="EmptyState"/>) into the existing flat shape, and surfaces a reduced
/// view via the additive sectionMeta map.
/// </summary>
public sealed record PortalSection<T>(
    string Status,
    string Source,
    DateTime GeneratedAtUtc,
    string? BaselineVersion,
    T? Data,
    string? EmptyState)
{
    public static PortalSection<T> Real(T data, string source) =>
        new(PortalDataStatus.Real, source, DateTime.UtcNow, null, data, null);

    public static PortalSection<T> Derived(T data, string source) =>
        new(PortalDataStatus.Derived, source, DateTime.UtcNow, null, data, null);

    public static PortalSection<T> Curated(T data, string baselineVersion) =>
        new(PortalDataStatus.CuratedBaseline, PortalSource.KnowledgeBaseline, DateTime.UtcNow, baselineVersion, data, null);

    public static PortalSection<T> Empty(T fallback, string source, string emptyState) =>
        new(PortalDataStatus.Unavailable, source, DateTime.UtcNow, null, fallback, emptyState);
}
