namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // All admin routes require the AdminOnly policy (role claim == "1")
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/knowledge/ingest", async (
            [FromBody] List<KnowledgeEntry> entries,
            [FromServices] IKnowledgeSource knowledgeSource,
            [FromServices] IMemoryCache memoryCache,
            [FromServices] ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            if (entries == null || entries.Count == 0)
                return Results.BadRequest("No entries provided");

            try
            {
                var count = await knowledgeSource.IngestBulkAsync(entries, ct);
                // Bust the parser's alias cache so the analyzer immediately reflects newly seeded compounds.
                memoryCache.Remove("analyzer:knowledge:aliases");
                return Results.Ok(new { Message = $"Successfully ingested {count} compounds", Count = count });
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("AdminEndpoints").LogError(ex, "Knowledge ingest failed");
                return Results.Problem("Knowledge ingestion failed. Check server logs for details.");
            }
        });

        group.MapPost("/knowledge-source-intake", async (
            [FromBody] AdminKnowledgeSourceIntakeRequest request,
            [FromServices] IKnowledgeSourceIntakeService intakeService,
            CancellationToken ct) =>
        {
            try
            {
                var response = await intakeService.CreateAsync(request, ct);
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        });

        group.MapPost("/knowledge-source-intake/{intakeRequestId:guid}/resolve-transcript", async (
            Guid intakeRequestId,
            [FromServices] IQueuedIntakeTranscriptResolutionService resolutionService,
            [FromServices] ITranscriptCandidateArtifactStagingService stagingService,
            [FromServices] ITranscriptCandidateArtifactReviewService reviewService,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            CancellationToken ct) =>
        {
            try
            {
                var resolved = await resolutionService.ResolveAsync(intakeRequestId, ct);

                var descriptor = stagingService.Stage(resolved);
                var reviewModel = reviewService.BuildReviewModel(descriptor);
                var nowUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                var record = TranscriptCandidateReviewRecord.Create(
                    artifactId: reviewModel.ArtifactId,
                    canonicality: reviewModel.Canonicality,
                    reviewState: reviewModel.ReviewState,
                    sourceType: reviewModel.SourceType,
                    sourceUrl: reviewModel.SourceUrl,
                    provider: reviewModel.Provider,
                    isDeterministicFixture: reviewModel.IsDeterministicFixture,
                    segmentCount: reviewModel.SegmentCount,
                    segmentSnapshotSignature: reviewModel.SegmentSnapshotSignature,
                    sourceMetadata: reviewModel.SourceMetadata,
                    createdAtUtc: nowUtc,
                    updatedAtUtc: nowUtc);
                await reviewStore.UpsertAsync(record, ct);

                return Results.Ok(new AdminTranscriptIntakeResolutionResponse(
                    IntakeRequestId: intakeRequestId,
                    SourceType: resolved.SourceReference.SourceType,
                    SourceUrl: resolved.SourceReference.SourceUrl,
                    Provider: resolved.Provider,
                    Status: "resolved",
                    ResultCode: "ok",
                    SegmentCount: resolved.Segments?.Count,
                    IsDeterministicFixture: resolved.IsDeterministicFixture,
                    ProviderMetadata: FilterSafeProviderMetadata(resolved.Metadata)));
            }
            catch (Exception ex) when (ex is ITranscriptSourceMaterialProviderFailure providerFailure)
            {
                return Results.Ok(new AdminTranscriptIntakeResolutionResponse(
                    IntakeRequestId: intakeRequestId,
                    SourceType: "video_url",
                    SourceUrl: string.Empty,
                    Provider: "unavailable",
                    Status: "failed",
                    ResultCode: providerFailure.Failure.Code,
                    SegmentCount: null,
                    IsDeterministicFixture: null,
                    ProviderMetadata: null));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        });

        group.MapGet("/stats", async (
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var stats = new
            {
                Profiles             = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.PersonProfiles, ct),
                KnowledgeEntries     = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.KnowledgeEntries, ct),
                TotalCompoundRecords = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.CompoundRecords, ct),
                TotalCheckIns        = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.CheckIns, ct),
                RegisteredUsers      = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.AppUsers, ct),
            };
            return Results.Ok(stats);
        });

        // Promote a user to Admin (admin-only — only existing admins can elevate others)
        group.MapPost("/users/{userId:guid}/promote", async (
            Guid userId,
            [FromServices] BioStack.Infrastructure.Repositories.IAppUserRepository userRepo,
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(userId, ct);
            if (user is null) return Results.NotFound();

            user.Role = BioStack.Domain.Enums.UserRole.Admin;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id, user.Email, Role = (int)user.Role });
        });

        // Demote Admin back to User
        group.MapPost("/users/{userId:guid}/demote", async (
            Guid userId,
            [FromServices] BioStack.Infrastructure.Repositories.IAppUserRepository userRepo,
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(userId, ct);
            if (user is null) return Results.NotFound();

            user.Role = BioStack.Domain.Enums.UserRole.User;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id, user.Email, Role = (int)user.Role });
        });

        // List users (admin view)
        group.MapGet("/users", async (
            [FromServices] BioStack.Infrastructure.Persistence.BioStackDbContext db,
            CancellationToken ct) =>
        {
            var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(db.AppUsers.Select(u => new
                {
                    u.Id, u.Email, u.DisplayName, u.AvatarUrl,
                    Role = (int)u.Role, Provider = u.Provider,
                    u.CreatedAtUtc, u.LastSeenAtUtc
                }), ct);
            return Results.Ok(users);
        });

        group.MapGet("/staged-transcript-candidate-reviews", async (
            [FromQuery] string reviewState,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(reviewState))
            {
                return Results.BadRequest(new { Message = "reviewState is required." });
            }

            try
            {
                var records = await reviewStore.ListByReviewStateAsync(reviewState, ct);
                var response = records.Select(MapStagedReviewRecordToResponse).ToArray();
                return Results.Ok(response);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        });

        group.MapGet("/staged-transcript-candidate-reviews/{artifactId}", async (
            string artifactId,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                return Results.BadRequest(new { Message = "artifactId is required." });
            }

            var record = await reviewStore.GetByArtifactIdAsync(artifactId, ct);
            if (record is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(MapStagedReviewRecordToResponse(record));
        });

        // Update the review state of a staged transcript candidate artifact.
        // Action must be one of: approve_for_promotion | reject_review | defer_review.
        // Approval is a state label only — it does NOT execute promotion.
        group.MapPost("/staged-transcript-candidate-reviews/{artifactId}/review-state", async (
            string artifactId,
            [FromBody] AdminUpdateStagedTranscriptCandidateReviewStateRequest? request,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            CancellationToken ct) =>
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Action))
            {
                return Results.BadRequest(new { Message = "action is required." });
            }

            var record = await reviewStore.GetByArtifactIdAsync(artifactId, ct);
            if (record is null)
            {
                return Results.NotFound();
            }

            // Construct the review model from the persisted record so the lifecycle
            // can evaluate the transition without touching any canonical source.
            var reviewModel = new TranscriptCandidateArtifactReviewModel(
                ArtifactId: record.ArtifactId,
                ReviewState: record.ReviewState,
                Canonicality: record.Canonicality,
                SourceType: record.SourceType,
                SourceUrl: record.SourceUrl,
                SourceMetadata: record.SourceMetadata,
                Provider: record.Provider,
                IsDeterministicFixture: record.IsDeterministicFixture,
                SegmentCount: record.SegmentCount,
                SegmentSnapshotSignature: record.SegmentSnapshotSignature);

            // Stateless lifecycle — instantiated directly; no external dependencies.
            var lifecycle = new TranscriptCandidateReviewLifecycle();
            var decision = lifecycle.ApplyAction(reviewModel, request.Action);

            if (!decision.IsTransitionAllowed)
            {
                return Results.UnprocessableEntity(new { Message = decision.RejectionReason });
            }

            var updatedRecord = await reviewStore.UpdateReviewStateAsync(
                artifactId: record.ArtifactId,
                expectedCurrentReviewState: decision.FromReviewState,
                nextReviewState: decision.ToReviewState,
                updatedAtUtc: DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                cancellationToken: ct);

            return Results.Ok(MapStagedReviewRecordToResponse(updatedRecord));
        });
    }

    private static IReadOnlyDictionary<string, string>? FilterSafeProviderMetadata(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var blockedTokens = new[]
        {
            "canonical",
            "knowledgeentry",
            "promotion",
            "candidate",
            "extraction",
            "summary",
            "safety",
            "medical"
        };

        Dictionary<string, string>? safe = null;
        foreach (var kvp in metadata)
        {
            var key = kvp.Key ?? string.Empty;
            if (blockedTokens.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            safe ??= new Dictionary<string, string>(StringComparer.Ordinal);
            safe[key] = kvp.Value;
        }

        return safe;
    }

    private static AdminStagedTranscriptCandidateReviewResponse MapStagedReviewRecordToResponse(
        TranscriptCandidateReviewRecord record)
        => new(
            ArtifactId: record.ArtifactId,
            Canonicality: record.Canonicality,
            ReviewState: record.ReviewState,
            SourceType: record.SourceType,
            SourceUrl: record.SourceUrl,
            Provider: record.Provider,
            IsDeterministicFixture: record.IsDeterministicFixture,
            SegmentCount: record.SegmentCount,
            SegmentSnapshotSignature: record.SegmentSnapshotSignature,
            SourceMetadata: record.SourceMetadata,
            CreatedAtUtc: record.CreatedAtUtc,
            UpdatedAtUtc: record.UpdatedAtUtc);
}
