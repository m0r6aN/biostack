namespace BioStack.Api.Endpoints;

using BioStack.Application.Governance;
using BioStack.Infrastructure.Keon;

public static class PolicyGateEndpoints
{
    public static void MapPolicyGateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/policy")
            .WithTags("PolicyGate")
            .RequireAuthorization();

        group.MapPost("/classify", HandleCheck)
            .WithName("PolicyClassify");

        group.MapPost("/check", HandleCheck)
            .WithName("PolicyCheck");
    }

    private static async Task<IResult> HandleCheck(
        PolicyCheckRequest request,
        PolicyGate gate,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Results.BadRequest(new { error = "text is required" });

        PolicyGateCheckResult result;
        try
        {
            result = await gate.CheckAsync(
                request.Text,
                request.Context ?? string.Empty,
                request.TenantId ?? string.Empty,
                request.ActorId ?? string.Empty,
                ct);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }

        return Results.Ok(new PolicyCheckResponse(
            Decision: DecisionString(result.Decision),
            DisclaimerText: result.DisclaimerText,
            RewrittenText: result.RewrittenText,
            BlockReason: result.BlockReason,
            PolicyHash: new PolicyHashDto(result.PolicyHash.Value, result.PolicyHash.Version),
            LocallyClassified: result.LocallyClassified));
    }

    private static string DecisionString(PolicyDecision d) => d switch
    {
        PolicyDecision.Allowed                  => "allowed",
        PolicyDecision.AllowedWithDisclaimer    => "allowed-with-disclaimer",
        PolicyDecision.RewriteRequired          => "rewrite-required",
        PolicyDecision.Blocked                  => "blocked",
        PolicyDecision.EscalateToProviderReview => "escalate-to-provider-review",
        _                                       => "blocked",
    };
}

// ── Request / Response DTOs ────────────────────────────────────────────────────

public sealed record PolicyCheckRequest(
    string Text,
    string? Context,
    string? TenantId,
    string? ActorId);

public sealed record PolicyCheckResponse(
    string Decision,
    string? DisclaimerText,
    string? RewrittenText,
    string? BlockReason,
    PolicyHashDto PolicyHash,
    bool LocallyClassified);

public sealed record PolicyHashDto(string Value, string Version);
