namespace BioStack.Contracts.Requests;

/// <summary>Marks the scheduled doses for a single day as taken. Date is date-only (YYYY-MM-DD).</summary>
public sealed record LogProtocolDosesRequest(string Date);
