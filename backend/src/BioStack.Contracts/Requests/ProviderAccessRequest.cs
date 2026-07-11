namespace BioStack.Contracts.Requests;

public sealed record ProviderAccessRequest(
    string Email,
    string Name,
    string Organization,
    string Role,
    bool Consent,
    string? Website = null);

public sealed record UpdateProviderAccessRequest(
    string Status,
    string? Owner);
