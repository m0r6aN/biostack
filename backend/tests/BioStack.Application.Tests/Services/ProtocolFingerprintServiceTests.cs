namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Responses;
using Xunit;

public sealed class ProtocolFingerprintServiceTests
{
    private readonly IProtocolFingerprintService _service = new ProtocolFingerprintService();

    [Fact]
    public void NormalizedProtocolHash_MatchesSemanticEquivalents()
    {
        var left = new NormalizedProtocol(
            new List<NormalizedProtocolCompound>
            {
                new("BPC-157", 500, "mcg", "daily", string.Empty, string.Empty, true)
            },
            new List<ProtocolBlendExpansionResponse>());

        var right = new NormalizedProtocol(
            new List<NormalizedProtocolCompound>
            {
                new("BPC-157", 500, "mg", "daily", string.Empty, string.Empty, true)
            },
            new List<ProtocolBlendExpansionResponse>());

        Assert.Equal(_service.GetNormalizedProtocolHash(left), _service.GetNormalizedProtocolHash(right));
    }

    [Fact]
    public void AnalysisKey_ChangesWithVersionStamp()
    {
        var protocol = new NormalizedProtocol(
            new List<NormalizedProtocolCompound> { new("BPC-157", 500, "mcg", "daily", string.Empty, string.Empty, true) },
            new List<ProtocolBlendExpansionResponse>());
        var left = new AnalysisContext("healing", string.Empty, "30-39", "180-219", new List<string>(), "v2", "v1", "v2");
        var right = left with { ScoringVersion = "v3" };

        Assert.NotEqual(_service.GetAnalysisKey(protocol, left), _service.GetAnalysisKey(protocol, right));
    }
}
