namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;

public static class CompoundInteractionHintCatalog
{
    public static IReadOnlyList<CompoundInteractionHint> Defaults { get; } = new List<CompoundInteractionHint>
    {
        Hint("BPC-157", "TB-500", InteractionType.Synergistic, 0.85m, new[] { "tissue-repair", "angiogenesis" }, "Known repair-stack pairing with overlapping recovery intent."),
        Hint("Semaglutide", "Tirzepatide", InteractionType.Redundant, 0.88m, new[] { "incretin-signaling", "appetite-regulation" }, "Stacking incretin agonists usually creates overlap rather than a clean additive gain."),
        Hint("Semaglutide", "Liraglutide", InteractionType.Redundant, 0.84m, new[] { "incretin-signaling", "glucose-regulation" }, "GLP-1 agonist overlap suggests redundancy before new signal."),
        Hint("Tirzepatide", "Retatrutide", InteractionType.Redundant, 0.76m, new[] { "incretin-signaling", "appetite-regulation" }, "Dual and triple incretin exposure should be interpreted as overlap-heavy."),
        Hint("Tesamorelin", "CJC-1295", InteractionType.Redundant, 0.71m, new[] { "GH-IGF-1 axis", "pituitary-signaling" }, "Shared growth-hormone-axis stimulation can blur attribution."),
        Hint("Tesamorelin", "Ipamorelin", InteractionType.Synergistic, 0.67m, new[] { "GH-IGF-1 axis", "pituitary-signaling" }, "Different GH-axis entry points can look complementary, but still need review."),
        Hint("Enclomiphene", "Tamoxifen", InteractionType.Redundant, 0.63m, new[] { "estrogen-receptor-modulation", "HPTA-signaling" }, "Multiple SERM-style levers can compress the same signaling lane."),
        Hint("Tamoxifen", "Raloxifene", InteractionType.Redundant, 0.74m, new[] { "estrogen-receptor-modulation" }, "Two SERMs in the same stack usually means more overlap than range."),
        Hint("Ostarine", "Ligandrol", InteractionType.Redundant, 0.72m, new[] { "androgen-receptor-signaling", "anabolism-context" }, "Parallel SARM signaling is usually overlapping rather than broadening."),
        Hint("Clomiphene", "Testosterone cypionate", InteractionType.Interfering, 0.61m, new[] { "HPTA-signaling", "androgen-receptor-signaling" }, "Feedback dynamics can make interpretation messy when both are active."),
        Hint("Metformin", "Berberine", InteractionType.Redundant, 0.66m, new[] { "AMPK-signaling", "glucose-regulation" }, "Shared glucose-handling pathways can reduce signal clarity."),
        Hint("Creatine monohydrate", "L-carnitine", InteractionType.Synergistic, 0.52m, new[] { "mitochondrial-energy", "cellular-energy" }, "Energy-support pairing can be complementary, but evidence is still mixed.")
    };

    public static async Task SeedDefaultsAsync(ICompoundInteractionHintRepository repository, CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetAllAsync(cancellationToken);
        var existingPairs = existing
            .Select(hint => CompoundInteractionHintRepository.NormalizePair(hint.CompoundA, hint.CompoundB))
            .ToHashSet();

        foreach (var hint in Defaults)
        {
            var normalized = CompoundInteractionHintRepository.NormalizePair(hint.CompoundA, hint.CompoundB);
            if (existingPairs.Contains(normalized))
            {
                continue;
            }

            await repository.AddAsync(new CompoundInteractionHint
            {
                Id = hint.Id,
                CompoundA = normalized.CompoundA,
                CompoundB = normalized.CompoundB,
                InteractionType = hint.InteractionType,
                Strength = hint.Strength,
                MechanismOverlap = hint.MechanismOverlap is null ? null : new List<string>(hint.MechanismOverlap),
                Notes = hint.Notes,
                CreatedAtUtc = hint.CreatedAtUtc
            }, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }

    private static CompoundInteractionHint Hint(
        string compoundA,
        string compoundB,
        InteractionType type,
        decimal strength,
        IEnumerable<string>? mechanismOverlap,
        string notes)
    {
        var normalized = CompoundInteractionHintRepository.NormalizePair(compoundA, compoundB);

        return new CompoundInteractionHint
        {
            Id = Guid.NewGuid(),
            CompoundA = normalized.CompoundA,
            CompoundB = normalized.CompoundB,
            InteractionType = type,
            Strength = strength,
            MechanismOverlap = mechanismOverlap?.ToList(),
            Notes = notes,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
