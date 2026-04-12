namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;

public static class ProtocolEndpoints
{
    public static void MapProtocolEndpoints(this WebApplication app)
    {
        var profileGroup = app.MapGroup("/api/v1/profiles/{profileId}/protocols")
            .WithTags("Protocols");

        profileGroup.MapGet("/", GetProtocols)
            .WithName("GetProtocols");

        profileGroup.MapPost("/", SaveCurrentStack)
            .WithName("SaveCurrentStackAsProtocol");

        profileGroup.MapGet("/current-stack-intelligence", GetCurrentStackIntelligence)
            .WithName("GetCurrentStackIntelligence");

        profileGroup.MapGet("/active-run", GetActiveRun)
            .WithName("GetActiveProtocolRun");

        profileGroup.MapGet("/mission-control", GetMissionControl)
            .WithName("GetProtocolMissionControl");

        var protocolGroup = app.MapGroup("/api/v1/protocols")
            .WithTags("Protocols");

        protocolGroup.MapGet("/{id}", GetProtocol)
            .WithName("GetProtocol");

        protocolGroup.MapGet("/{id}/review", GetProtocolReview)
            .WithName("GetProtocolReview");

        protocolGroup.MapGet("/{id}/patterns", GetProtocolPatterns)
            .WithName("GetProtocolPatterns");

        protocolGroup.MapGet("/{id}/drift", GetProtocolDrift)
            .WithName("GetProtocolDrift");

        protocolGroup.MapPost("/{id}/review/complete", CompleteReview)
            .WithName("CompleteProtocolReview");

        protocolGroup.MapPost("/{id}/computations", RecordComputation)
            .WithName("RecordProtocolComputation");

        protocolGroup.MapPost("/{id}/runs", StartRun)
            .WithName("StartProtocolRun");

        protocolGroup.MapPost("/runs/{runId}/complete", CompleteRun)
            .WithName("CompleteProtocolRun");

        protocolGroup.MapPost("/runs/{runId}/abandon", AbandonRun)
            .WithName("AbandonProtocolRun");

        protocolGroup.MapPost("/runs/{runId}/evolve", EvolveFromRun)
            .WithName("EvolveProtocolFromRun");
    }

    private static async Task<IResult> GetProtocols(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        var protocols = await protocolService.GetProtocolsByProfileAsync(profileId, ct);
        return Results.Ok(protocols);
    }

    private static async Task<IResult> GetCurrentStackIntelligence(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        var intelligence = await protocolService.GetCurrentStackIntelligenceAsync(profileId, ct);
        return Results.Ok(intelligence);
    }

    private static async Task<IResult> GetActiveRun(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        var run = await protocolService.GetActiveRunAsync(profileId, ct);
        return run is null ? Results.NoContent() : Results.Ok(run);
    }

    private static async Task<IResult> GetMissionControl(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        var missionControl = await protocolService.GetMissionControlAsync(profileId, ct);
        return Results.Ok(missionControl);
    }

    private static async Task<IResult> SaveCurrentStack(Guid profileId, SaveProtocolRequest request, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var protocol = await protocolService.SaveCurrentStackAsync(profileId, request, ct);
            return Results.Created($"/api/v1/protocols/{protocol.Id}", protocol);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No active compounds", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetProtocol(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var protocol = await protocolService.GetProtocolAsync(id, ct);
            return Results.Ok(protocol);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetProtocolReview(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var review = await protocolService.GetProtocolReviewAsync(id, ct);
            return Results.Ok(review);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetProtocolPatterns(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var snapshot = await protocolService.GetPatternSnapshotAsync(id, ct);
            return Results.Ok(snapshot);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetProtocolDrift(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var snapshot = await protocolService.GetDriftSnapshotAsync(id, ct);
            return Results.Ok(snapshot);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CompleteReview(Guid id, CompleteProtocolReviewRequest request, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var completed = await protocolService.CompleteReviewAsync(id, request, ct);
            return Results.Created($"/api/v1/protocols/{id}/review/completions/{completed.Id}", completed);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> RecordComputation(Guid id, CreateProtocolComputationRequest request, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var computation = await protocolService.RecordComputationAsync(id, request, ct);
            return Results.Created($"/api/v1/protocols/{id}/computations/{computation.Id}", computation);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("type", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> StartRun(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var run = await protocolService.StartRunAsync(id, ct);
            return Results.Created($"/api/v1/protocols/{id}/runs/{run.Id}", run);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CompleteRun(Guid runId, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var run = await protocolService.CompleteRunAsync(runId, ct);
            return Results.Ok(run);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> AbandonRun(Guid runId, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var run = await protocolService.AbandonRunAsync(runId, ct);
            return Results.Ok(run);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> EvolveFromRun(Guid runId, EvolveProtocolFromRunRequest request, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var protocol = await protocolService.EvolveFromRunAsync(runId, request, ct);
            return Results.Created($"/api/v1/protocols/{protocol.Id}", protocol);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("completed or abandoned", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }
}
