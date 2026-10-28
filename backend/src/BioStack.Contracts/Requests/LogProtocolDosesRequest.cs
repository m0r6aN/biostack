namespace BioStack.Contracts.Requests;

/// <summary>
/// Logs doses for a calendar day. With no <paramref name="Entries"/> it only marks the day's scheduled
/// doses taken (the original behaviour). With entries it records, per compound, the amount the user says
/// they actually took. Amounts are always the user's own entry; the server never fills one in.
/// </summary>
public sealed record LogProtocolDosesRequest(string Date, IReadOnlyList<LogDoseEntryRequest>? Entries = null);

public sealed record LogDoseEntryRequest(
    Guid CompoundId,
    decimal? Amount,
    string? Unit,
    string? Note = null,
    DateTime? TakenAtUtc = null);
