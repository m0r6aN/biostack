namespace BioStack.Application.Tests.ProtocolIntelligence;

using Xunit;

/// <summary>
/// Guards the auditor packet commercialization boundary note. The note records what could be
/// monetized later without changing BioStack's safety posture: it must sell verification confidence
/// (allowed claims) and must document the forbidden medical-authority claims so future SKUs cannot
/// drift into them. This is a boundary guard only; it adds no product or pricing surface.
/// </summary>
public sealed class ProtocolIntelligenceOfflineBoundaryTests_AuditorPacketCommercializationBoundaryNote
{
    private static readonly string[] AllowedClaimConcepts =
    [
        "verifies supplied artifact integrity",
        "recomputes deterministic hashes",
        "validates receipt binding",
        "documents the offline verification workflow",
        "supports audit review of exported protocol-operation records",
    ];

    private static readonly string[] ForbiddenClaimConcepts =
    [
        "certifies medical correctness",
        "validates dosing",
        "proves treatment safety",
        "authenticates clinical appropriateness",
        "proves database state",
        "proves PDF authenticity",
        "proves runtime execution behavior",
        "guarantees a health outcome",
    ];

    private static readonly string[] AudienceConcepts =
    [
        "researcher",
        "self-tracker",
        "internal reviewer",
        "compliance reviewer",
        "external auditor",
    ];

    private static readonly string[] SkuHeadings =
    [
        "Offline Verification Kit",
        "Auditor Packet Export",
        "Compliance Review Bundle",
        "Team Governance Evidence Pack",
        "Enterprise Verification Support",
    ];

    [Fact]
    public void CommercializationNote_ExistsAndSellsVerificationConfidenceNotMedicalAuthority()
    {
        var text = ReadNote();

        Assert.Contains("monetize verification confidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Global allowed claims", text, StringComparison.Ordinal);
        Assert.Contains("Global forbidden claims", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CommercializationNote_ListsEverySku_WithAudiences()
    {
        var text = ReadNote();

        foreach (var heading in SkuHeadings)
        {
            Assert.Contains(heading, text, StringComparison.Ordinal);
        }

        foreach (var audience in AudienceConcepts)
        {
            Assert.Contains(audience, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CommercializationNote_DeclaresAllowedClaims()
    {
        var text = ReadNote();

        foreach (var claim in AllowedClaimConcepts)
        {
            Assert.Contains(claim, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CommercializationNote_DocumentsEveryForbiddenMedicalAuthorityClaim()
    {
        var text = ReadNote();

        foreach (var claim in ForbiddenClaimConcepts)
        {
            Assert.Contains(claim, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReadNote()
    {
        var path = Path.Combine(
            RepositoryRoot(),
            "docs",
            "architecture",
            "protocol-operations-auditor-packet-commercialization-boundary.md");

        Assert.True(File.Exists(path), $"Expected commercialization boundary note at '{path}'.");
        return File.ReadAllText(path);
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "BioStack.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate BioStack repository root.");
    }
}
