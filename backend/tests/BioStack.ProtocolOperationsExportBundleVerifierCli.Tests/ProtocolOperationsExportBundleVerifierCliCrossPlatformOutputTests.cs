namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

/// <summary>
/// Cross-platform output normalization guard: proves that CLI stdout/stderr, `--result-json` payloads,
/// and receipt JSON payloads are stable and OS-independent. This is a tests-only guard — it does not
/// change any production behavior. It asserts what the current implementation already guarantees:
/// compact (non-indented) JSON with no embedded newlines, no OS path separators or host-identifying
/// data echoed into output, stable dash-token error codes regardless of input path shape, and
/// byte-identical output across repeated runs and across Windows-style vs POSIX-style input paths.
/// </summary>
public sealed class ProtocolOperationsExportBundleVerifierCliCrossPlatformOutputTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";

    // Deny-list tokens that must never appear in emitted output. These are the classic sources of
    // cross-platform / cross-machine non-determinism: usernames, machine names, temp-dir names,
    // drive-letter paths, stack frames, and raw exception text.
    private static readonly string[] StackFrameMarkers = ["   at ", "\tat ", "Exception"];

    [Fact]
    public void ResultJson_ForBundleVerification_ContainsNoRawNewlines()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var result = InvokeCli("--result-json", bundlePath);

        Assert.Equal(0, result.ExitCode);
        AssertNoRawNewlines(result.StandardOutput);
    }

    [Fact]
    public void ReceiptJson_ContainsNoRawNewlines()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var result = InvokeCli("--receipt-json", bundlePath);

        Assert.Equal(0, result.ExitCode);
        AssertNoRawNewlines(result.StandardOutput);
    }

    [Fact]
    public void ResultJson_ForReceiptVerification_ContainsNoRawNewlines()
    {
        var receiptJson = InvokeCli("--receipt-json", CreateTempJsonFile(ReadFixtureJson())).StandardOutput;
        var receiptPath = CreateTempJsonFile(receiptJson);

        var result = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);

        Assert.Equal(0, result.ExitCode);
        AssertNoRawNewlines(result.StandardOutput);
    }

    [Fact]
    public void HumanReadableSummary_NormalizesAcrossNewlineConventions_ForValidGoldenFixture()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var result = InvokeCli(bundlePath);
        var normalizedLines = NormalizeLines(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            [
                "status: verified",
                "schema-version: 1.0.0",
                "checks:",
                "errors:",
                "- none",
            ],
            new[]
            {
                normalizedLines[0],
                normalizedLines[1],
                normalizedLines[6],
                normalizedLines[^2],
                normalizedLines[^1],
            });

        // The whole payload must be free of raw '\r' once normalized, and every line-content
        // comparison must be independent of which newline convention the current OS uses.
        Assert.DoesNotContain('\r', string.Join('\n', normalizedLines));
    }

    [Theory]
    [InlineData("relative-subdir/missing-bundle.json")]
    [InlineData("relative-subdir\\missing-bundle.json")]
    public void MissingFile_UsingEitherPathSeparatorConvention_EmitsStableTokenAndNoLeak(string relativeInput)
    {
        // A path that doesn't exist, mixing forward and back slashes, and including a fake
        // "username" segment designed to tempt a leak if the CLI ever echoed input paths.
        var fakeUsernameSegment = "Users_fake-leak-probe_9f3c";
        var probePath = $"{fakeUsernameSegment}/{relativeInput}";

        var result = InvokeCli(probePath);

        Assert.Equal(3, result.ExitCode);
        Assert.Contains("status: missing-file", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- input-file-missing", result.StandardOutput, StringComparison.Ordinal);
        AssertNoLeak(result.StandardOutput, probePath, fakeUsernameSegment);
    }

    [Fact]
    public void DirectoryInput_UsingWindowsStyleAbsolutePath_EmitsStableTokenAndNoLeak()
    {
        using var directory = TempDirectory.Create();

        // Feed the same directory path through both a native representation and a forced
        // forward-slash representation to prove the emitted token doesn't depend on separator style.
        var nativeResult = InvokeCli(directory.Path);
        var forwardSlashResult = InvokeCli(directory.Path.Replace('\\', '/'));

        Assert.Equal(4, nativeResult.ExitCode);
        Assert.Contains("- input-path-is-directory", nativeResult.StandardOutput, StringComparison.Ordinal);
        AssertNoLeak(nativeResult.StandardOutput, directory.Path, Environment.UserName);

        // Exit code and stable token must match regardless of separator convention used to reach
        // the same underlying directory (both '/' and '\' resolve identically on Windows; on POSIX
        // systems the forward-slash form is simply the native form).
        Assert.Equal(nativeResult.ExitCode, forwardSlashResult.ExitCode);
        Assert.Contains("- input-path-is-directory", forwardSlashResult.StandardOutput, StringComparison.Ordinal);
        AssertNoLeak(forwardSlashResult.StandardOutput, directory.Path, Environment.UserName);
    }

    [Fact]
    public void InvalidJsonInput_DoesNotLeakStackTraceOrExceptionText()
    {
        var fixturePath = CreateTempJsonFile("{ not-json");

        var result = InvokeCli(fixturePath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("status: invalid-json", result.StandardOutput, StringComparison.Ordinal);
        AssertNoLeak(result.StandardOutput, fixturePath, Environment.UserName);
    }

    [Fact]
    public void ReceiptJson_ForMissingFile_DoesNotLeakHostOrPathData()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"leak-probe-{Guid.NewGuid():N}.json");

        var result = InvokeCli("--receipt-json", missingPath);

        Assert.NotEqual(0, result.ExitCode);
        AssertNoLeak(result.StandardOutput, missingPath, Environment.UserName);
    }

    [Fact]
    public void CheckAndErrorOrdering_IsStableAcrossRepeatedRuns_ForValidBundle()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var first = InvokeCli(bundlePath);
        var second = InvokeCli(bundlePath);

        var firstChecks = ExtractSection(first.StandardOutput, "checks:", "errors:");
        var secondChecks = ExtractSection(second.StandardOutput, "checks:", "errors:");

        Assert.Equal(firstChecks, secondChecks);
        Assert.NotEmpty(firstChecks);
    }

    [Fact]
    public void CheckAndErrorOrdering_IsStableAcrossRepeatedRuns_ForFailingBundle()
    {
        var fixturePath = CreateTempJsonFile(
            ReadFixtureJson().Replace(
                "Observational history is limited to recorded events.",
                "Bundle persisted to C:\\\\exports\\\\protocol-operations-report.json.",
                StringComparison.Ordinal));

        var first = InvokeCli(fixturePath);
        var second = InvokeCli(fixturePath);

        var firstErrors = ExtractSection(first.StandardOutput, "errors:", terminator: null);
        var secondErrors = ExtractSection(second.StandardOutput, "errors:", terminator: null);

        Assert.Equal(firstErrors, secondErrors);
        Assert.NotEmpty(firstErrors);
    }

    [Fact]
    public void ResultJson_ForBundleVerification_IsByteIdentical_AcrossRepeatedRuns()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var first = InvokeCli("--result-json", bundlePath);
        var second = InvokeCli("--result-json", bundlePath);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(first.StandardOutput, second.StandardOutput);
        AssertNoRawNewlines(first.StandardOutput);
    }

    [Fact]
    public void ReceiptJson_IsByteIdentical_AcrossRepeatedRuns()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var first = InvokeCli("--receipt-json", bundlePath);
        var second = InvokeCli("--receipt-json", bundlePath);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(first.StandardOutput, second.StandardOutput);
        AssertNoRawNewlines(first.StandardOutput);
    }

    [Fact]
    public void ResultJson_InMemoryUtf8Encoding_HasNoByteOrderMark()
    {
        // The CLI writes via TextWriter.Write (no explicit encoding declared in production code), so
        // the only guarantee we can assert without changing production behavior is over the in-memory
        // string: encoding it as UTF-8 ourselves must not introduce a BOM, and the string itself must
        // not start with the BOM character. This does NOT assert that Console/stdout in a real process
        // is BOM-free -- that depends on the TextWriter the host supplies, which this test harness does
        // not exercise (it uses StringWriter, not a Console stream).
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());

        var result = InvokeCli("--result-json", bundlePath);

        Assert.False(result.StandardOutput.StartsWith('﻿'));

        var utf8Bytes = Encoding.UTF8.GetBytes(result.StandardOutput);
        var bomBytes = new byte[] { 0xEF, 0xBB, 0xBF };
        Assert.False(
            utf8Bytes.Length >= bomBytes.Length &&
            utf8Bytes[0] == bomBytes[0] && utf8Bytes[1] == bomBytes[1] && utf8Bytes[2] == bomBytes[2]);
    }

    [Fact]
    public void ReceiptJson_RoundTripsThroughUtf8MemoryStream_WithoutBom()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var receiptJson = InvokeCli("--receipt-json", bundlePath).StandardOutput;

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            writer.Write(receiptJson);
        }

        var bytes = memoryStream.ToArray();
        Assert.True(bytes.Length >= 3);
        Assert.False(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);

        // Round trip must reproduce the exact same JSON text; this exercises the property this test
        // class actually controls (an explicit no-BOM UTF8Encoding), not a guarantee the CLI itself makes.
        var roundTripped = Encoding.UTF8.GetString(bytes);
        Assert.Equal(receiptJson, roundTripped);
    }

    [Fact]
    public void RelativePath_ToExistingFile_VerifiesSuccessfullyRegardlessOfSeparatorStyle()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var directory = Path.GetDirectoryName(bundlePath)!;
        var fileName = Path.GetFileName(bundlePath);

        var originalCwd = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = directory;

            var viaRelativeNative = InvokeCli(fileName);
            var viaRelativeForwardSlash = InvokeCli("./" + fileName.Replace('\\', '/'));

            Assert.Equal(0, viaRelativeNative.ExitCode);
            Assert.Equal(0, viaRelativeForwardSlash.ExitCode);
            Assert.Equal(viaRelativeNative.StandardOutput, viaRelativeForwardSlash.StandardOutput);
        }
        finally
        {
            Environment.CurrentDirectory = originalCwd;
        }
    }

    [Fact]
    public void InvalidInputPath_EmptyAndWhitespace_ReturnsDeterministicUsageError()
    {
        var resultEmptyArg = InvokeCli(" ");
        var resultNoPath = InvokeCli("--receipt-json");

        Assert.Equal(2, resultEmptyArg.ExitCode);
        Assert.Contains("input-path-required", resultEmptyArg.StandardOutput, StringComparison.Ordinal);

        Assert.Equal(2, resultNoPath.ExitCode);
        Assert.Contains("input-path-required", resultNoPath.StandardOutput, StringComparison.Ordinal);
    }

    private static void AssertNoRawNewlines(string payload)
    {
        Assert.DoesNotContain('\n', payload);
        Assert.DoesNotContain('\r', payload);

        // Confirm it is actually parseable single-line JSON, not merely absent of the literal chars.
        using var _ = JsonDocument.Parse(payload);
    }

    private static void AssertNoLeak(string output, string probeInput, string fakeOrRealUsername)
    {
        Assert.DoesNotContain(probeInput, output, StringComparison.Ordinal);
        Assert.DoesNotContain(fakeOrRealUsername, output, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, output, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, output, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetTempPath().TrimEnd('\\', '/'), output, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.CurrentDirectory, output, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", output, StringComparison.Ordinal);
        Assert.Matches(new System.Text.RegularExpressions.Regex(@"^(?!.*[A-Za-z]:\\).*$", System.Text.RegularExpressions.RegexOptions.Singleline), output);

        foreach (var marker in StackFrameMarkers)
        {
            Assert.DoesNotContain(marker, output, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<string> NormalizeLines(string payload)
    {
        return payload
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractSection(string output, string startMarker, string? terminator)
    {
        var lines = NormalizeLines(output);
        var startIndex = Array.IndexOf(lines.ToArray(), startMarker);
        Assert.True(startIndex >= 0, $"Expected to find marker '{startMarker}' in output.");

        var section = new List<string>();
        for (var i = startIndex + 1; i < lines.Count; i++)
        {
            if (terminator is not null && string.Equals(lines[i], terminator, StringComparison.Ordinal))
            {
                break;
            }

            if (!lines[i].StartsWith("- ", StringComparison.Ordinal))
            {
                break;
            }

            section.Add(lines[i]);
        }

        return section;
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string CreateTempJsonFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static string ReadFixtureJson()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "ProtocolOperationsExportBundle",
            FixtureFileName);

        return File.ReadAllText(fixturePath);
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path) => Path = path;

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"leak-probe-dir-{Guid.NewGuid():N}");
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
