namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;

/// <summary>
/// Read-only, observational protocol operations report. Surfaces factual counts
/// recent activity for profile's protocol - no recommendations, diagnosis, dosing
/// instructions, treatment advice, or Protocol Intelligence narrative.
/// </summary>
public static class ProtocolOperationsReportEndpoints
{
    public static void MapProtocolOperationsReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/profiles/{profileId}/protocol")
            .WithTags("Protocol Operations Report")
            .RequireAuthorization();

        group.MapGet("/operations-report", GetOperationsReport)
            .WithName("GetProtocolOperationsReport");

        group.MapGet("/operations-report/export", GetOperationsReportExport)
            .WithName("GetProtocolOperationsReportExport");

        group.MapGet("/operations-report/export/bundle", GetOperationsReportExportBundle)
            .WithName("GetProtocolOperationsReportExportBundle");
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

    private static async Task<IResult> GetOperationsReportExport(
        Guid profileId,
        IProtocolOperationsReportExportService service,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetExportAsync(profileId, ct));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetOperationsReportExportBundle(
        Guid profileId,
        IProtocolOperationsExportBundleService service,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await service.GetBundleAsync(profileId, ct));
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
