namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Repositories;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;

public sealed class OverlapService : IOverlapService
{
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IInteractionFlagRepository _flagRepository;

    public OverlapService(IKnowledgeSource knowledgeSource, IInteractionFlagRepository flagRepository)
    {
        _knowledgeSource = knowledgeSource;
        _flagRepository = flagRepository;
    }

    public async Task<List<InteractionFlagResponse>> CheckOverlapAsync(OverlapCheckRequest request, CancellationToken cancellationToken = default)
    {
        var flags = new List<InteractionFlagResponse>();

        if (request.CompoundNames.Count < 2)
            return flags;

        var knowledgeEntries = new List<KnowledgeEntry>();
        foreach (var name in request.CompoundNames)
        {
            var entry = await _knowledgeSource.GetCompoundAsync(name, cancellationToken);
            if (entry is not null)
                knowledgeEntries.Add(entry);
        }

        for (int i = 0; i < knowledgeEntries.Count; i++)
        {
            for (int j = i + 1; j < knowledgeEntries.Count; j++)
            {
                var compound1 = knowledgeEntries[i];
                var compound2 = knowledgeEntries[j];

                var overlappingPathways = compound1.Pathways
                    .Intersect(compound2.Pathways)
                    .ToList();

                if (overlappingPathways.Count > 0)
                {
                    foreach (var pathway in overlappingPathways)
                    {
                        var flag = new InteractionFlag
                        {
                            Id = Guid.NewGuid(),
                            CompoundNames = new List<string> { compound1.CanonicalName, compound2.CanonicalName },
                            OverlapType = OverlapType.PathwayOverlap,
                            PathwayTag = pathway,
                            Description = $"Both {compound1.CanonicalName} and {compound2.CanonicalName} are associated with the {pathway} pathway. Educational reference only.",
                            EvidenceConfidence = "Limited — Educational reference only",
                            CreatedAtUtc = DateTime.UtcNow
                        };

                        await _flagRepository.AddAsync(flag, cancellationToken);
                        flags.Add(MapToResponse(flag));
                    }
                }
            }
        }

        if (flags.Count > 0)
            await _flagRepository.SaveChangesAsync(cancellationToken);

        return flags;
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
