namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Keon;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

public static class AdminEndpoints
{
    private const string KnowledgeIngestOverrideHeader = "X-BioStack-Admin-Override";
    private const string KnowledgeIngestOverrideValue = "canonical-knowledge-ingest";

    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/knowledge/ingest", async (
            [FromBody] List<KnowledgeEntry> entries,
            HttpRequest httpRequest,
            [FromServices] IConfiguration configuration,
            [FromServices] IKnowledgeSource knowledgeSource,
            [FromServices] IMemoryCache memoryCache,
            [FromServices] ILoggerFactory loggerFactory,
            [FromServices] IRuntimeReceiptFactory receipts,
            [FromServices] ICurrentUserAccessor currentUser,
            CancellationToken ct) =>
        {
            var enabled = configuration.GetValue<bool>("Admin:KnowledgeIngest:Enabled");
            if (!enabled)
            {
                return Results.NotFound();
            }

            if (!httpRequest.Headers.TryGetValue(KnowledgeIngestOverrideHeader, out var overrideHeader) ||
                !string.Equals(overrideHeader.ToString(), KnowledgeIngestOverrideValue, StringComparison.Ordinal))
            {
                return Results.Forbid();
            }

            if (entries is null || entries.Count == 0)
            {
                return Results.BadRequest("No entries provided");
            }

            var evidenceRefs = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.CanonicalName))
                .Select(e => ReceiptRefs.Compound(e.CanonicalName))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (evidenceRefs.Count == 0)
            {
                evidenceRefs.Add("admin-override:knowledge-ingest");
            }

            await receipts.IssueAndAppendAsync(new ReceiptContext(
                ReceiptClass: ReceiptClass.AdminOverridePerformed,
                SubjectUri: "admin:knowledge-ingest",
                Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
                EvidenceRefs: evidenceRefs,
                Decision: "admin-override",
                EffectStatus: "canonical-write",
                InputHashSeed: string.Join("|", entries.Select(e => e.CanonicalName).Order(StringComparer.Ordinal))),
                ct);

            try
            {
                var count = await knowledgeSource.IngestBulkAsync(entries, ct);
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
            [FromServices] IRuntimeReceiptFactory receipts,
            [FromServices] ICurrentUserAccessor currentUser,
            CancellationToken ct) =>
        {
            try
            {
                var response = await intakeService.CreateAsync(request, ct);
                if (!response.Deduplicated)
                {
                    await receipts.IssueAndAppendAsync(new ReceiptContext(
                        ReceiptClass: ReceiptClass.SourceIntakeReceived,
                        SubjectUri: $"source-intake:{response.IntakeRequestId:N}",
                        Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
                        EvidenceRefs:
                        [
                            ReceiptRefs.SourceIntake(response.IntakeRequestId.ToString("N")),
                            ReceiptRefs.Source(request.SourceUrl.Trim()),
                        ],
                        Decision: "queued",
                        EffectStatus: "non-canonical",
                        InputHashSeed: $"{response.IntakeRequestId:N}|{request.SourceType}|{request.SourceUrl.Trim()}"),
                        ct);
                }

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
            [FromServices] IRuntimeReceiptFactory receipts,
            [FromServices] ICurrentUserAccessor currentUser,
            CancellationToken ct) =>
        {
            try
            {
                var resolved = await resolutionService.ResolveAsync(intakeRequestId, ct);
                var actor = ReceiptActor.User(currentUser.GetCurrentUserId());
                await receipts.IssueAndAppendAsync(new ReceiptContext(
                    ReceiptClass: ReceiptClass.SourceTranscriptResolved,
                    SubjectUri: $"source-intake:{intakeRequestId:N}/transcript",
                    Actor: actor,
                    EvidenceRefs:
                    [
                        ReceiptRefs.SourceIntake(intakeRequestId.ToString("N")),
                        ReceiptRefs.Source(resolved.SourceReference.SourceUrl),
                    ],
                    Decision: "resolved",
                    EffectStatus: "non-canonical",
                    InputHashSeed: $"{intakeRequestId:N}|{resolved.SourceReference.SourceUrl}|{resolved.Provider}|{resolved.Segments.Count}"),
                    ct);

                var descriptor = stagingService.Stage(resolved);
                var reviewModel = reviewService.BuildReviewModel(descriptor);
                var now = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
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
                    createdAtUtc: now,
                    updatedAtUtc: now,
                    intakeRequestId: intakeRequestId);

                await reviewStore.UpsertAsync(record, ct);
                await receipts.IssueAndAppendAsync(new ReceiptContext(
                    ReceiptClass: ReceiptClass.SourceCandidateStaged,
                    SubjectUri: record.ArtifactId,
                    Actor: actor,
                    EvidenceRefs:
                    [
                        ReceiptRefs.SourceIntake(intakeRequestId.ToString("N")),
                        ReceiptRefs.StagedArtifact(record.ArtifactId),
                        ReceiptRefs.Source(record.SourceUrl),
                    ],
                    Decision: record.ReviewState,
                    EffectStatus: "non-canonical",
                    InputHashSeed: $"{intakeRequestId:N}|{record.ArtifactId}|{record.SegmentSnapshotSignature}"),
                    ct);

                return Results.Ok(new AdminTranscriptIntakeResolutionResponse(
                    IntakeRequestId: intakeRequestId,
                    SourceType: resolved.SourceReference.SourceType,
                    SourceUrl: resolved.SourceReference.SourceUrl,
                    Provider: resolved.Provider,
                    Status: "resolved",
                    ResultCode: "ok",
                    SegmentCount: resolved.Segments.Count,
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
            [FromServices] BioStackDbContext db,
            CancellationToken ct) =>
        {
            var stats = new
            {
                Profiles = await db.PersonProfiles.CountAsync(ct),
                KnowledgeEntries = await db.KnowledgeEntries.CountAsync(ct),
                TotalCompoundRecords = await db.CompoundRecords.CountAsync(ct),
                TotalCheckIns = await db.CheckIns.CountAsync(ct),
                RegisteredUsers = await db.AppUsers.CountAsync(ct),
            };
            return Results.Ok(stats);
        });

        group.MapPost("/users/{userId:guid}/promote", async (
            Guid userId,
            [FromServices] IAppUserRepository userRepo,
            [FromServices] BioStackDbContext db,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(userId, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.Role = BioStack.Domain.Enums.UserRole.Admin;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id, user.Email, Role = (int)user.Role });
        });

        group.MapPost("/users/{userId:guid}/demote", async (
            Guid userId,
            [FromServices] IAppUserRepository userRepo,
            [FromServices] BioStackDbContext db,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(userId, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.Role = BioStack.Domain.Enums.UserRole.User;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { user.Id, user.Email, Role = (int)user.Role });
        });

        group.MapGet("/users", async (
            [FromServices] BioStackDbContext db,
            CancellationToken ct) =>
        {
            var users = await db.AppUsers
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.DisplayName,
                    u.AvatarUrl,
                    Role = (int)u.Role,
                    u.Provider,
                    u.CreatedAtUtc,
                    u.LastSeenAtUtc,
                })
                .ToListAsync(ct);
            return Results.Ok(users);
        });

        group.MapGet("/staged-transcript-candidate-reviews", async (
            [FromQuery] string? reviewState,
            [FromQuery] bool? promoted,
            [FromQuery] bool? targetAssigned,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            CancellationToken ct) =>
        {
            if (reviewState is not null && string.IsNullOrWhiteSpace(reviewState))
            {
                return Results.BadRequest(new { Message = "reviewState cannot be whitespace." });
            }

            try
            {
                var filter = new TranscriptCandidateReviewFilter(
                    ReviewState: reviewState,
                    IsPromoted: promoted,
                    IsTargetAssigned: targetAssigned);
                var records = await reviewStore.ListAsync(filter, ct);
                return Results.Ok(records.Select(MapStagedReviewRecordToResponse).ToArray());
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
            return record is null ? Results.NotFound() : Results.Ok(MapStagedReviewRecordToResponse(record));
        });

        group.MapPost("/staged-transcript-candidate-reviews/{artifactId}/review-state", async (
            string artifactId,
            [FromBody] AdminUpdateStagedTranscriptCandidateReviewStateRequest? request,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            [FromServices] IRuntimeReceiptFactory receipts,
            [FromServices] ICurrentUserAccessor currentUser,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                return Results.BadRequest(new { Message = "artifactId is required." });
            }

            if (request is null || string.IsNullOrWhiteSpace(request.Action))
            {
                return Results.BadRequest(new { Message = "action is required." });
            }

            var record = await reviewStore.GetByArtifactIdAsync(artifactId, ct);
            if (record is null)
            {
                return Results.NotFound();
            }

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

            var lifecycle = new TranscriptCandidateReviewLifecycle();
            var decision = lifecycle.ApplyAction(reviewModel, request.Action);
            if (!decision.IsTransitionAllowed)
            {
                return Results.UnprocessableEntity(new { Message = decision.RejectionReason });
            }

            try
            {
                var updatedAt = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                var updatedRecord = await reviewStore.UpdateReviewStateAsync(
                    artifactId: record.ArtifactId,
                    expectedCurrentReviewState: decision.FromReviewState,
                    nextReviewState: decision.ToReviewState,
                    updatedAtUtc: updatedAt,
                    cancellationToken: ct);

                await receipts.IssueAndAppendAsync(new ReceiptContext(
                    ReceiptClass: ReceiptClass.SourceReviewStateChanged,
                    SubjectUri: record.ArtifactId,
                    Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
                    EvidenceRefs: BuildReviewEvidenceRefs(updatedRecord),
                    Decision: $"{decision.FromReviewState}->{decision.ToReviewState}",
                    EffectStatus: "non-canonical",
                    InputHashSeed: $"{record.ArtifactId}|{decision.FromReviewState}|{decision.ToReviewState}|{updatedAt}"),
                    ct);

                return Results.Ok(MapStagedReviewRecordToResponse(updatedRecord));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Message = ex.Message });
            }
        });

        group.MapPost("/staged-transcript-candidate-reviews/{artifactId}/promotion-target", async (
            string artifactId,
            [FromBody] AdminAssignPromotionTargetRequest? request,
            [FromServices] ITranscriptCandidateReviewStore reviewStore,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                return Results.BadRequest(new { Message = "artifactId is required." });
            }

            if (request is null || string.IsNullOrWhiteSpace(request.TargetCanonicalName))
            {
                return Results.BadRequest(new { Message = "targetCanonicalName is required." });
            }

            try
            {
                var updatedRecord = await reviewStore.AssignPromotionTargetAsync(
                    artifactId,
                    targetCanonicalName: request.TargetCanonicalName,
                    cancellationToken: ct);
                return Results.Ok(MapStagedReviewRecordToResponse(updatedRecord));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Message = ex.Message });
            }
        });

        group.MapPost("/staged-transcript-candidate-reviews/{artifactId}/execute-promotion", async (
            string artifactId,
            [FromServices] ITranscriptCandidatePromotionService promotionService,
            [FromServices] IRuntimeReceiptFactory receipts,
            [FromServices] ICurrentUserAccessor currentUser,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                return Results.BadRequest(new { Message = "artifactId is required." });
            }

            try
            {
                var updatedRecord = await promotionService.ExecutePromotionAsync(artifactId, ct);
                await receipts.IssueAndAppendAsync(new ReceiptContext(
                    ReceiptClass: ReceiptClass.SourceArtifactPromoted,
                    SubjectUri: updatedRecord.ArtifactId,
                    Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
                    EvidenceRefs: BuildPromotionEvidenceRefs(updatedRecord),
                    Decision: "promoted",
                    EffectStatus: "canonical-write",
                    InputHashSeed: $"{updatedRecord.ArtifactId}|{updatedRecord.TargetCanonicalName}|{updatedRecord.PromotedKnowledgeEntryId}"),
                    ct);
                return Results.Ok(MapStagedReviewRecordToResponse(updatedRecord));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { Message = ex.Message });
            }
        });

        group.MapPost("/staged-transcript-candidate-reviews/{artifactId}/promotion-preview", async (
            string artifactId,
            [FromServices] ITranscriptCandidatePromotionPreviewService previewService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(artifactId))
            {
                return Results.BadRequest(new { Message = "artifactId is required." });
            }

            try
            {
                var result = await previewService.PreviewAsync(artifactId, ct);
                return Results.Ok(MapToPreviewResponse(result));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });
    }

    private static IReadOnlyDictionary<string, string>? FilterSafeProviderMetadata(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var blockedTokens = new[] { "canonical", "knowledgeentry", "promotion", "candidate", "extraction", "summary", "safety", "medical" };
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
            UpdatedAtUtc: record.UpdatedAtUtc,
            TargetCanonicalName: record.TargetCanonicalName,
            PromotedKnowledgeEntryId: record.PromotedKnowledgeEntryId,
            PromotedAtUtc: record.PromotedAtUtc,
            IntakeRequestId: record.IntakeRequestId);

    private static PromotionPreviewResponse MapToPreviewResponse(PromotionPreviewResult result)
        => new(
            ArtifactId: result.ArtifactId,
            CanPromote: result.CanPromote,
            ReviewState: result.ReviewState,
            TargetAssigned: result.TargetAssigned,
            TargetCanonicalName: result.TargetCanonicalName,
            ResolvedTargetKnowledgeEntryId: result.ResolvedTargetKnowledgeEntryId,
            AlreadyPromoted: result.AlreadyPromoted,
            PromotedKnowledgeEntryId: result.PromotedKnowledgeEntryId,
            EvidenceGate: new PromotionPreviewEvidenceGateDto(
                result.EvidenceGate.Passed,
                result.EvidenceGate.Tier,
                CitationCount: result.EvidenceGate.CitationCount,
                MechanismSummaryPresent: result.EvidenceGate.MechanismSummaryPresent,
                FailureReasons: result.EvidenceGate.FailureReasons),
            BlockingReasons: result.BlockingReasons,
            WouldWrite: result.WouldWrite);

    private static IReadOnlyList<string> BuildReviewEvidenceRefs(TranscriptCandidateReviewRecord record)
    {
        var refs = new List<string>
        {
            ReceiptRefs.StagedArtifact(record.ArtifactId),
            ReceiptRefs.Source(record.SourceUrl),
        };
        if (record.IntakeRequestId is { } intakeRequestId)
        {
            refs.Insert(0, ReceiptRefs.SourceIntake(intakeRequestId.ToString("N")));
        }

        return refs;
    }

    private static IReadOnlyList<string> BuildPromotionEvidenceRefs(TranscriptCandidateReviewRecord record)
    {
        var refs = BuildReviewEvidenceRefs(record).ToList();
        if (!string.IsNullOrWhiteSpace(record.TargetCanonicalName))
        {
            refs.Add(ReceiptRefs.Compound(record.TargetCanonicalName));
        }

        if (record.PromotedKnowledgeEntryId is { } promotedKnowledgeEntryId)
        {
            refs.Add(ReceiptRefs.KnowledgeEntry(promotedKnowledgeEntryId.ToString("N")));
        }

        return refs;
    }
}
