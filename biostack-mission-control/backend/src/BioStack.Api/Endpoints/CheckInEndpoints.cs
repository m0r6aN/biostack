namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class CheckInEndpoints
{
    public static void MapCheckInEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/checkins")
            .WithTags("Check-Ins");

        group.MapGet("/", GetCheckIns)
            .WithName("GetCheckIns");

        group.MapPost("/", CreateCheckIn)
            .WithName("CreateCheckIn");
    }

    private static async Task<IResult> GetCheckIns(Guid profileId, ICheckInService checkInService, CancellationToken ct)
    {
        var checkIns = await checkInService.GetCheckInsByProfileAsync(profileId, ct);
        return Results.Ok(checkIns);
    }

    private static async Task<IResult> CreateCheckIn(Guid profileId, CreateCheckInRequest request, ICheckInService checkInService, CancellationToken ct)
    {
        try
        {
            var checkIn = await checkInService.CreateCheckInAsync(profileId, request, ct);
            return Results.Created($"/api/v1/profiles/{profileId}/checkins/{checkIn.Id}", checkIn);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
