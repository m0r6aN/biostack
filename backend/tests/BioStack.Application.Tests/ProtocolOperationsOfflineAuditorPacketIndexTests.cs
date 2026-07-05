namespace BioStack.Application.Tests;

using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Guards the docs-only Protocol Operations offline auditor packet index. The index is an
/// inspection entry point only: it must point at existing offline-kit artifacts and preserve
/// the non-authoritative boundary without adding product surface.
/// </summary>
public sealed class ProtocolOperationsOfflineAuditorPacketIndexTests
{
    private static readonly string[] ExpectedInspectionOrder =
    [
        "backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/README.md",
        "backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RUNBOOK.md",
        "backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RELEASE_CHECKLIST.md",
        "backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/smoke-offline-kit.ps1",
        "backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/smoke-offline-kit.sh",
        "backend/tests/Fixtures/ProtocolOperationsExportBundle/ProtocolOperationsOfflineVerificationResultCodeCatalog.golden.json",
        "backend/tests/BioStack.Application.Tests/ProtocolOperationsOfflineVerificationCapstoneGuardTests.cs",
    ];

    [Fact]
    public void AuditorPacketIndex_ExistsUnderDocs()
    {
        Assert.True(
            File.Exists(IndexPath()),
            $"Expected Protocol Operations offline auditor packet index at '{IndexPath()}'.");
    }

    [Fact]
    public void AuditorPacketIndex_ListsInspectionArtifactsInDeterministicOrder()
    {
        var index = File.ReadAllText(IndexPath());
        var links = MarkdownLinks(index)
            .Where(link => ExpectedInspectionOrder.Contains(link, StringComparer.Ordinal))
            .ToArray();

        Assert.Equal(ExpectedInspectionOrder, links);

        foreach (var relativePath in links)
        {
            var absolutePath = Path.Combine(
                RepositoryRoot(),
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(absolutePath), $"Expected indexed artifact '{relativePath}' to exist.");
        }
    }

    [Fact]
    public void AuditorPacketIndex_DeclaresProofsAndNonProofsWithoutProductSurface()
    {
        var index = File.ReadAllText(IndexPath());

        Assert.Contains("What this proves", index, StringComparison.Ordinal);
        Assert.Contains("What this does not prove", index, StringComparison.Ordinal);
        Assert.Contains("air-gapped", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deterministic", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("non-authoritative", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not provide medical advice", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not generate PDFs", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not add frontend", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not add persistence", index, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not expand Protocol Intelligence runtime behavior", index, StringComparison.Ordinal);
    }

    private static string IndexPath()
    {
        return Path.Combine(
            RepositoryRoot(),
            "docs",
            "protocol-operations-offline-auditor-packet-index.md");
    }

    private static string[] MarkdownLinks(string markdown)
    {
        return Regex.Matches(markdown, @"\[[^\]]+\]\(([^)]+)\)")
            .Select(match => match.Groups[1].Value)
            .ToArray();
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
