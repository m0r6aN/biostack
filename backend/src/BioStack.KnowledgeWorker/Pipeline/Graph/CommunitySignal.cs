namespace BioStack.KnowledgeWorker.Pipeline.Graph;

using System.Text.Json.Serialization;

public sealed record CommunitySignal(
    bool Present,
    CommunitySignalStrength SignalStrength,
    CommunitySignalDirection SignalDirection,
    CommunitySignalUse? SignalUse,
    CanonicalTruthStatus CanonicalTruthStatus,
    string? Notes);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommunitySignalStrength { None, Isolated, Recurring, Widespread }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommunitySignalDirection { Positive, Negative, Mixed, Unclear }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommunitySignalUse { Popularity, AliasDiscovery, StackPattern, AdverseSelfReport, Misinformation, ResearchPriority }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalTruthStatus { Unsupported, Contradicted, PlausibleMechanistic, PartiallySupported, Supported, Unknown }
