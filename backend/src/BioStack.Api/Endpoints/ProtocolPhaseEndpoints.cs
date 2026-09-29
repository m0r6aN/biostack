namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class ProtocolPhaseEndpoints
{
    public static void MapProtocolPhaseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/phases")
            .WithTags("Protocol Phases");

        group.MapGet("/", GetPhases)
            .WithName("GetPhases");

        group.MapPost("/", CreatePhase)
            .WithName("CreatePhase")
            .RequireConsent();
    }

    private static async Task<IResult> GetPhases(Guid profileId, IProtocolPhaseService phaseService, CancellationToken ct)
    {
        var phases = await phaseService.GetPhasesByProfileAsync(profileId, ct);
        return Results.Ok(phases);
    }

    private static async Task<IResult> CreatePhase(
        Guid profileId,
        CreateProtocolPhaseRequest request,
        IProtocolPhaseService phaseService,
        IEntitlementService entitlementService,
        HttpContext http,
        CancellationToken ct)
    {
        try
        {
            var entitlements = await entitlementService.GetUserEntitlementsAsync(HttpUser.GetUserId(http.User), ct);
            if (!entitlements.IsPro)
            {
                return Results.Problem(
                    title: "Phase planning requires Pro",
                    detail: "Free users can preview phases and cycles. Creating/editing phase plans requires Pro.",
                    statusCode: StatusCodes.Status402PaymentRequired);
            }

            var phase = await phaseService.CreatePhaseAsync(profileId, request, ct);
            return Results.Created($"/api/v1/profiles/{profileId}/phases/{phase.Id}", phase);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
