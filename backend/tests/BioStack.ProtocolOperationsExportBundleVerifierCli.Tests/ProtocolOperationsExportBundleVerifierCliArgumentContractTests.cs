namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

public sealed class ProtocolOperationsExportBundleVerifierCliArgumentContractTests
{
    [Fact]
    public void Run_PrintsStableHelp_WhenHelpIsRequested()
    {
        var result = InvokeCli("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Contains("Usage:", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("--receipt-json", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("--verify-receipt-json", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsUsageError_ForUnknownOption()
    {
        using var file = TempFile.Create("{}");

        var result = InvokeCli("--unknown-option", file.Path);

        AssertUsageError(result, "unknown-argument");
    }

    [Theory]
    [InlineData("--receipt-json")]
    [InlineData("--verify-receipt-json")]
    public void Run_ReturnsUsageError_WhenModeOptionHasNoPath(string option)
    {
        var result = InvokeCli(option);

        AssertUsageError(result, "input-path-required");
    }

    [Fact]
    public void Run_ReturnsUsageError_ForMutuallyInvalidModeOptions()
    {
        using var file = TempFile.Create("{}");

        var result = InvokeCli("--receipt-json", "--verify-receipt-json", file.Path);

        AssertUsageError(result, "mode-conflict");
    }

    [Fact]
    public void Run_ReturnsUsageError_ForEmptyPath()
    {
        var result = InvokeCli(" ");

        AssertUsageError(result, "input-path-required");
    }

    [Fact]
    public void Run_ReturnsInvalidInput_WhenBundlePathIsDirectory()
    {
        using var directory = TempDirectory.Create();

        var result = InvokeCli(directory.Path);

        Assert.Equal(4, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Contains("status: invalid-input", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- input-path-is-directory", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ReturnsUsageError_WhenReceiptPathIsDirectory()
    {
        using var directory = TempDirectory.Create();

        var result = InvokeCli("--verify-receipt-json", directory.Path);

        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Contains("Protocol Operations Export Bundle Verification Receipt: INVALID", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("Status: invalid-input", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("Errors: input-path-is-directory", result.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_ArgumentErrors_DoNotUseMedicalRuntimePdfOrPersistenceWording()
    {
        var result = InvokeCli("--unknown-option");

        Assert.Equal(2, result.ExitCode);
        Assert.DoesNotContain("diagnosis", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("treatment", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protocol intelligence runtime", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pdf", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("persist", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertUsageError(
        (int ExitCode, string StandardOutput, string StandardError) result,
        string expectedError)
    {
        Assert.Equal(2, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardError);
        Assert.Contains("invalid-input", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains(expectedError, result.StandardOutput, StringComparison.Ordinal);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path) => Path = path;

        public string Path { get; }

        public static TempFile Create(string content)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            File.WriteAllText(path, content);
            return new TempFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path) => Path = path;

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path);
            }
        }
    }
}
