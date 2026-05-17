namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public static class ConsentEndpoints
{
    public static void MapConsentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/consent")
            .WithTags("Consent")
            .RequireAuthorization();

        group.MapGet("/", GetConsent)
            .WithName("GetConsent");

        group.MapPost("/", RecordConsent)
            .WithName("RecordConsent");
    }

    private static async Task<IResult> GetConsent(IConsentGate consentGate, CancellationToken ct)
    {
        try
        {
            var status = await consentGate.GetStatusAsync(ct);
            return Results.Ok(new ConsentStatusResponse(status.Accepted, status.ConsentAcceptedAtUtc, status.ConsentVersion));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (InvalidOperationException)
        {
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> RecordConsent(RecordConsentRequest? request, IConsentGate consentGate, CancellationToken ct)
    {
        try
        {
            var status = await consentGate.RecordAsync(request?.ConsentVersion, ct);
            return Results.Ok(new ConsentStatusResponse(status.Accepted, status.ConsentAcceptedAtUtc, status.ConsentVersion));
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Unauthorized();
        }
        catch (InvalidOperationException)
        {
            return Results.Unauthorized();
        }
    }
}
