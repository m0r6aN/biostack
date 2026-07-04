namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierCliOfflineKitGuardTests
{
    private const string ReadmeRelativePath = @"backend\tools\BioStack.ProtocolOperationsExportBundleVerifierCli\README.md";
    private const string ScriptRelativePath = @"backend\tools\BioStack.ProtocolOperationsExportBundleVerifierCli\verify-offline-kit.ps1";
    private const string WorkflowRelativePath = @".github\workflows\protocol-operations-offline-verification-kit.yml";

    [Fact]
    public void Readme_DocumentsOfflineKitVerificationCommand()
    {
        var readmePath = GetRepoFilePath(ReadmeRelativePath);

        Assert.True(File.Exists(readmePath), $"Expected README at '{readmePath}'.");

        var readme = File.ReadAllText(readmePath);

        Assert.Contains(
            "pwsh ./backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/verify-offline-kit.ps1",
            readme,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OfflineKitScript_RunsRequiredValidationCommands()
    {
        var scriptPath = GetRepoFilePath(ScriptRelativePath);

        Assert.True(File.Exists(scriptPath), $"Expected script at '{scriptPath}'.");

        var script = File.ReadAllText(scriptPath);

        Assert.Contains("dotnet build backend/BioStack.sln", script, StringComparison.Ordinal);
        Assert.Contains("dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundle", script, StringComparison.Ordinal);
        Assert.Contains("dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolOperationsExportBundleGoldenFixture", script, StringComparison.Ordinal);
        Assert.Contains("dotnet test backend/tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter ProtocolIntelligenceOfflineBoundaryTests", script, StringComparison.Ordinal);
        Assert.Contains("dotnet test backend/tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests/BioStack.ProtocolOperationsExportBundleVerifierCli.Tests.csproj", script, StringComparison.Ordinal);
        Assert.Contains("dotnet list backend/BioStack.sln package --include-transitive --vulnerable", script, StringComparison.Ordinal);
        Assert.Contains("git diff --check", script, StringComparison.Ordinal);
    }

    [Fact]
    public void OfflineKitWorkflow_UsesFocusedGuardJob()
    {
        var workflowPath = GetRepoFilePath(WorkflowRelativePath);

        Assert.True(File.Exists(workflowPath), $"Expected workflow at '{workflowPath}'.");

        var workflow = File.ReadAllText(workflowPath);

        Assert.Contains("protocol-operations-offline-verification-kit:", workflow, StringComparison.Ordinal);
        Assert.Contains("pull_request:", workflow, StringComparison.Ordinal);
        Assert.Contains("uses: actions/checkout@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("uses: actions/setup-dotnet@v4", workflow, StringComparison.Ordinal);
        Assert.Contains("pwsh ./backend/tools/BioStack.ProtocolOperationsExportBundleVerifierCli/verify-offline-kit.ps1", workflow, StringComparison.Ordinal);
        Assert.Contains("git diff --check origin/main...HEAD", workflow, StringComparison.Ordinal);
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
