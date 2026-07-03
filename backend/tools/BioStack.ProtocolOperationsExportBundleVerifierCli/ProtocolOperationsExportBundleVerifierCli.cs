namespace BioStack.ProtocolOperationsExportBundleVerifierCli;

using System.Text.Json;
using BioStack.Application.Services;
using BioStack.Contracts.Responses;

public static class ProtocolOperationsExportBundleVerifierCli
{
    private const string ReceiptJsonFlag = "--receipt-json";
    private const string VerifyReceiptJsonFlag = "--verify-receipt-json";
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
        AllowTrailingCommas = false,
    };

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (!TryParseArguments(args, out var options, out var argumentErrors))
        {
            return options.Mode is CliMode.VerifyReceiptJson
                ? WriteReceiptVerificationInvalid(stdout, InvalidInputStatus, argumentErrors)
                : WriteBundleFailure(
                    stdout,
                    options.Mode is CliMode.EmitReceiptJson,
                    status: InvalidInputStatus,
                    bundle: null,
                    result: null,
                    errors: argumentErrors,
                    exitCode: 2);
        }

        return options.Mode switch
        {
            CliMode.VerifyReceiptJson => RunReceiptVerification(options.InputPath, stdout),
            CliMode.EmitReceiptJson or CliMode.VerifyBundle => RunBundleVerification(options, stdout),
            _ => WriteBundleFailure(stdout, emitReceiptJson: false, InvalidInputStatus, null, null, ["unknown-mode"], 2),
        };
    }

    private static int RunBundleVerification(CliOptions options, TextWriter stdout)
    {
        if (!File.Exists(options.InputPath))
        {
            return WriteBundleFailure(
                stdout,
                options.Mode is CliMode.EmitReceiptJson,
                status: MissingFileStatus,
                bundle: null,
                result: null,
                errors: ["input-file-missing"],
                exitCode: 3);
        }

        string json;
        try
        {
            json = File.ReadAllText(options.InputPath);
        }
        catch (Exception) when (IsInputFailure())
        {
            return WriteBundleFailure(
                stdout,
                options.Mode is CliMode.EmitReceiptJson,
                status: InvalidInputStatus,
                bundle: null,
                result: null,
                errors: ["input-read-failed"],
                exitCode: 4);
        }

        ProtocolOperationsExportBundle? bundle;
        try
        {
            bundle = JsonSerializer.Deserialize<ProtocolOperationsExportBundle>(json, BundleSerializerOptions);
        }
        catch (JsonException)
        {
            return WriteBundleFailure(
                stdout,
                options.Mode is CliMode.EmitReceiptJson,
                status: InvalidJsonStatus,
                bundle: null,
                result: null,
                errors: ["input-json-invalid"],
                exitCode: 5);
        }
        catch (Exception) when (IsDeserializationFailure())
        {
            return WriteBundleFailure(
                stdout,
                options.Mode is CliMode.EmitReceiptJson,
                status: InvalidInputStatus,
                bundle: null,
                result: null,
                errors: ["input-deserialization-failed"],
                exitCode: 6);
        }

        if (bundle is null)
        {
            return WriteBundleFailure(
                stdout,
                options.Mode is CliMode.EmitReceiptJson,
                status: InvalidInputStatus,
                bundle: null,
                result: null,
                errors: ["input-bundle-missing"],
                exitCode: 6);
        }

        var result = new ProtocolOperationsExportBundleVerifier().Verify(bundle);
        var status = result.IsValid ? VerifiedStatus : VerificationFailedStatus;
        var exitCode = result.IsValid ? 0 : 1;

        try
        {
            if (options.Mode is CliMode.EmitReceiptJson)
            {
                ProtocolOperationsExportBundleVerificationReceiptJson.Write(stdout, status, bundle, result, errorsOverride: null);
            }
            else
            {
                WriteBundleSummary(
                    stdout,
                    status,
                    result.SchemaVersion,
                    result.ExpectedBundleSha256,
                    result.ActualBundleSha256,
                    result.ExpectedReportExportSha256,
                    result.ActualReportExportSha256,
                    result.Checks,
                    result.Errors);
            }
        }
        catch (Exception)
        {
            return WriteBundleFailure(
                stdout,
                options.Mode is CliMode.EmitReceiptJson,
                status: InvalidInputStatus,
                bundle: null,
                result: null,
                errors: ["receipt-generation-failed"],
                exitCode: 7);
        }

        return exitCode;
    }

    private static int RunReceiptVerification(string inputPath, TextWriter stdout)
    {
        if (!File.Exists(inputPath))
        {
            return WriteReceiptVerificationInvalid(stdout, MissingFileStatus, ["input-file-missing"]);
        }

        string json;
        try
        {
            json = File.ReadAllText(inputPath);
        }
        catch (Exception) when (IsInputFailure())
        {
            return WriteReceiptVerificationInvalid(stdout, InvalidInputStatus, ["input-read-failed"]);
        }

        var verification = ProtocolOperationsExportBundleVerificationReceiptJsonVerifier.Verify(json);
        if (!verification.IsValid)
        {
            return WriteReceiptVerificationInvalid(stdout, verification.Status, verification.Errors);
        }

        stdout.WriteLine("Protocol Operations Export Bundle Verification Receipt: VALID");
        stdout.WriteLine($"Status: {verification.Status}");
        stdout.WriteLine($"ReceiptContentHash: {verification.ReceiptContentHash}");
        return 0;
    }

    private static bool TryParseArguments(string[] args, out CliOptions options, out string[] errors)
    {
        var mode = CliMode.VerifyBundle;
        string? inputPath = null;
        var parseErrors = new List<string>();

        foreach (var arg in args)
        {
            if (string.Equals(arg, ReceiptJsonFlag, StringComparison.Ordinal))
            {
                if (mode is not CliMode.VerifyBundle)
                {
                    parseErrors.Add("mode-conflict");
                    continue;
                }

                mode = CliMode.EmitReceiptJson;
                continue;
            }

            if (string.Equals(arg, VerifyReceiptJsonFlag, StringComparison.Ordinal))
            {
                if (mode is not CliMode.VerifyBundle)
                {
                    parseErrors.Add("mode-conflict");
                    continue;
                }

                mode = CliMode.VerifyReceiptJson;
                continue;
            }

            if (string.IsNullOrWhiteSpace(arg))
            {
                parseErrors.Add("input-path-required");
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                parseErrors.Add("unknown-argument");
                continue;
            }

            if (inputPath is not null)
            {
                parseErrors.Add("input-path-duplicated");
                continue;
            }

            inputPath = arg;
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            parseErrors.Add("input-path-required");
        }

        options = new CliOptions(inputPath ?? string.Empty, mode);
        errors = parseErrors.ToArray();
        return errors.Length == 0;
    }

    private static bool IsInputFailure() => true;

    private static bool IsDeserializationFailure() => true;

    private static int WriteBundleFailure(
        TextWriter stdout,
        bool emitReceiptJson,
        string status,
        ProtocolOperationsExportBundle? bundle,
        ProtocolOperationsExportBundleVerificationResult? result,
        IReadOnlyList<string> errors,
        int exitCode)
    {
        if (emitReceiptJson)
        {
            ProtocolOperationsExportBundleVerificationReceiptJson.Write(stdout, status, bundle, result, errors);
        }
        else
        {
            WriteBundleSummary(
                stdout,
                status,
                schemaVersion: null,
                expectedBundleSha256: null,
                actualBundleSha256: null,
                expectedReportExportSha256: null,
                actualReportExportSha256: null,
                checks: [],
                errors);
        }

        return exitCode;
    }

    private static int WriteReceiptVerificationInvalid(TextWriter stdout, string status, IReadOnlyList<string> errors)
    {
        stdout.WriteLine("Protocol Operations Export Bundle Verification Receipt: INVALID");
        stdout.WriteLine($"Status: {status}");
        stdout.WriteLine($"Errors: {string.Join(", ", errors)}");
        return 1;
    }

    private static void WriteBundleSummary(
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

    private sealed record CliOptions(string InputPath, CliMode Mode);

    private enum CliMode
    {
        VerifyBundle,
        EmitReceiptJson,
        VerifyReceiptJson,
    }
}
