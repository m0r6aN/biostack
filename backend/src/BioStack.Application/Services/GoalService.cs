namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Repositories;

public sealed class GoalService : IGoalService
{
    private static readonly IReadOnlyList<GoalDefinitionResponse> Definitions =
    [
        new("recovery-muscles", "Muscle, joint, and tendon recovery", "recovery", "Observe comfort, function, and recovery patterns over time", true),
        new("recovery-inflammation", "Inflammation-related patterns", "recovery", "Track user-reported inflammation-related changes over time", true),
        new("recovery-injury", "Injury recovery", "recovery", "Observe recovery trends following an injury", true),
        new("recovery-post-workout", "Post-workout recovery", "recovery", "Track soreness and return-to-baseline after training", true),

        new("energy-levels", "Daily energy", "energy", "Observe self-reported energy patterns across daily routines", true),
        new("energy-mitochondrial", "Cellular energy context", "energy", "Organize observations related to cellular energy evidence", true),
        new("energy-metabolic", "Metabolic patterns", "energy", "Track weight, appetite, and energy trends over time", true),
        new("energy-fat-loss", "Body composition", "energy", "Observe weight and body-composition trends without prescribing a target", true),

        new("cognitive-focus", "Focus and clarity", "cognitive", "Track self-reported attention and clarity patterns", true),
        new("cognitive-memory", "Memory", "cognitive", "Observe self-reported working and long-term memory patterns", true),
        new("cognitive-performance", "Cognitive performance", "cognitive", "Track self-reported mental processing and output", true),
        new("cognitive-neuro-health", "Neurological health context", "cognitive", "Organize neurological observations for longitudinal review", true),

        new("longevity-aging", "Aging-related changes", "longevity", "Observe visible and functional changes over time", true),
        new("longevity-cellular", "Cellular repair context", "longevity", "Organize observations related to cellular repair evidence", true),
        new("longevity-pathways", "Longevity pathway context", "longevity", "Track observations alongside evidence about longevity-associated pathways", true),

        new("performance-endurance", "Endurance", "performance", "Track stamina and aerobic-capacity observations", true),
        new("performance-strength", "Strength output", "performance", "Observe strength and power trends over time", true),
        new("performance-training", "Training capacity", "performance", "Track training volume, intensity, and recovery patterns", true),

        new("skin-elasticity", "Skin elasticity", "skin", "Observe changes in skin firmness and elasticity", true),
        new("skin-appearance", "Skin appearance", "skin", "Track self-reported tone, texture, and skin quality", true),
        new("skin-collagen", "Collagen context", "skin", "Organize skin observations alongside collagen-related evidence", true),

        new("organ-health", "Organ health context", "organ", "Organize user-entered observations for longitudinal review", true),
        new("organ-gut", "Digestive patterns", "organ", "Track self-reported digestive and gastrointestinal patterns", true),
        new("organ-cardiovascular", "Cardiovascular context", "organ", "Organize cardiovascular observations for longitudinal review", true),
    ];

    private static readonly IReadOnlyDictionary<string, GoalDefinitionResponse> DefinitionsById =
        Definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);

    private readonly IProfileGoalRepository _profileGoalRepository;
    private readonly IOwnershipGuard _ownershipGuard;

    public GoalService(IProfileGoalRepository profileGoalRepository, IOwnershipGuard ownershipGuard)
    {
        _profileGoalRepository = profileGoalRepository;
        _ownershipGuard = ownershipGuard;
    }

    public IReadOnlyList<GoalDefinitionResponse> GetDefinitions() => Definitions;

    public async Task<IReadOnlyList<ProfileGoalResponse>> GetProfileGoalsAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, cancellationToken);
        var profileGoals = await _profileGoalRepository.GetByProfileIdAsync(profileId, cancellationToken);
        return MapResponses(profileId, profileGoals);
    }

    public async Task<IReadOnlyList<ProfileGoalResponse>> SetProfileGoalsAsync(
        Guid profileId,
        IEnumerable<string>? goalIds,
        CancellationToken cancellationToken = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, cancellationToken);

        if (goalIds is null)
        {
            throw new ArgumentException("goalIds is required.", nameof(goalIds));
        }

        var requestedIds = goalIds
            .Select(id => id?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var unknownIds = requestedIds
            .Where(id => !DefinitionsById.TryGetValue(id, out var definition) || !definition.IsActive)
            .ToArray();
        if (unknownIds.Length > 0)
        {
            throw new ArgumentException(
                $"Unknown or inactive goal IDs: {string.Join(", ", unknownIds)}.",
                nameof(goalIds));
        }

        var existing = await _profileGoalRepository.GetByProfileIdAsync(profileId, cancellationToken);
        var requestedSet = requestedIds.ToHashSet(StringComparer.Ordinal);
        var removed = existing.Where(goal => !requestedSet.Contains(goal.GoalDefinitionId)).ToArray();
        _profileGoalRepository.RemoveRange(removed);

        var existingIds = existing.Select(goal => goal.GoalDefinitionId).ToHashSet(StringComparer.Ordinal);
        foreach (var goalDefinitionId in requestedIds.Where(id => !existingIds.Contains(id)))
        {
            await _profileGoalRepository.AddAsync(new ProfileGoal
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                GoalDefinitionId = goalDefinitionId,
                CreatedAtUtc = DateTime.UtcNow,
            }, cancellationToken);
        }

        await _profileGoalRepository.SaveChangesAsync(cancellationToken);
        var saved = await _profileGoalRepository.GetByProfileIdAsync(profileId, cancellationToken);
        return MapResponses(profileId, saved);
    }

    private static IReadOnlyList<ProfileGoalResponse> MapResponses(
        Guid profileId,
        IReadOnlyCollection<ProfileGoal> profileGoals)
    {
        var byDefinitionId = profileGoals.ToDictionary(goal => goal.GoalDefinitionId, StringComparer.Ordinal);
        return Definitions
            .Where(definition => byDefinitionId.ContainsKey(definition.Id))
            .Select(definition =>
            {
                var goal = byDefinitionId[definition.Id];
                return new ProfileGoalResponse(goal.Id, profileId, goal.GoalDefinitionId, definition, goal.CreatedAtUtc);
            })
            .ToArray();
    }
}

public interface IGoalService
{
    IReadOnlyList<GoalDefinitionResponse> GetDefinitions();
    Task<IReadOnlyList<ProfileGoalResponse>> GetProfileGoalsAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfileGoalResponse>> SetProfileGoalsAsync(Guid profileId, IEnumerable<string>? goalIds, CancellationToken cancellationToken = default);
}
