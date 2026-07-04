namespace BioStack.ProtocolOperationsExportBundleVerifierCli.Tests;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.ProtocolOperationsExportBundleVerifierCli;
using Xunit;

/// <summary>
/// Freezes the machine-readable <c>--result-json</c> output shape independently from the
/// human-readable CLI summary. Human text may evolve; the automation contract must not drift
/// silently. The snapshot covers property ordering, hash field names, artifact type, status, schema
/// id/version fields, and check/error ordering for every result-json variant, and asserts no
/// forbidden field (machine path, absolute path, username, timestamp, database/PDF/runtime/medical
/// authority claim) is present.
/// </summary>
public sealed class ProtocolOperationsExportBundleVerifierCliResultJsonSchemaSnapshotTests
{
    private const string FixtureFileName = "ProtocolOperationsExportBundle.golden.json";
    private const string ExpectedBundleSha256 = "744054e7ead52b5473aec8e88e22ff5ffc37658a30061fbbd800c3b339c14c19";

    private static readonly JsonElement Snapshot = LoadSnapshot();

    [Fact]
    public void ValidBundleResultJson_MatchesSchemaSnapshot()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var result = InvokeCli("--result-json", bundlePath);
        using var payload = JsonDocument.Parse(result.StandardOutput);
        var contract = Snapshot.GetProperty("bundleResult");

