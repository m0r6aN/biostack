namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class ProtocolPhaseEndpoints
{
    public static void MapProtocolPhaseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/phases")
            .WithTags("Protocol Phases")
            .RequireAuthorization();

        group.MapGet("/", GetPhases)
            .WithName("GetPhases");

        group.MapPost("/", CreatePhase)
            .WithName("CreatePhase");
    }

    private static async Task<IResult> GetPhases(Guid profileId, IProtocolPhaseService phaseService, CancellationToken ct)
    {
        try
        {
            var phases = await phaseService.GetPhasesByProfileAsync(profileId, ct);
            return Results.Ok(phases);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CreatePhase(Guid profileId, CreateProtocolPhaseRequest request, IProtocolPhaseService phaseService, CancellationToken ct)
    {
        try
        {
            var phase = await phaseService.CreatePhaseAsync(profileId, request, ct);
            return Results.Created($"/api/v1/profiles/{profileId}/phases/{phase.Id}", phase);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
