namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;

public static class TimelineEndpoints
{
    public static void MapTimelineEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/timeline")
            .WithTags("Timeline")
            .RequireAuthorization();

        group.MapGet("/", GetTimeline)
            .WithName("GetTimeline");
    }

    private static async Task<IResult> GetTimeline(Guid profileId, ITimelineService timelineService, CancellationToken ct)
    {
        try
        {
            var events = await timelineService.GetTimelineAsync(profileId, ct);
            return Results.Ok(events);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
