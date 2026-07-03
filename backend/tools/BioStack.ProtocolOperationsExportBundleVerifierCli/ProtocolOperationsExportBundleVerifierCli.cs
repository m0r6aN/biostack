namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;

public static class ProtocolOperationsExportBundleVerifierCli
{
    private const string VerifiedStatus = "verified";
    private const string VerificationFailedStatus = "verification-failed";
    private const string MissingFileStatus = "missing-file";
    private const string InvalidJsonStatus = "invalid-json";
    private const string InvalidInputStatus = "invalid-input";
    private const string Unavailable = "(unavailable)";
    private const string None = "none";

    private static readonly JsonSerializerOptions BundleSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            WriteSummary(
                stdout,
                status: InvalidInputStatus,
                schemaVersion: null,
                expectedBundleSha256: null,
                actualBundleSha256: null,
                expectedReportExportSha256: null,
                actualReportExportSha256: null,
                checks: [],
                errors: ["input-path-required"]);

            return 2;
        }

        var path = args[0];
        if (!File.Exists(path))
        {
            WriteSummary(
                stdout,
                status: MissingFileStatus,
                schemaVersion: null,
                expectedBundleSha256: null,
                actualBundleSha256: null,
                expectedReportExportSha256: null,
                actualReportExportSha256: null,
                checks: [],
                errors: ["input-file-missing"]);

            return 3;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception) when (IsInputFailure())
        {
            WriteSummary(
                stdout,
                status: InvalidInputStatus,
                schemaVersion: null,
                expectedBundleSha256: null,
                actualBundleSha256: null,
                expectedReportExportSha256: null,
                actualReportExportSha256: null,
                checks: [],
                errors: ["input-read-failed"]);

            return 4;
        }

        ProtocolOperationsExportBundle? bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<ProtocolOperationsExportBundle>(json, BundleSerializerOptions);
        }
        catch (JsonException)
        {
            WriteSummary(
                stdout,
                status: InvalidJsonStatus,
                schemaVersion: null,
                expectedBundleSha256: null,
                actualBundleSha256: null,
                expectedReportExportSha256: null,
                actualReportExportSha256: null,
                checks: [],
                errors: ["input-json-invalid"]);

            return 5;
        }

        if (bundle is null)
        {
            WriteSummary(
                stdout,
                status: InvalidInputStatus,
                schemaVersion: null,
                expectedBundleSha256: null,
                actualBundleSha256: null,
                expectedReportExportSha256: null,
                actualReportExportSha256: null,
                checks: [],
                errors: ["input-bundle-missing"]);

            return 6;
        }

        var result = new ProtocolOperationsExportBundleVerifier().Verify(bundle);

        WriteSummary(
            stdout,
            status: result.IsValid ? VerifiedStatus : VerificationFailedStatus,
            schemaVersion: result.SchemaVersion,
            expectedBundleSha256: result.ExpectedBundleSha256,
            actualBundleSha256: result.ActualBundleSha256,
            expectedReportExportSha256: result.ExpectedReportExportSha256,
            actualReportExportSha256: result.ActualReportExportSha256,
            checks: result.Checks,
            errors: result.Errors);

        return result.IsValid ? 0 : 1;
    }

    private static bool IsInputFailure() =>
        true;

    private static void WriteSummary(
        TextWriter stdout,
        string status,
        string? schemaVersion,
        string? expectedBundleSha256,
        string? actualBundleSha256,
        string? expectedReportExportSha256,
        string? actualReportExportSha256,
        IReadOnlyList<string> checks,
        IReadOnlyList<string> errors)
    {
        stdout.WriteLine($"status: {status}");
        stdout.WriteLine($"schema-version: {FormatValue(schemaVersion)}");
        stdout.WriteLine($"expected-bundle-sha256: {FormatValue(expectedBundleSha256)}");
        stdout.WriteLine($"actual-bundle-sha256: {FormatValue(actualBundleSha256)}");
        stdout.WriteLine($"expected-report-export-sha256: {FormatValue(expectedReportExportSha256)}");
        stdout.WriteLine($"actual-report-export-sha256: {FormatValue(actualReportExportSha256)}");
        stdout.WriteLine("checks:");

        if (checks.Count == 0)
        {
            stdout.WriteLine($"- {None}");
        }
        else
        {
            foreach (var check in checks)
            {
                stdout.WriteLine($"- {check}");
            }
        }

        stdout.WriteLine("errors:");
        if (errors.Count == 0)
        {
            stdout.WriteLine($"- {None}");
            return;
        }

        foreach (var error in errors)
        {
            stdout.WriteLine($"- {error}");
        }
    }

    private static string FormatValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Unavailable : value;
}
