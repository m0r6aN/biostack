namespace BioStack.Api.Endpoints;

using BioStack.Api.Auth;
using BioStack.Application.ProtocolIntelligence;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Keon;

public static class ProtocolEndpoints
{
    private static IResult ProductGate(FeatureLimitExceededException ex) =>
        Results.Json(
            new ProductErrorResponse(ex.Code, ex.Message, ex.Tier.ToString(), ex.Limit, true),
            statusCode: StatusCodes.Status402PaymentRequired);

    public static void MapProtocolEndpoints(this WebApplication app)
    {
        var profileGroup = app.MapGroup("/api/v1/profiles/{profileId}/protocols")
            .WithTags("Protocols")
            .RequireAuthorization();

        profileGroup.MapGet("", GetProtocols)
            .WithName("GetProtocols");

        profileGroup.MapPost("", SaveCurrentStack)
            .WithName("SaveCurrentStackAsProtocol")
            .RequireConsent();

        profileGroup.MapGet("/current-stack-intelligence", GetCurrentStackIntelligence)
            .WithName("GetCurrentStackIntelligence");

        profileGroup.MapGet("/active-run", GetActiveRun)
            .WithName("GetActiveProtocolRun");

        profileGroup.MapGet("/mission-control", GetMissionControl)
            .WithName("GetProtocolMissionControl");

        var protocolGroup = app.MapGroup("/api/v1/protocols")
            .WithTags("Protocols")
            .RequireAuthorization();

        protocolGroup.MapGet("/{id}", GetProtocol)
            .WithName("GetProtocol");

        protocolGroup.MapGet("/{id}/intelligence", GetProtocolIntelligence)
            .WithName("GetProtocolIntelligence");

        protocolGroup.MapPost("/{id}/intelligence/preview", PreviewProtocolIntelligence)
            .WithName("PreviewProtocolIntelligence");

        protocolGroup.MapGet("/{id}/review", GetProtocolReview)
            .WithName("GetProtocolReview");

        protocolGroup.MapGet("/{id}/patterns", GetProtocolPatterns)
            .WithName("GetProtocolPatterns");

        protocolGroup.MapGet("/{id}/drift", GetProtocolDrift)
            .WithName("GetProtocolDrift");

        protocolGroup.MapGet("/{id}/sequence-expectation", GetProtocolSequenceExpectation)
            .WithName("GetProtocolSequenceExpectation");

        protocolGroup.MapPost("/{id}/review/complete", CompleteReview)
            .WithName("CompleteProtocolReview")
            .RequireConsent();

        protocolGroup.MapPost("/{id}/computations", RecordComputation)
            .WithName("RecordProtocolComputation")
            .RequireConsent();

        protocolGroup.MapPost("/{id}/runs", StartRun)
            .WithName("StartProtocolRun")
            .RequireConsent();

        protocolGroup.MapPost("/runs/{runId}/complete", CompleteRun)
            .WithName("CompleteProtocolRun")
            .RequireConsent();

        protocolGroup.MapPost("/runs/{runId}/abandon", AbandonRun)
            .WithName("AbandonProtocolRun")
            .RequireConsent();

        protocolGroup.MapPost("/runs/{runId}/evolve", EvolveFromRun)
            .WithName("EvolveProtocolFromRun")
            .RequireConsent();
    }

