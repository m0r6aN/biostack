namespace BioStack.Contracts.Responses;

public sealed record ProviderAccessConfirmationResponse(
    Guid RequestId,
    string Status,
    DateTime SubmittedAtUtc);

public sealed record ProviderAccessReviewResponse(
    Guid RequestId,
    string Email,
    string Name,
    string Organization,
    string Role,
    string Status,
    string? Owner,
    string ConsentVersion,
    DateTime ConsentRecordedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
