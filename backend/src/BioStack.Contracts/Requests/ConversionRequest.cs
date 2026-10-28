namespace BioStack.Contracts.Requests;

using System.Text.Json.Serialization;

/// <summary>
/// Unit conversion request. The server owns the unit table: there is deliberately no client-supplied
/// conversion factor, and any unrecognised property (for example a legacy "conversionFactor") is rejected
/// at deserialization, so the request is a 400 rather than silently ignored.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConversionRequest(
    decimal Amount,
    string FromUnit,
    string ToUnit
);
