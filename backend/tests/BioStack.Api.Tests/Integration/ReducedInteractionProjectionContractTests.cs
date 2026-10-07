namespace BioStack.Api.Tests.Integration;

using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using Xunit;

public sealed class ReducedInteractionProjectionContractTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData(InteractionType.Synergistic, true)]
    [InlineData(InteractionType.Complementary, true)]
    [InlineData(InteractionType.Redundant, true)]
    [InlineData(InteractionType.Interfering, true)]
    [InlineData(InteractionType.Neutral, false)]
    [InlineData(InteractionType.Unknown, false)]
    public void Pair_Signals_Have_Only_Identity_And_Explicit_Unavailable_Severity(InteractionType type, bool included)
    {
        var full = new InteractionIntelligenceResponse(new(1, 2, 3), new(1, 2, 3), 42,
            [], [new("A", "B", type, 0.99, ["private-pathway"], "private-reason", true)], [], []);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(InteractionIntelligenceProjection.Project(full, false), Options));
        Assert.Equal(new[] { "pairs" }, doc.RootElement.EnumerateObject().Select(p => p.Name));
        var pairs = doc.RootElement.GetProperty("pairs").EnumerateArray().ToArray();
        if (!included) { Assert.Empty(pairs); return; }
        var pair = Assert.Single(pairs);
        Assert.Equal(new[] { "compoundA", "compoundB", "severity" }, pair.EnumerateObject().Select(p => p.Name).OrderBy(x => x));
        Assert.Equal("A", pair.GetProperty("compoundA").GetString());
        Assert.Equal("B", pair.GetProperty("compoundB").GetString());
        Assert.Equal(JsonValueKind.Null, pair.GetProperty("severity").ValueKind);
        Assert.Same(full, InteractionIntelligenceProjection.Project(full, true));
    }

    [Theory]
    [InlineData(OverlapType.AdditiveBenefit, true)]
    [InlineData(OverlapType.PathwayOverlap, true)]
    [InlineData(OverlapType.MechanismicSimilarity, true)]
    [InlineData(OverlapType.PotentialInteraction, true)]
    [InlineData(OverlapType.Unknown, false)]
    public void Flag_Signals_Do_Not_Expose_Type_Pathway_Or_Confidence(OverlapType type, bool included)
    {
        var id = Guid.NewGuid();
        var full = new List<InteractionFlagResponse> { new(id, ["A", "B"], type, "private-pathway", "private-reason", "high confidence", DateTime.UtcNow) };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(InteractionIntelligenceProjection.ProjectFlags(full, false), Options));
        var flags = doc.RootElement.EnumerateArray().ToArray();
        if (!included) { Assert.Empty(flags); return; }
        var flag = Assert.Single(flags);
        Assert.Equal(new[] { "compoundNames", "createdAtUtc", "id", "severity" }, flag.EnumerateObject().Select(p => p.Name).OrderBy(x => x));
        Assert.Equal(id, flag.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, flag.GetProperty("severity").ValueKind);
        Assert.Same(full, InteractionIntelligenceProjection.ProjectFlags(full, true));
    }
}
