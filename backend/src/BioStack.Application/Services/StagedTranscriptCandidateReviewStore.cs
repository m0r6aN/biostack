namespace BioStack.Application.Services;

using System.Text.Json;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class StagedTranscriptCandidateReviewStore : ITranscriptCandidateReviewStore
{
    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly BioStackDbContext _dbContext;

    public StagedTranscriptCandidateReviewStore(BioStackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertAsync(TranscriptCandidateReviewRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var normalized = NormalizeRecord(record);

        var existing = await _dbContext.StagedTranscriptCandidateReviews
            .SingleOrDefaultAsync(x => x.ArtifactId == normalized.ArtifactId, cancellationToken);

        if (existing is null)
        {
            _dbContext.StagedTranscriptCandidateReviews.Add(ToEntity(normalized));
        }
        else
        {
            existing.Canonicality = normalized.Canonicality;
            existing.ReviewState = normalized.ReviewState;
            existing.SourceType = normalized.SourceType;
            existing.SourceUrl = normalized.SourceUrl;
            existing.SourceMetadataJson = SerializeMetadata(normalized.SourceMetadata);
            existing.Provider = normalized.Provider;
            existing.IsDeterministicFixture = normalized.IsDeterministicFixture;
            existing.SegmentCount = normalized.SegmentCount;
            existing.SegmentSnapshotSignature = normalized.SegmentSnapshotSignature;
            existing.CreatedAtUtc = normalized.CreatedAtUtc;
            existing.UpdatedAtUtc = normalized.UpdatedAtUtc;
            existing.TargetCanonicalName = normalized.TargetCanonicalName;
            existing.PromotedKnowledgeEntryId = normalized.PromotedKnowledgeEntryId;
            existing.PromotedAtUtc = normalized.PromotedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TranscriptCandidateReviewRecord?> GetByArtifactIdAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("ArtifactId is required.", nameof(artifactId));
        }

        var entity = await _dbContext.StagedTranscriptCandidateReviews
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ArtifactId == artifactId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<TranscriptCandidateReviewRecord>> ListAsync(
        TranscriptCandidateReviewFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.ReviewState is not null && string.IsNullOrWhiteSpace(filter.ReviewState))
        {
            throw new ArgumentException("ReviewState must not be whitespace when provided.", nameof(filter));
        }

        var query = _dbContext.StagedTranscriptCandidateReviews
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.ReviewState))
            query = query.Where(x => x.ReviewState == filter.ReviewState);

        if (filter.IsPromoted.HasValue)
            query = filter.IsPromoted.Value
                ? query.Where(x => x.PromotedKnowledgeEntryId != null)
                : query.Where(x => x.PromotedKnowledgeEntryId == null);

        if (filter.IsTargetAssigned.HasValue)
            query = filter.IsTargetAssigned.Value
                ? query.Where(x => x.TargetCanonicalName != null && x.TargetCanonicalName != "")
                : query.Where(x => x.TargetCanonicalName == null || x.TargetCanonicalName == "");

        var entities = await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToArray();
    }

    public async Task<TranscriptCandidateReviewRecord> UpdateReviewStateAsync(
        string artifactId,
        string expectedCurrentReviewState,
        string nextReviewState,
        string updatedAtUtc,
        string? expectedRowVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("ArtifactId is required.", nameof(artifactId));
        }

        if (string.IsNullOrWhiteSpace(expectedCurrentReviewState))
        {
            throw new ArgumentException("ExpectedCurrentReviewState is required.", nameof(expectedCurrentReviewState));
        }

        if (string.IsNullOrWhiteSpace(nextReviewState))
        {
            throw new ArgumentException("NextReviewState is required.", nameof(nextReviewState));
        }

        if (string.IsNullOrWhiteSpace(updatedAtUtc))
        {
            throw new ArgumentException("UpdatedAtUtc is required.", nameof(updatedAtUtc));
        }

        var entity = await _dbContext.StagedTranscriptCandidateReviews
            .SingleOrDefaultAsync(x => x.ArtifactId == artifactId, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException($"No staged transcript candidate review record found for artifactId '{artifactId}'.");
        }

        if (!string.Equals(entity.ReviewState, expectedCurrentReviewState, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ReviewState mismatch for artifactId '{artifactId}'. Expected '{expectedCurrentReviewState}', actual '{entity.ReviewState}'.");
        }

        if (!string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new InvalidOperationException(
                "RowVersion-based concurrency is not enabled for this infrastructure store.");
        }

        var nextRecord = ToRecord(entity).WithReviewState(nextReviewState, updatedAtUtc);

        entity.ReviewState = nextRecord.ReviewState;
        entity.UpdatedAtUtc = nextRecord.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(entity);
    }

    public async Task<TranscriptCandidateReviewRecord> AssignPromotionTargetAsync(
        string artifactId,
        string targetCanonicalName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("ArtifactId is required.", nameof(artifactId));
        }

        if (string.IsNullOrWhiteSpace(targetCanonicalName))
        {
            throw new ArgumentException("TargetCanonicalName is required.", nameof(targetCanonicalName));
        }

        var entity = await _dbContext.StagedTranscriptCandidateReviews
            .SingleOrDefaultAsync(x => x.ArtifactId == artifactId, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException($"No staged transcript candidate review record found for artifactId '{artifactId}'.");
        }

        if (!string.Equals(entity.ReviewState, TranscriptCandidateReviewState.ReviewApprovedForPromotion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Promotion target can only be assigned to records in state '{TranscriptCandidateReviewState.ReviewApprovedForPromotion}'. " +
                $"Current state: '{entity.ReviewState}'.");
        }

        if (entity.IsDeterministicFixture)
        {
            throw new InvalidOperationException(
                "Promotion target cannot be assigned to a deterministic fixture record.");
        }

        entity.TargetCanonicalName = targetCanonicalName;
        entity.UpdatedAtUtc = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(entity);
    }

    public async Task<TranscriptCandidateReviewRecord> RecordPromotionCompletionAsync(
        string artifactId,
        Guid promotedKnowledgeEntryId,
        string promotedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            throw new ArgumentException("ArtifactId is required.", nameof(artifactId));
        }

        if (promotedKnowledgeEntryId == Guid.Empty)
        {
            throw new ArgumentException("PromotedKnowledgeEntryId must not be empty.", nameof(promotedKnowledgeEntryId));
        }

        if (string.IsNullOrWhiteSpace(promotedAtUtc))
        {
            throw new ArgumentException("PromotedAtUtc is required.", nameof(promotedAtUtc));
        }

        var entity = await _dbContext.StagedTranscriptCandidateReviews
            .SingleOrDefaultAsync(x => x.ArtifactId == artifactId, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException($"No staged transcript candidate review record found for artifactId '{artifactId}'.");
        }

        entity.PromotedKnowledgeEntryId = promotedKnowledgeEntryId;
        entity.PromotedAtUtc = promotedAtUtc;
        entity.UpdatedAtUtc = promotedAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(entity);
    }

    private static TranscriptCandidateReviewRecord NormalizeRecord(TranscriptCandidateReviewRecord record)
        => TranscriptCandidateReviewRecord.Create(
            artifactId: record.ArtifactId,
            canonicality: record.Canonicality,
            reviewState: record.ReviewState,
            sourceType: record.SourceType,
            sourceUrl: record.SourceUrl,
            provider: record.Provider,
            isDeterministicFixture: record.IsDeterministicFixture,
            segmentCount: record.SegmentCount,
            segmentSnapshotSignature: record.SegmentSnapshotSignature,
            sourceMetadata: record.SourceMetadata,
            createdAtUtc: record.CreatedAtUtc,
            updatedAtUtc: record.UpdatedAtUtc,
            rowVersion: null,
            targetCanonicalName: record.TargetCanonicalName,
            promotedKnowledgeEntryId: record.PromotedKnowledgeEntryId,
            promotedAtUtc: record.PromotedAtUtc);

    private static StagedTranscriptCandidateReviewEntity ToEntity(TranscriptCandidateReviewRecord record)
        => new()
        {
            ArtifactId = record.ArtifactId,
            Canonicality = record.Canonicality,
            ReviewState = record.ReviewState,
            SourceType = record.SourceType,
            SourceUrl = record.SourceUrl,
            SourceMetadataJson = SerializeMetadata(record.SourceMetadata),
            Provider = record.Provider,
            IsDeterministicFixture = record.IsDeterministicFixture,
            SegmentCount = record.SegmentCount,
            SegmentSnapshotSignature = record.SegmentSnapshotSignature,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            TargetCanonicalName = record.TargetCanonicalName,
            PromotedKnowledgeEntryId = record.PromotedKnowledgeEntryId,
            PromotedAtUtc = record.PromotedAtUtc,
        };

    private static TranscriptCandidateReviewRecord ToRecord(StagedTranscriptCandidateReviewEntity entity)
        => TranscriptCandidateReviewRecord.Create(
            artifactId: entity.ArtifactId,
            canonicality: entity.Canonicality,
            reviewState: entity.ReviewState,
            sourceType: entity.SourceType,
            sourceUrl: entity.SourceUrl,
            provider: entity.Provider,
            isDeterministicFixture: entity.IsDeterministicFixture,
            segmentCount: entity.SegmentCount,
            segmentSnapshotSignature: entity.SegmentSnapshotSignature,
            sourceMetadata: DeserializeMetadata(entity.SourceMetadataJson),
            createdAtUtc: entity.CreatedAtUtc,
            updatedAtUtc: entity.UpdatedAtUtc,
            rowVersion: null,
            targetCanonicalName: entity.TargetCanonicalName,
            promotedKnowledgeEntryId: entity.PromotedKnowledgeEntryId,
            promotedAtUtc: entity.PromotedAtUtc);

    private static string SerializeMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        var ordered = metadata
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

        return JsonSerializer.Serialize(ordered, MetadataJsonOptions);
    }

    private static IReadOnlyDictionary<string, string> DeserializeMetadata(string json)
    {
        var raw = string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json, MetadataJsonOptions)
              ?? new Dictionary<string, string>(StringComparer.Ordinal);

        return raw
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
    }
}
