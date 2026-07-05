namespace BioStack.Application.Services;

using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IKnowledgeSourceIntakeService
{
    Task<AdminKnowledgeSourceIntakeResponse> CreateAsync(AdminKnowledgeSourceIntakeRequest request, CancellationToken cancellationToken = default);
}

public sealed class KnowledgeSourceIntakeService : IKnowledgeSourceIntakeService
{
    private const int MinMaxVideos = 1;
    private const int MaxMaxVideos = 200;

    private readonly BioStackDbContext _dbContext;

    public KnowledgeSourceIntakeService(BioStackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminKnowledgeSourceIntakeResponse> CreateAsync(
        AdminKnowledgeSourceIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var sourceTypeValue = ToSourceTypeValue(request.SourceType);
        var sourceUrl = request.SourceUrl.Trim();

        // Ordered client-side: the SQLite provider cannot translate ORDER BY on DateTimeOffset,
        // and the duplicate set for a single (SourceUrl, SourceType) is tiny. Oldest queued wins.
        var existing = (await _dbContext.KnowledgeSourceIntakeRequests
                .Where(x => x.SourceUrl == sourceUrl && x.SourceType == sourceTypeValue && x.Status == "queued")
                .ToListAsync(cancellationToken))
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefault();

        if (existing is not null)
        {
            return new AdminKnowledgeSourceIntakeResponse(
                IntakeRequestId: existing.Id,
                Status: existing.Status,
                CreatedAtUtc: existing.CreatedAtUtc,
                Message: "Duplicate request: existing queued intake request returned.",
                Deduplicated: true);
        }

        var entity = new KnowledgeSourceIntakeRequest
        {
            Id = Guid.NewGuid(),
            SourceType = sourceTypeValue,
            SourceUrl = sourceUrl,
            OptionalInstructions = string.IsNullOrWhiteSpace(request.OptionalInstructions)
                ? null
                : request.OptionalInstructions.Trim(),
            RequestedOutputs = request.RequestedOutputs
                .Select(ToRequestedOutputValue)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            MaxVideos = request.ChannelOptions?.MaxVideos,
            PublishedAfterUtc = request.ChannelOptions?.PublishedAfterUtc,
            PublishedBeforeUtc = request.ChannelOptions?.PublishedBeforeUtc,
            Status = "queued",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        await _dbContext.KnowledgeSourceIntakeRequests.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminKnowledgeSourceIntakeResponse(
            IntakeRequestId: entity.Id,
            Status: entity.Status,
            CreatedAtUtc: entity.CreatedAtUtc,
            Message: "Knowledge source intake request queued.");
    }

    private static void ValidateRequest(AdminKnowledgeSourceIntakeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceUrl))
        {
            throw new ArgumentException("SourceUrl is required.", nameof(request));
        }

        var url = request.SourceUrl.Trim();
        var isVideoUrl = IsSupportedVideoUrl(url);
        var isChannelUrl = IsSupportedChannelUrl(url);

        if (request.SourceType == KnowledgeSourceType.VideoUrl && !isVideoUrl)
        {
            throw new ArgumentException("sourceType=video_url requires a valid supported video URL.", nameof(request));
        }

        if (request.SourceType == KnowledgeSourceType.ChannelUrl && !isChannelUrl)
        {
            throw new ArgumentException("sourceType=channel_url requires a valid supported channel URL.", nameof(request));
        }

        if (request.SourceType == KnowledgeSourceType.VideoUrl && isChannelUrl)
        {
            throw new ArgumentException("Mismatched sourceType/url: channel URL provided for video_url.", nameof(request));
        }

        if (request.SourceType == KnowledgeSourceType.ChannelUrl && isVideoUrl)
        {
            throw new ArgumentException("Mismatched sourceType/url: video URL provided for channel_url.", nameof(request));
        }

        if (request.ChannelOptions is not null)
        {
            if (request.ChannelOptions.MaxVideos.HasValue &&
                (request.ChannelOptions.MaxVideos.Value < MinMaxVideos || request.ChannelOptions.MaxVideos.Value > MaxMaxVideos))
            {
                throw new ArgumentException($"channelOptions.maxVideos must be between {MinMaxVideos} and {MaxMaxVideos}.", nameof(request));
            }

            if (request.ChannelOptions.PublishedAfterUtc.HasValue &&
                request.ChannelOptions.PublishedBeforeUtc.HasValue &&
                request.ChannelOptions.PublishedAfterUtc.Value > request.ChannelOptions.PublishedBeforeUtc.Value)
            {
                throw new ArgumentException("channelOptions.publishedAfterUtc must be before or equal to publishedBeforeUtc.", nameof(request));
            }
        }

        if (request.RequestedOutputs is null || request.RequestedOutputs.Count == 0)
        {
            throw new ArgumentException("requestedOutputs must include at least one output area.", nameof(request));
        }
    }

    private static string ToSourceTypeValue(KnowledgeSourceType sourceType) => sourceType switch
    {
        KnowledgeSourceType.VideoUrl => "video_url",
        KnowledgeSourceType.ChannelUrl => "channel_url",
        _ => throw new ArgumentOutOfRangeException(nameof(sourceType), "Unsupported source type."),
    };

    private static string ToRequestedOutputValue(RequestedOutputArea output) => output switch
    {
        RequestedOutputArea.SourceMetadata => "source_metadata",
        RequestedOutputArea.TranscriptQuality => "transcript_quality",
        RequestedOutputArea.CoreThesis => "core_thesis",
        RequestedOutputArea.Claims => "claims",
        RequestedOutputArea.CompoundsMentioned => "compounds_mentioned",
        RequestedOutputArea.BiomarkersOrLabsMentioned => "biomarkers_or_labs_mentioned",
        RequestedOutputArea.ProtocolPhases => "protocol_phases",
        RequestedOutputArea.SafetyFlags => "safety_flags",
        RequestedOutputArea.EvidenceGaps => "evidence_gaps",
        RequestedOutputArea.RawArtifactRefs => "raw_artifact_refs",
        _ => throw new ArgumentOutOfRangeException(nameof(output), "Unsupported requested output area."),
    };

    private static bool IsSupportedVideoUrl(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();

        if (host is "www.youtube.com" or "youtube.com")
        {
            return uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase)
                && uri.Query.Contains("v=", StringComparison.OrdinalIgnoreCase);
        }

        if (host is "youtu.be")
        {
            var path = uri.AbsolutePath.Trim('/');
            return !string.IsNullOrWhiteSpace(path);
        }

        return false;
    }

    private static bool IsSupportedChannelUrl(string sourceUrl)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host is not ("www.youtube.com" or "youtube.com"))
        {
            return false;
        }

        var path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.StartsWith("channel/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("@", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("c/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("user/", StringComparison.OrdinalIgnoreCase);
    }
}
