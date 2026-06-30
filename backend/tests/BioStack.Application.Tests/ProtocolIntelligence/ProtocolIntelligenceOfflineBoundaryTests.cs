namespace BioStack.Application.Tests.ProtocolIntelligence;

using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Regression guard for the offline-only Protocol Intelligence boundary. This test scans
/// production source only; tests and docs may name forbidden legacy symbols while describing
/// the boundary.
/// </summary>
public sealed class ProtocolIntelligenceOfflineBoundaryTests
{
    private static readonly string[] SourceRoots =
    [
        Path.Combine("backend", "src", "BioStack.Api"),
        Path.Combine("backend", "src", "BioStack.Application"),
        Path.Combine("backend", "src", "BioStack.Contracts"),
        Path.Combine("backend", "src", "BioStack.KnowledgeWorker"),
        Path.Combine("frontend", "src"),
    ];

    private static readonly string[] SourceExtensions = [".cs", ".ts", ".tsx"];

    private static readonly ForbiddenPattern[] ForbiddenRuntimePatterns =
    [
        new(
            "runtime Protocol Intelligence service",
            @"\bProtocolIntelligenceService\b"),
        new(
            "runtime Protocol Intelligence response model",
            @"\bProtocolIntelligenceResponse\b"),
        new(
            "parallel forbidden-output scanner",
            @"\bForbiddenOutputScanner\b"),
        new(
            "runtime Protocol Intelligence preview route",
            @"intelligence/preview"),
        new(
            "runtime Protocol Intelligence detail route",
            @"[""']/{id}/intelligence[""']"),
        new(
            "frontend Protocol Intelligence panel",
            @"\bProtocolIntelligencePanel\b"),
        new(
            "frontend high-risk warning PI panel",
            @"\bHighRiskWarningPanel\b"),
        new(
            "frontend phase map PI panel",
            @"\bPhaseMapPanel\b"),
        new(
            "frontend side-effect ambiguity PI panel",
            @"\bSideEffectAmbiguityPanel\b"),
        new(
            "frontend source-quality PI panel",
            @"\bSourceQualityPanel\b"),
    ];

    [Fact]
    public void ProductionSource_DoesNotRestoreRuntimeOrUserFacingProtocolIntelligenceSurfaces()
    {
        var root = RepositoryRoot();
        var violations = new List<string>();

        foreach (var file in ProductionSourceFiles(root))
        {
            var relativePath = Path.GetRelativePath(root, file);
            var text = File.ReadAllText(file);

            foreach (var pattern in ForbiddenRuntimePatterns)
            {
                if (Regex.IsMatch(text, pattern.Regex, RegexOptions.CultureInvariant))
                {
                    violations.Add($"{relativePath}: restored {pattern.Label}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void CanonicalOfflineProtocolIntelligenceComponentsRemainAllowed()
    {
        var root = RepositoryRoot();

        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.Application", "ProtocolIntelligence", "ProtocolIntelligenceGate.cs"),
            "public sealed class ProtocolIntelligenceGate");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.Application", "ProtocolIntelligence", "ProtocolIntelligenceContracts.cs"),
            "public sealed record ProtocolIntelligenceArtifactSet");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.Application", "ProtocolIntelligence", "ProtocolIntelligenceArtifactLoader.cs"),
            "public sealed class ProtocolIntelligenceArtifactLoader");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.Application", "Governance", "DoctrineSanitizer.cs"),
            "public sealed class DoctrineSanitizer");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.KnowledgeWorker", "Jobs", "ProtocolIntelligenceEvaluationJob.cs"),
            "public sealed class ProtocolIntelligenceEvaluationJob");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.KnowledgeWorker", "Jobs", "ProtocolIntelligenceEvaluationJob.cs"),
            "private const string ReportVersion = \"1.1.0\"");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.KnowledgeWorker", "Jobs", "ProtocolIntelligenceEvaluationJob.cs"),
            "EvaluationSummary Summary");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.KnowledgeWorker", "Jobs", "ProtocolIntelligenceEvaluationJob.cs"),
            "string Status");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.KnowledgeWorker", "Jobs", "ProtocolIntelligenceEvaluationJob.cs"),
            "IReadOnlyList<string> Warnings");
        AssertFileContains(
            root,
            Path.Combine("backend", "src", "BioStack.KnowledgeWorker", "Jobs", "ProtocolIntelligenceEvaluationJob.cs"),
            "IReadOnlyList<string> FailureDetails");
    }

    [Fact]
    public void ArchitectureNote_DocumentsOfflineOnlyBoundary()
    {
        var root = RepositoryRoot();
        var path = Path.Combine(root, "docs", "architecture", "protocol-intelligence-offline-boundary.md");

        Assert.True(File.Exists(path), "Protocol Intelligence offline boundary note is missing.");

        var text = File.ReadAllText(path);
        Assert.Contains("offline/build-time artifact evaluation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no runtime narrative", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no public API", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no UI panel", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no parallel scanner", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ProtocolIntelligenceEvaluationJob", text, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionSourceFiles(string root)
    {
        foreach (var sourceRoot in SourceRoots)
        {
            var absoluteRoot = Path.Combine(root, sourceRoot);
            Assert.True(Directory.Exists(absoluteRoot), $"Expected source root '{sourceRoot}' to exist.");

            foreach (var file in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories))
            {
                if (SourceExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }
    }

    private static void AssertFileContains(string root, string relativePath, string expected)
    {
        var path = Path.Combine(root, relativePath);
        Assert.True(File.Exists(path), $"Expected '{relativePath}' to exist.");
        Assert.Contains(expected, File.ReadAllText(path), StringComparison.Ordinal);
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
