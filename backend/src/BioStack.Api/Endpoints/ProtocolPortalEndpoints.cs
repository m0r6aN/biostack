namespace BioStack.Api.Endpoints;

using System.Globalization;
using BioStack.Api.Auth;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public static class ProtocolPortalEndpoints
{
    private static IResult ProductGate(FeatureLimitExceededException ex) =>
        Results.Json(
            new ProductErrorResponse(ex.Code, ex.Message, ex.Tier.ToString(), ex.Limit, true),
            statusCode: StatusCodes.Status402PaymentRequired);

    public static void MapProtocolPortalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/protocol")
            .WithTags("Protocol Portal")
            .RequireAuthorization();

        group.MapGet("/portal", GetPortal).WithName("GetProtocolPortal");
        group.MapGet("/active", GetActive).WithName("GetProtocolActive");
        group.MapGet("/schedule", GetSchedule).WithName("GetProtocolSchedule");
        group.MapGet("/schedule/week", GetWeek).WithName("GetProtocolScheduleWeek");
        group.MapGet("/diet", GetDiet).WithName("GetProtocolDiet");
        group.MapGet("/supplements", GetSupplements).WithName("GetProtocolSupplements");
        group.MapGet("/monitoring", GetMonitoring).WithName("GetProtocolMonitoring");
        group.MapGet("/milestones", GetMilestones).WithName("GetProtocolMilestones");
        group.MapGet("/resources", GetResources).WithName("GetProtocolResources");

        group.MapPost("/doses/log", LogDoses).WithName("LogProtocolDoses").RequireConsent();

        // Care-team messaging lives off the protocol group, under the profile.
        var careTeam = app.MapGroup("/api/v1/profiles/{profileId}/care-team")
            .WithTags("Protocol Portal")
            .RequireAuthorization();
        careTeam.MapPost("/message", SendCareTeamMessage).WithName("SendCareTeamMessage").RequireConsent();
    }

    private static async Task<IResult> GetPortal(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetPortalAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetActive(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetActiveAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetSchedule(Guid profileId, string? date, IProtocolPortalService service, CancellationToken ct)
    {
        if (!TryParseDateParam(date, out var parsed))
            return Results.BadRequest(new { message = "date must be YYYY-MM-DD." });

        try
        {
            return Results.Ok(await service.GetScheduleAsync(profileId, parsed, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetWeek(Guid profileId, string? start, IProtocolPortalService service, CancellationToken ct)
    {
        if (!TryParseDateParam(start, out var parsed))
            return Results.BadRequest(new { message = "start must be YYYY-MM-DD." });

        try
        {
            return Results.Ok(await service.GetWeekAsync(profileId, parsed, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetDiet(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetDietAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetSupplements(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetSupplementsAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMonitoring(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetMonitoringAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMilestones(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetMilestonesAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetResources(Guid profileId, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetResourcesAsync(profileId, ct));
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> LogDoses(Guid profileId, LogProtocolDosesRequest request, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            await service.LogDosesAsync(profileId, request, ct);
            return Results.NoContent();
        }
        catch (FeatureLimitExceededException ex)
        {
            return ProductGate(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SendCareTeamMessage(Guid profileId, CareTeamMessageRequest request, IProtocolPortalService service, CancellationToken ct)
    {
        try
        {
            await service.SaveCareTeamNoteAsync(profileId, request, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static bool TryParseDateParam(string? value, out DateOnly? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            parsed = date;
            return true;
        }

        return false;
    }
}