        Assert.Equal(0, result.ExitCode);
        AssertBundleResultSchema(payload.RootElement, contract, expectedStatus: contract.GetProperty("validStatus").GetString()!);
        AssertNoForbiddenFields(payload.RootElement, result.StandardOutput, bundlePath);
    }

    [Fact]
    public void InvalidBundleResultJson_MatchesSchemaSnapshot()
    {
        var bundlePath = CreateTempJsonFile(
            ReadFixtureJson().Replace(ExpectedBundleSha256, new string('b', 64), StringComparison.Ordinal));
        var result = InvokeCli("--result-json", bundlePath);
        using var payload = JsonDocument.Parse(result.StandardOutput);
        var contract = Snapshot.GetProperty("bundleResult");

        Assert.Equal(1, result.ExitCode);
        AssertBundleResultSchema(payload.RootElement, contract, expectedStatus: contract.GetProperty("invalidStatus").GetString()!);
        Assert.Contains("bundle-sha256-mismatch", ReadStringArray(payload.RootElement.GetProperty("errors")));
        AssertNoForbiddenFields(payload.RootElement, result.StandardOutput, bundlePath);
    }

    [Fact]
    public void ValidReceiptResultJson_MatchesSchemaSnapshot()
    {
        var receiptPath = CreateTempJsonFile(EmitReceiptJson());
        var result = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);
        using var payload = JsonDocument.Parse(result.StandardOutput);
        var contract = Snapshot.GetProperty("receiptResult");

        Assert.Equal(0, result.ExitCode);
        AssertReceiptResultSchema(payload.RootElement, contract, expectedStatus: contract.GetProperty("validStatus").GetString()!);
        Assert.Empty(ReadStringArray(payload.RootElement.GetProperty("errors")));
        AssertNoForbiddenFields(payload.RootElement, result.StandardOutput, receiptPath);
    }

    [Fact]
    public void InvalidReceiptResultJson_MatchesSchemaSnapshot()
    {
        var receiptPath = CreateTempJsonFile("{ not-json");
        var result = InvokeCli("--result-json", "--verify-receipt-json", receiptPath);
        using var payload = JsonDocument.Parse(result.StandardOutput);
        var contract = Snapshot.GetProperty("receiptResult");

        Assert.Equal(1, result.ExitCode);
        AssertReceiptResultSchema(payload.RootElement, contract, expectedStatus: contract.GetProperty("invalidStatus").GetString()!);
        Assert.Contains("input-json-invalid", ReadStringArray(payload.RootElement.GetProperty("errors")));
        AssertNoForbiddenFields(payload.RootElement, result.StandardOutput, receiptPath);
    }

    private static void AssertBundleResultSchema(JsonElement root, JsonElement contract, string expectedStatus)
    {
        Assert.Equal(ReadStringArray(contract.GetProperty("orderedProperties")), ReadPropertyNames(root));
        Assert.Equal(expectedStatus, root.GetProperty("status").GetString());
        Assert.Equal(contract.GetProperty("artifactTypeChecked").GetString(), root.GetProperty("artifactTypeChecked").GetString());
        Assert.Equal(contract.GetProperty("schemaIdChecked").GetString(), root.GetProperty("schemaIdChecked").GetString());
        Assert.Equal("1.0.0", root.GetProperty("schemaVersionChecked").GetString());
        Assert.Equal(ReadStringArray(contract.GetProperty("checksOrder")), ReadStringArray(root.GetProperty("checks")));

        // Hash field names are frozen (names, not values).
        foreach (var hashField in ReadStringArray(contract.GetProperty("hashFieldNames")))
        {
            Assert.True(root.TryGetProperty(hashField, out _), $"Missing hash field '{hashField}'.");
        }
    }

    private static void AssertReceiptResultSchema(JsonElement root, JsonElement contract, string expectedStatus)
    {
        Assert.Equal(ReadStringArray(contract.GetProperty("orderedProperties")), ReadPropertyNames(root));
        Assert.Equal(expectedStatus, root.GetProperty("status").GetString());
        Assert.Equal(contract.GetProperty("artifactTypeChecked").GetString(), root.GetProperty("artifactTypeChecked").GetString());

        // Schema id/verifier id fields are frozen field names; values are stable only on success.
        var boundaries = root.GetProperty("boundaries");
        if (boundaries.ValueKind == JsonValueKind.Object)
        {
            Assert.Equal(ReadStringArray(contract.GetProperty("boundariesProperties")), ReadPropertyNames(boundaries));
            Assert.Equal(contract.GetProperty("receiptSchemaIdChecked").GetString(), root.GetProperty("receiptSchemaIdChecked").GetString());
            Assert.Equal(contract.GetProperty("verifierSchemaIdChecked").GetString(), root.GetProperty("verifierSchemaIdChecked").GetString());
            Assert.Equal(contract.GetProperty("bundleSchemaIdChecked").GetString(), root.GetProperty("bundleSchemaIdChecked").GetString());
        }
        else
        {
            // Invalid input path still emits the full frozen shape with null values.
            Assert.Equal(JsonValueKind.Null, boundaries.ValueKind);
        }

        foreach (var hashField in ReadStringArray(contract.GetProperty("hashFieldNames")))
        {
            Assert.True(root.TryGetProperty(hashField, out _), $"Missing hash field '{hashField}'.");
        }
    }

    private static void AssertNoForbiddenFields(JsonElement root, string rawOutput, string suppliedPath)
    {
        // Forbidden property names (claims) — checked recursively. Boundary negation flags such as
        // noPdf / nonMedical / noRuntimeExpansion are intentionally allowed.
        var forbiddenNameSubstrings = ReadStringArray(Snapshot.GetProperty("forbiddenPropertyNameSubstrings"));
        foreach (var name in CollectPropertyNames(root))
        {
            foreach (var forbidden in forbiddenNameSubstrings)
            {
                Assert.False(
                    name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"Result JSON exposes forbidden property name '{name}' (matched '{forbidden}').");
            }
        }

        // Forbidden runtime values — machine-specific paths, identity, and timestamps.
        Assert.DoesNotContain(suppliedPath, rawOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetDirectoryName(suppliedPath)!, rawOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.CurrentDirectory, rawOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.MachineName, rawOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, rawOutput, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"[A-Za-z]:\\"), rawOutput);
        Assert.DoesNotMatch(new Regex(@"\bfile://"), rawOutput);
        Assert.DoesNotMatch(new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}"), rawOutput);
    }

    private static IEnumerable<string> CollectPropertyNames(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;
                    foreach (var nested in CollectPropertyNames(property.Value))
                    {
                        yield return nested;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in CollectPropertyNames(item))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }

    private static JsonElement LoadSnapshot()
    {
        var path = Path.Combine(RepositoryRoot(), "backend", "tests", "Fixtures", "ProtocolOperationsExportBundle", "ProtocolOperationsResultJsonSchemaSnapshot.golden.json");
        var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.Clone();
    }

    private static string EmitReceiptJson()
    {
        var bundlePath = CreateTempJsonFile(ReadFixtureJson());
        var result = InvokeCli("--receipt-json", bundlePath);
        Assert.Equal(0, result.ExitCode);
        return result.StandardOutput;
    }

    private static (int ExitCode, string StandardOutput, string StandardError) InvokeCli(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var exitCode = ProtocolOperationsExportBundleVerifierCli.Run(args, stdout, stderr);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string[] ReadPropertyNames(JsonElement element) =>
        element.EnumerateObject().Select(property => property.Name).ToArray();

    private static string[] ReadStringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();

    private static string CreateTempJsonFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static string ReadFixtureJson()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ProtocolOperationsExportBundle", FixtureFileName);
        return File.ReadAllText(fixturePath);
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
