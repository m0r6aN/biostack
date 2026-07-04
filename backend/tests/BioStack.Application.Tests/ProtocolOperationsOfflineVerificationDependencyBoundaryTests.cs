namespace BioStack.Application.Tests;

using System.Text.RegularExpressions;
using Xunit;

public sealed class ProtocolIntelligenceOfflineBoundaryTests_ProtocolOperationsOfflineVerificationDependencyBoundary
{
    private static readonly string[] OfflineVerificationFiles =
    [
        Path.Combine("backend", "src", "BioStack.Application", "Services", "ProtocolOperationsExportBundleVerifier.cs"),
        Path.Combine("backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli", "Program.cs"),
        Path.Combine("backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli", "ProtocolOperationsExportBundleVerifierCli.cs"),
        Path.Combine("backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli", "ProtocolOperationsExportBundleVerificationReceiptJson.cs"),
        Path.Combine("backend", "tools", "BioStack.ProtocolOperationsExportBundleVerifierCli", "ProtocolOperationsExportBundleVerificationReceiptJsonVerifier.cs"),
    ];

    private static readonly string[] AllowedUsingPrefixes =
    [
        "System",
        "BioStack.Application.Abstractions",
        "BioStack.Application.Services",
        "BioStack.Contracts.Responses",
    ];

    private static readonly ForbiddenPattern[] ForbiddenDependencyPatterns =
    [
        new("database context", @"\bBioStackDbContext\b|\bDbContext\b"),
        new("http client", @"\bIHttpClientFactory\b|\bHttpClient\b"),
        new("api endpoint surface", @"\bMap(?:Get|Post|Put|Delete)\b|\bFromServices\b|\bController\b"),
        new("frontend asset dependency", @"frontend[/\\]src|\.tsx\b"),
        new("pdf rendering dependency", @"\bQuestPDF\b|\bPdfSharp\b|\bPdfRenderer\b|\bPdfGenerator\b|\bDocumentGenerator\b"),
        new("protocol intelligence runtime service", @"\bProtocolIntelligenceService\b|\bProtocolIntelligenceResponse\b"),
        new("transcript or intake service", @"\bTranscript(?:Intake)?Service\b|\bIntakeService\b"),
        new("recommendation or medical guidance service", @"\bRecommendationService\b|\bDosingService\b|\bMedicalGuidance\b"),
    ];

    [Fact]
    public void OfflineVerificationSource_UsesOnlyApprovedNamespaceDependencies()
    {
        var root = RepositoryRoot();

        foreach (var relativePath in OfflineVerificationFiles)
        {
            var path = Path.Combine(root, relativePath);
            Assert.True(File.Exists(path), $"Expected '{relativePath}' to exist.");

            var usingLines = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("using ", StringComparison.Ordinal))
                .Select(line => line["using ".Length..].TrimEnd(';'))
                .ToArray();

            foreach (var usingLine in usingLines)
            {
                Assert.Contains(
                    AllowedUsingPrefixes,
                    prefix => usingLine.StartsWith(prefix, StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void OfflineVerificationSource_DoesNotReferenceForbiddenInfrastructureOrServices()
    {
        var root = RepositoryRoot();

        foreach (var relativePath in OfflineVerificationFiles)
        {
            var path = Path.Combine(root, relativePath);
            var text = File.ReadAllText(path);

            foreach (var forbiddenPattern in ForbiddenDependencyPatterns)
            {
                Assert.False(
                    Regex.IsMatch(text, forbiddenPattern.Regex, RegexOptions.CultureInvariant),
                    $"Did not expect {forbiddenPattern.Label} in '{relativePath}'.");
            }
        }
    }

    [Fact]
    public void OfflineVerificationCliSource_RemainsReadOnlyExceptForExistingStdoutReceiptEmission()
    {
        var root = RepositoryRoot();

        foreach (var relativePath in OfflineVerificationFiles.Where(path => path.Contains("BioStack.ProtocolOperationsExportBundleVerifierCli", StringComparison.Ordinal)))
        {
            var path = Path.Combine(root, relativePath);
            var text = File.ReadAllText(path);

            Assert.DoesNotContain("File.WriteAllText", text, StringComparison.Ordinal);
            Assert.DoesNotContain("File.AppendAllText", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Directory.CreateDirectory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpClient", text, StringComparison.Ordinal);
        }
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

    private sealed record ForbiddenPattern(string Label, string Regex);
}
