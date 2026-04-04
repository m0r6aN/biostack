namespace BioStack.Contracts.Requests;

public sealed record LeadCaptureRequest(
    string Email,
    string Source
);