    private static async Task<IResult> GetProtocols(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var protocols = await protocolService.GetProtocolsByProfileAsync(profileId, ct);
            return Results.Ok(protocols);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetCurrentStackIntelligence(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var intelligence = await protocolService.GetCurrentStackIntelligenceAsync(profileId, ct);
            return Results.Ok(intelligence);
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

    private static async Task<IResult> GetActiveRun(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var run = await protocolService.GetActiveRunAsync(profileId, ct);
            return run is null ? Results.NoContent() : Results.Ok(run);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMissionControl(Guid profileId, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var missionControl = await protocolService.GetMissionControlAsync(profileId, ct);
            return Results.Ok(missionControl);
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

    private static async Task<IResult> GetProtocolIntelligence(
        Guid id,
        IProtocolIntelligenceService protocolIntelligenceService,
        IFeatureGate featureGate,
        CancellationToken ct)
    {
        var tier = await featureGate.GetCurrentTierAsync(ct);
        var response = protocolIntelligenceService.BuildResponse(id, tier, []);
        return Results.Ok(response);
    }

    private static async Task<IResult> PreviewProtocolIntelligence(
        Guid id,
        ProtocolIntelligencePreviewRequest request,
        IProtocolIntelligenceService protocolIntelligenceService,
        IFeatureGate featureGate,
        CancellationToken ct)
    {
        var tier = await featureGate.GetCurrentTierAsync(ct);
        var artifacts = request.ReviewedArtifacts
            .Select(artifact => new ProtocolIntelligenceReviewedArtifact(
                artifact.ArtifactType,
                artifact.Artifact.ToDictionary(
                    pair => pair.Key,
                    pair => ConvertJsonElement(pair.Value),
                    StringComparer.Ordinal)))
            .ToArray();
        var response = protocolIntelligenceService.BuildResponse(id, tier, artifacts);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetProtocolReview(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var review = await protocolService.GetProtocolReviewAsync(id, ct);
            return Results.Ok(review);
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

    private static async Task<IResult> GetProtocolPatterns(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var snapshot = await protocolService.GetPatternSnapshotAsync(id, ct);
            return Results.Ok(snapshot);
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

    private static async Task<IResult> GetProtocolDrift(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var snapshot = await protocolService.GetDriftSnapshotAsync(id, ct);
            return Results.Ok(snapshot);
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

    private static async Task<IResult> GetProtocolSequenceExpectation(Guid id, IProtocolService protocolService, CancellationToken ct)
    {
        try
        {
            var snapshot = await protocolService.GetSequenceExpectationSnapshotAsync(id, ct);
            return Results.Ok(snapshot);
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

    private static async Task<IResult> CompleteReview(
        Guid id,
        CompleteProtocolReviewRequest request,
        IProtocolService protocolService,
        IRuntimeReceiptFactory receipts,
        ICurrentUserAccessor currentUser,
        CancellationToken ct)
    {
        try
        {
            var completed = await protocolService.CompleteReviewAsync(id, request, ct);

            // Issue a Decision Receipt — this review completion is a governed effect on the Spine.
            // Actor is the authenticated user; evidence refs bind the receipt to the reviewed
            // protocol (and run, when present) so it proves what was reviewed, not merely that
            // a review happened.
            var evidenceRefs = new List<string> { ReceiptRefs.Protocol(completed.ProtocolId) };
            if (completed.RunId is { } runId)
                evidenceRefs.Add(ReceiptRefs.ProtocolRun(runId));

            var receipt = await receipts.IssueAndAppendAsync(new ReceiptContext(
                ReceiptClass: ReceiptClass.ProtocolReviewCompleted,
                SubjectUri: $"protocol:{id}/review",
                Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
                EvidenceRefs: evidenceRefs,
                Decision: "commentary-only",
                EffectStatus: "commentary-only",
                InputHashSeed: completed.Id.ToString("N")), ct);

            return Results.Created($"/api/v1/protocols/{id}/review/completions/{completed.Id}", new
            {
                id = completed.Id,
                protocolId = completed.ProtocolId,
                runId = completed.RunId,
                completedAtUtc = completed.CompletedAtUtc,
                notes = completed.Notes,
                receiptUri = receipt.ReceiptUri,
            });
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

    private static object? ConvertJsonElement(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when element.TryGetDouble(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Null => null,
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertJsonElement(property.Value),
                StringComparer.Ordinal),
            _ => element.ToString()
        };

    private sealed record ProtocolIntelligencePreviewRequest(
        IReadOnlyList<ProtocolIntelligencePreviewArtifact> ReviewedArtifacts);

    private sealed record ProtocolIntelligencePreviewArtifact(
        string ArtifactType,
        Dictionary<string, JsonElement> Artifact);
}
