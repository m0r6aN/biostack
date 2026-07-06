namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using Xunit;

public sealed class ProtocolOperationsOfflineVerificationReleaseChecklistTests
{
    private const string ChecklistRelativePath =
        "backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/OFFLINE_VERIFICATION_RELEASE_CHECKLIST.md";

    [Fact]
    public void ReleaseChecklist_ExistsInOfflineVerificationDocsArea()
    {
        var checklistPath = GetRepoFilePath(ChecklistRelativePath);
        Assert.True(File.Exists(checklistPath), $"Expected release checklist at '{checklistPath}'.");
    }

    [Fact]
    public void ReleaseChecklist_DocumentsRequiredCommandsReviewStepsAndReleaseBlockers()
    {
        var checklist = File.ReadAllText(GetRepoFilePath(ChecklistRelativePath));

        Assert.Contains("dotnet build backend/BioStack.sln", checklist, StringComparison.Ordinal);
        Assert.Contains(
            "dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests",
            checklist,
            StringComparison.Ordinal);
        Assert.Contains(
            "dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj",
            checklist,
            StringComparison.Ordinal);
        Assert.Contains("dotnet list backend/BioStack.sln package --include-transitive --vulnerable", checklist, StringComparison.Ordinal);
        Assert.Contains("git diff --check origin/main...HEAD", checklist, StringComparison.Ordinal);
        Assert.Contains("boundary-language review", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reviewer checklist", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("allowed changes", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forbidden changes", checklist, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("medical advice", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dosing guidance", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diagnosis", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("treatment", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prescription", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PDF generation", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PDF authenticity claims", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("persistence/database-state verification claims", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Protocol Intelligence runtime/user-facing claims", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("network calls from offline verification paths", checklist, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sample personal health data", checklist, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate) || Directory.Exists(Path.GetDirectoryName(candidate)!))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.GetFullPath(relativePath, AppContext.BaseDirectory);
    }
}
