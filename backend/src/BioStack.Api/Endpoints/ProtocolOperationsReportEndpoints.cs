namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;

/// <summary>
/// Read-only, observational protocol operations report. Surfaces factual counts and
/// recent activity for a profile's protocol — no recommendations, diagnosis, dosing
/// instructions, treatment advice, or Protocol Intelligence narrative.
/// </summary>
public static class ProtocolOperationsReportEndpoints
{
    public static void MapProtocolOperationsReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/protocol")
            .WithTags("Protocol Operations Report")
            .RequireAuthorization();

        group.MapGet("/operations-report", GetOperationsReport).WithName("GetProtocolOperationsReport");
    }

    private static async Task<IResult> GetOperationsReport(
        Guid profileId,
        IProtocolOperationsReportService service,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetReportAsync(profileId, ct));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
