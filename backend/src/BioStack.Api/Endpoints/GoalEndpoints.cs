namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/goals", (IGoalService goalService) => Results.Ok(goalService.GetDefinitions()))
            .WithTags("Goals")
            .WithName("GetGoalDefinitions")
            .RequireAuthorization();

        var profileGroup = app.MapGroup("/api/v1/profiles/{profileId:guid}/goals")
            .WithTags("Goals")
            .RequireAuthorization();

        profileGroup.MapGet("/", GetProfileGoals)
            .WithName("GetProfileGoals");

        profileGroup.MapPost("/", SetProfileGoals)
            .WithName("SetProfileGoals")
            .RequireConsent();
    }

    private static async Task<IResult> GetProfileGoals(
        Guid profileId,
        IGoalService goalService,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await goalService.GetProfileGoalsAsync(profileId, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> SetProfileGoals(
        Guid profileId,
        SetProfileGoalsRequest request,
        IGoalService goalService,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await goalService.SetProfileGoalsAsync(profileId, request.GoalIds, cancellationToken));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["goalIds"] = [exception.Message],
            });
        }
    }
}
