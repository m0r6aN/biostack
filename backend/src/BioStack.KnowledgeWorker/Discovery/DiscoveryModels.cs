namespace BioStack.KnowledgeWorker.Discovery;

using System.Text.Json.Nodes;

/// <summary>One citation that a discovery source found for a candidate name.</summary>
public sealed record DiscoveryEvidence(
    string SourceId,
    string Identifier,
    string Title,
    string Url,
    DateOnly? Date,
    string? Detail);

/// <summary>A raw candidate name extracted from one source document (trial, paper, approval, preprint).</summary>
public sealed record DiscoveryHit(
    string Name,
    IReadOnlyList<string> Aliases,
    string Classification,
    string AreaId,
    DiscoveryEvidence Evidence,
    string? Phase = null,
    string? Sponsor = null,
    IReadOnlyList<string>? Conditions = null);

/// <summary>What one source returned for a run, including the queries it sent and any errors.</summary>
public sealed record DiscoverySourceResult(
    string SourceId,
    IReadOnlyList<DiscoveryHit> Hits,
    IReadOnlyList<string> Queries,
    IReadOnlyList<string> Errors);

/// <summary>Inclusive date window a discovery run looks back over.</summary>
public sealed record DiscoveryWindow(DateOnly From, DateOnly To);

/// <summary>
/// An open-ended discovery source. Implementations query a public API whose terms allow
/// automated access (see docs/biostack/discovery-runbook.md, "Source terms and risks"),
/// never scrape HTML, and never throw for network failures: errors are returned in
/// <see cref="DiscoverySourceResult.Errors"/> so one source failing does not stop the run.
/// </summary>
public interface IDiscoverySource
{
    string SourceId { get; }
    Task<DiscoverySourceResult> DiscoverAsync(DiscoveryScope scope, DiscoveryWindow window, CancellationToken cancellationToken);
}

/// <summary>Where an aggregated candidate stands against the existing corpus.</summary>
public enum CorpusMatchStatus
{
    /// <summary>Not in the corpus under any name, alias, brand or synonym.</summary>
    New,
    /// <summary>Already a record (by name, alias, brand, synonym or moiety).</summary>
    InCorpus,
    /// <summary>Not a record name, but mentioned next to one record's name in its text — likely a missing alias.</summary>
    PossibleAlias,
}

public sealed record DiscoveredCandidate(
    string Name,
    IReadOnlyList<string> Aliases,
    string Classification,
    IReadOnlyList<string> AreaIds,
    IReadOnlyList<DiscoveryEvidence> Evidence,
    IReadOnlyList<string> Phases,
    IReadOnlyList<string> Sponsors,
    IReadOnlyList<string> Conditions,
    int Score,
    CorpusMatchStatus Status,
    string? MatchedRecord,
    IReadOnlyList<string>? NameFlags = null)
{
    public IReadOnlyList<string> SourceIds => Evidence.Select(e => e.SourceId).Distinct(StringComparer.Ordinal).ToList();
}

public sealed record DiscoveryRunOutput(
    IReadOnlyList<DiscoveredCandidate> Candidates,
    IReadOnlyList<DiscoverySourceResult> SourceResults,
    JsonObject? ResearchRequestBatch,
    JsonArray DraftRecords,
    IReadOnlyList<ScreeningNote>? Screening = null);
