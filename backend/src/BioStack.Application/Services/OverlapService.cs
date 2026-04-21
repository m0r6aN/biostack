namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class OverlapService : IOverlapService
{
    private readonly IInteractionIntelligenceService _interactionIntelligenceService;
    private readonly IInteractionFlagRepository _flagRepository;

    public OverlapService(IInteractionIntelligenceService interactionIntelligenceService, IInteractionFlagRepository flagRepository)
    {
        _interactionIntelligenceService = interactionIntelligenceService;
        _flagRepository = flagRepository;
    }

    public async Task<List<InteractionFlagResponse>> CheckOverlapAsync(OverlapCheckRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CompoundNames.Count < 2)
            return new List<InteractionFlagResponse>();

        var intelligence = await _interactionIntelligenceService.EvaluateByNamesAsync(request.CompoundNames, cancellationToken);
        var flags = intelligence.Interactions
            .Where(result => result.Type != InteractionType.Neutral)
            .Select(MapToFlag)
            .ToList();

        foreach (var flag in flags)
        {
            await _flagRepository.AddAsync(flag, cancellationToken);
        }

        if (flags.Count > 0)
            await _flagRepository.SaveChangesAsync(cancellationToken);

        return flags.Select(MapToResponse).ToList();
    }

    private static InteractionFlag MapToFlag(InteractionResultResponse result)
    {
        var pathwayTag = result.SharedPathways.FirstOrDefault() ?? string.Empty;

        return new InteractionFlag
        {
            Id = Guid.NewGuid(),
            CompoundNames = new List<string> { result.CompoundA, result.CompoundB },
            OverlapType = result.Type switch
            {
                InteractionType.Synergistic => OverlapType.AdditiveBenefit,
                InteractionType.Redundant => OverlapType.PathwayOverlap,
                InteractionType.Interfering => OverlapType.PotentialInteraction,
                _ => OverlapType.Unknown
            },
            PathwayTag = pathwayTag,
            Description = result.Reason,
            EvidenceConfidence = $"Confidence {result.Confidence:0.00}",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static InteractionFlagResponse MapToResponse(InteractionFlag flag)
    {
        return new InteractionFlagResponse(
            flag.Id,
            flag.CompoundNames,
            flag.OverlapType,
            flag.PathwayTag,
            flag.Description,
            flag.EvidenceConfidence,
            flag.CreatedAtUtc
        );
    }
}

public interface IOverlapService
{
    Task<List<InteractionFlagResponse>> CheckOverlapAsync(OverlapCheckRequest request, CancellationToken cancellationToken = default);
}
