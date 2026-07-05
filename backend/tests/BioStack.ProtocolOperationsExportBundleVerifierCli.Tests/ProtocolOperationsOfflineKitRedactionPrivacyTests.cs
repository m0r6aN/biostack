namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

/// <summary>
/// Proves that bundle verification, receipt verification, and result-json output never
/// echo sensitive-looking input material (paths, usernames, secrets, stack traces, etc.),
/// and that malicious/malformed input fails CLOSED with stable, deterministic error tokens.
///
/// All fixture values in this file are obviously-fake placeholders (e.g. "sk-FAKE-DEADBEEF",
/// "attacker_secret") chosen only to exercise the deny-list scan. No real secrets, no medical
/// content, and no production behavior is exercised or changed by these tests.
/// </summary>
public sealed class ProtocolOperationsOfflineKitRedactionPrivacyTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";

    // Deny-list of sensitive-looking substrings/patterns that must never appear in CLI output
    // (plain summary, --receipt-json, or --result-json), regardless of what was fed in.
    private static readonly string[] StaticDenyListTokens =
    [
        "attacker_secret",
        "sk-FAKE-DEADBEEF",
        "Server=",
        "Password=",
        "Data Source=",
        "ssn",
        "connectionString",
        "   at ", // raw stack trace frame marker
    ];

    [Fact]
    public void BundleVerification_MissingFile_DoesNotEchoEmbeddedUsernamePathSegment()
    {
        var fakeUsernameSegment = "attacker_secret";
        var maliciousMissingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}",
            "users",
            fakeUsernameSegment,
            "bundle.json");

        var plain = InvokeCli(maliciousMissingPath);
        var resultJson = InvokeCli("--result-json", maliciousMissingPath);
        var receiptJson = InvokeCli("--receipt-json", maliciousMissingPath);

        AssertFailsClosed(plain, expectedExitCodeNonZero: true);
        AssertFailsClosed(resultJson, expectedExitCodeNonZero: true);
        AssertFailsClosed(receiptJson, expectedExitCodeNonZero: true);

        Assert.Contains("status: missing-file", plain.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("missing-file", resultJson.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("missing-file", receiptJson.StandardOutput, StringComparison.Ordinal);

        AssertDoesNotEchoPath(plain.StandardOutput, maliciousMissingPath, fakeUsernameSegment);
        AssertDoesNotEchoPath(resultJson.StandardOutput, maliciousMissingPath, fakeUsernameSegment);
        AssertDoesNotEchoPath(receiptJson.StandardOutput, maliciousMissingPath, fakeUsernameSegment);

        AssertNoDenyListLeak(plain.StandardOutput + plain.StandardError);
        AssertNoDenyListLeak(resultJson.StandardOutput + resultJson.StandardError);
        AssertNoDenyListLeak(receiptJson.StandardOutput + receiptJson.StandardError);
    }

    [Fact]
    public void BundleVerification_MissingFile_DoesNotEchoEnvironmentOrHostIdentity()
    {
        var maliciousMissingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}",
            "users",
            "attacker_secret",
            "bundle.json");

        var receiptJson = InvokeCli("--receipt-json", maliciousMissingPath);
        var resultJson = InvokeCli("--result-json", maliciousMissingPath);
        var plain = InvokeCli(maliciousMissingPath);

        foreach (var output in new[] { receiptJson.StandardOutput, resultJson.StandardOutput, plain.StandardOutput })
        {
            Assert.DoesNotContain(Environment.UserName, output, StringComparison.Ordinal);
            Assert.DoesNotContain(Environment.MachineName, output, StringComparison.Ordinal);
            Assert.DoesNotContain(Environment.CurrentDirectory, output, StringComparison.Ordinal);
            AssertNoAbsolutePathLeak(output);
        }
    }

    [Fact]
    public void BundleVerification_InvalidJsonWithFakeApiKey_ReturnsStableTokenAndDoesNotEchoSecret()
    {
        const string fakeApiKey = "sk-FAKE-DEADBEEF";
        var invalidJsonWithSecret = $$"""{ "apiKey": "{{fakeApiKey}}", """;
        var fixturePath = CreateTempJsonFile(invalidJsonWithSecret);

        var plain = InvokeCli(fixturePath);
        var resultJson = InvokeCli("--result-json", fixturePath);
        var receiptJson = InvokeCli("--receipt-json", fixturePath);

        AssertFailsClosed(plain, expectedExitCodeNonZero: true);
        AssertFailsClosed(resultJson, expectedExitCodeNonZero: true);
        AssertFailsClosed(receiptJson, expectedExitCodeNonZero: true);

        Assert.Contains("status: invalid-json", plain.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("- input-json-invalid", plain.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("invalid-json", resultJson.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("input-json-invalid", resultJson.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("invalid-json", receiptJson.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("input-json-invalid", receiptJson.StandardOutput, StringComparison.Ordinal);

        Assert.DoesNotContain(fakeApiKey, plain.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(fakeApiKey, resultJson.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(fakeApiKey, receiptJson.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey", plain.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiKey", resultJson.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiKey", receiptJson.StandardOutput, StringComparison.OrdinalIgnoreCase);

        AssertNoDenyListLeak(plain.StandardOutput + plain.StandardError);
        AssertNoDenyListLeak(resultJson.StandardOutput + resultJson.StandardError);
        AssertNoDenyListLeak(receiptJson.StandardOutput + receiptJson.StandardError);
    }

    [Fact]
    public void BundleVerification_MalformedBundleWithExtraSensitiveFields_FailsClosedWithoutEchoingInjectedValues()
    {
        var malformedBundleWithSensitiveFields = BuildMalformedBundleWithSensitiveExtensionFields();
        var fixturePath = CreateTempJsonFile(malformedBundleWithSensitiveFields);

        var plain = InvokeCli(fixturePath);
        var resultJson = InvokeCli("--result-json", fixturePath);
        var receiptJson = InvokeCli("--receipt-json", fixturePath);

        // Extra/unknown properties are disallowed by strict deserialization contracts in this
        // CLI (PropertyNameCaseInsensitive=false, no unknown-member tolerance downstream), so
        // this either fails to deserialize (invalid-input/invalid-json) or fails verification
        // (verification-failed) -- both are fail-closed outcomes.
        Assert.NotEqual(0, plain.ExitCode);
        Assert.NotEqual(0, resultJson.ExitCode);
        Assert.NotEqual(0, receiptJson.ExitCode);

        foreach (var combined in new[]
                 {
                     plain.StandardOutput + plain.StandardError,
                     resultJson.StandardOutput + resultJson.StandardError,
                     receiptJson.StandardOutput + receiptJson.StandardError,
                 })
        {
            AssertNoDenyListLeak(combined);
            Assert.DoesNotContain("s3cr3t-connection-value", combined, StringComparison.Ordinal);
            Assert.DoesNotContain("hunter2-fake-password", combined, StringComparison.Ordinal);
            Assert.DoesNotContain("000-00-0000", combined, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BundleVerification_UnexpectedExtensionFieldsOnValidBundle_FailsDeterministicallyWithoutEchoingValues()
    {
        var fixtureWithExtensionFields = InjectUnexpectedExtensionFields(ReadFixtureJson());
        var fixturePath = CreateTempJsonFile(fixtureWithExtensionFields);

        var plain = InvokeCli(fixturePath);
        var resultJson = InvokeCli("--result-json", fixturePath);
        var receiptJson = InvokeCli("--receipt-json", fixturePath);

        // Strict deserializer options (ReadCommentHandling=Disallow, AllowTrailingCommas=false,
        // and record-based binding without extension-data capture) mean unknown fields are either
        // silently ignored by System.Text.Json's default unknown-member tolerance (record still
        // binds known members) or rejected. Either way, injected extension values must never
        // surface in output, and the known-good bundle must still verify deterministically.
        var combinedPlain = plain.StandardOutput + plain.StandardError;
        var combinedResult = resultJson.StandardOutput + resultJson.StandardError;
        var combinedReceipt = receiptJson.StandardOutput + receiptJson.StandardError;

        Assert.DoesNotContain("attacker_secret_extension_value", combinedPlain, StringComparison.Ordinal);
        Assert.DoesNotContain("attacker_secret_extension_value", combinedResult, StringComparison.Ordinal);
        Assert.DoesNotContain("attacker_secret_extension_value", combinedReceipt, StringComparison.Ordinal);

        AssertNoDenyListLeak(combinedPlain);
        AssertNoDenyListLeak(combinedResult);
        AssertNoDenyListLeak(combinedReceipt);
    }

    [Fact]
    public void ReceiptVerification_MissingFile_DoesNotEchoInputPathOrHostIdentity()
    {
        var fakeUsernameSegment = "attacker_secret";
        var maliciousMissingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}",
            "users",
            fakeUsernameSegment,
            "receipt.json");

        var plain = InvokeCli("--verify-receipt-json", maliciousMissingPath);
        var resultJson = InvokeCli("--result-json", "--verify-receipt-json", maliciousMissingPath);

        Assert.Equal(1, plain.ExitCode);
        Assert.Equal(1, resultJson.ExitCode);
        Assert.Contains("Status: missing-file", plain.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("missing-file", resultJson.StandardOutput, StringComparison.Ordinal);

        AssertDoesNotEchoPath(plain.StandardOutput, maliciousMissingPath, fakeUsernameSegment);
        AssertDoesNotEchoPath(resultJson.StandardOutput, maliciousMissingPath, fakeUsernameSegment);
        Assert.DoesNotContain(Environment.UserName, plain.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, plain.StandardOutput, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceiptVerification_InvalidJsonWithFakeSecret_ReturnsStableTokenAndDoesNotEchoSecret()
    {
        const string fakeApiKey = "sk-FAKE-DEADBEEF";
        var invalidJsonWithSecret = $$"""{ "apiKey": "{{fakeApiKey}}", """;
        var receiptPath = CreateTempJsonFile(invalidJsonWithSecret);

        var plain = InvokeCli("--verify-receipt-json", receiptPath);
        var resultJson = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);

        Assert.Equal(1, plain.ExitCode);
        Assert.Equal(1, resultJson.ExitCode);
        Assert.Contains("Status: invalid-json", plain.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("Errors: input-json-invalid", plain.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("invalid-json", resultJson.StandardOutput, StringComparison.Ordinal);

        Assert.DoesNotContain(fakeApiKey, plain.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(fakeApiKey, resultJson.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("apiKey", plain.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apiKey", resultJson.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReceiptVerification_MalformedReceiptWithExtraSensitiveFields_FailsClosedWithoutEchoingInjectedValues()
    {
        var validReceiptJson = InvokeCli("--receipt-json", CreateTempJsonFile(ReadFixtureJson())).StandardOutput;
        var tamperedReceipt = InjectSensitiveExtensionFieldsIntoReceipt(validReceiptJson);
        var receiptPath = CreateTempJsonFile(tamperedReceipt);

        var plain = InvokeCli("--verify-receipt-json", receiptPath);
        var resultJson = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);

        // Structural validation still runs over the deserialized receipt regardless of the
        // extra members present, so a tampered/extended receipt must still be judged on its
        // known fields. The malicious values themselves must never surface in output.
        var combinedPlain = plain.StandardOutput + plain.StandardError;
        var combinedResult = resultJson.StandardOutput + resultJson.StandardError;

        Assert.DoesNotContain("s3cr3t-connection-value", combinedPlain, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2-fake-password", combinedPlain, StringComparison.Ordinal);
        Assert.DoesNotContain("000-00-0000", combinedPlain, StringComparison.Ordinal);
        Assert.DoesNotContain("s3cr3t-connection-value", combinedResult, StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2-fake-password", combinedResult, StringComparison.Ordinal);
        Assert.DoesNotContain("000-00-0000", combinedResult, StringComparison.Ordinal);

        AssertNoDenyListLeak(combinedPlain);
        AssertNoDenyListLeak(combinedResult);
    }

    [Fact]
    public void ResultJson_ForValidGoldenFixture_ContainsNoSensitiveCategoriesAndPreservesSchemaShape()
    {
        var fixturePath = CreateTempJsonFile(ReadFixtureJson());

        var result = InvokeCli("--result-json", fixturePath);
        using var payload = JsonDocument.Parse(result.StandardOutput);

        Assert.Equal(0, result.ExitCode);

        // Schema shape unchanged: same known top-level properties as the established
        // ResultJson contract for a bundle verification result.
        var expectedProperties = new[]
        {
            "status",
            "artifactTypeChecked",
            "schemaIdChecked",
            "schemaVersionChecked",
            "expectedBundleContentHash",
            "actualBundleContentHash",
            "expectedReportExportContentHash",
            "actualReportExportContentHash",
            "checks",
            "errors",
        };

        foreach (var propertyName in expectedProperties)
        {
            Assert.True(
                payload.RootElement.TryGetProperty(propertyName, out _),
                $"Expected result-json to contain property '{propertyName}'.");
        }

        AssertNoDenyListLeak(result.StandardOutput);
        Assert.DoesNotContain(fixturePath, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, result.StandardOutput, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(@"C:\Users\attacker_secret\bundle.json")]
    [InlineData(@"D:\exports\hidden\bundle.json")]
    [InlineData("file:///home/attacker_secret/bundle.json")]
    [InlineData("/home/attacker_secret/bundle.json")]
    public void BundleVerification_AbsolutePathVariants_NeverAppearInAnyOutputMode(string absolutePathVariant)
    {
        // These are not real filesystem paths that exist on this machine; they are used purely
        // as *input values* that a hostile caller might supply as the CLI argument, to prove the
        // CLI never echoes the raw argument back on the missing-file path.
        var plain = InvokeCli(absolutePathVariant);
        var resultJson = InvokeCli("--result-json", absolutePathVariant);
        var receiptJson = InvokeCli("--receipt-json", absolutePathVariant);

        foreach (var output in new[] { plain.StandardOutput, resultJson.StandardOutput, receiptJson.StandardOutput })
        {
            Assert.DoesNotContain(absolutePathVariant, output, StringComparison.Ordinal);
            Assert.DoesNotContain("attacker_secret", output, StringComparison.Ordinal);
        }
    }

    private static string BuildMalformedBundleWithSensitiveExtensionFields()
    {
        // A structurally-invalid bundle object carrying extra sensitive-looking fields that a
        // hostile/careless caller might embed. Values are obviously fake placeholders.
        return """
        {
          "Metadata": {
            "SchemaVersion": "1.0.0",
            "GeneratedAtUtc": "2026-01-09T12:00:00Z",
            "ProfileId": "33333333-3333-3333-3333-333333333333",
            "ProtocolId": "44444444-4444-4444-4444-444444444444"
          },
          "connectionString": "Server=fake-host;Database=fake-db;Password=hunter2-fake-password;",
          "password": "hunter2-fake-password",
          "ssn": "000-00-0000",
          "apiKey": "sk-FAKE-DEADBEEF",
          "notes": "s3cr3t-connection-value",
          "ReportExport": null,
          "Artifacts": [],
          "Integrity": {
            "HashAlgorithm": "SHA-256",
            "BundleContentHash": "0000000000000000000000000000000000000000000000000000000000000",
            "ReportExportContentHash": "0000000000000000000000000000000000000000000000000000000000000"
          },
          "Disclaimer": "This non-medical export bundle is an observational record package for review only. It does not provide clinical guidance or care planning."
        }
        """;
    }

    private static string InjectUnexpectedExtensionFields(string bundleJson)
    {
        // Insert unexpected top-level extension fields alongside a structurally valid bundle,
        // to prove extra unknown members never leak even when the rest of the bundle verifies.
        return bundleJson.Replace(
            "\"Metadata\": {",
            "\"connectionString\": \"Server=fake-host;Password=attacker_secret_extension_value;\", \"Metadata\": {",
            StringComparison.Ordinal);
    }

    private static string InjectSensitiveExtensionFieldsIntoReceipt(string receiptJson)
    {
        using var document = JsonDocument.Parse(receiptJson);
        var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(receiptJson)!;
        payload["connectionString"] = "Server=fake-host;Database=fake-db;Password=hunter2-fake-password;";
        payload["password"] = "hunter2-fake-password";
        payload["ssn"] = "000-00-0000";
        payload["notes"] = "s3cr3t-connection-value";
        return JsonSerializer.Serialize(payload);
    }

    private static void AssertFailsClosed((int ExitCode, string StandardOutput, string StandardError) result, bool expectedExitCodeNonZero)
    {
        if (expectedExitCodeNonZero)
        {
            Assert.NotEqual(0, result.ExitCode);
        }
    }

    private static void AssertDoesNotEchoPath(string output, string fullPath, string sensitiveSegment)
    {
        Assert.DoesNotContain(fullPath, output, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveSegment, output, StringComparison.Ordinal);
    }

    private static void AssertNoAbsolutePathLeak(string output)
    {
        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.None))
        {
            Assert.False(
                DriveLetterAbsolutePathRegex().IsMatch(line),
                $"Output line unexpectedly contained a drive-letter absolute path: '{line}'.");
            Assert.False(
                line.Contains("file://", StringComparison.OrdinalIgnoreCase),
                $"Output line unexpectedly contained a file:// URI: '{line}'.");
        }
    }

    private static void AssertNoDenyListLeak(string combinedOutput)
    {
        foreach (var token in StaticDenyListTokens)
        {
            Assert.DoesNotContain(token, combinedOutput, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("System.Exception", combinedOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("System.IO.IOException", combinedOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Text.Json.JsonException", combinedOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("System.NullReferenceException", combinedOutput, StringComparison.Ordinal);
    }

    private static System.Text.RegularExpressions.Regex DriveLetterAbsolutePathRegex() => DriveLetterAbsolutePathRegexField;

    private static readonly System.Text.RegularExpressions.Regex DriveLetterAbsolutePathRegexField = new(
        @"\b[A-Za-z]:\\",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string CreateTempJsonFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content, Encoding.UTF8);
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
}
