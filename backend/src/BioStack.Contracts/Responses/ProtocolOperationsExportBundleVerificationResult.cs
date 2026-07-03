namespace BioStack.Contracts.Responses;

public sealed record ProtocolOperationsExportBundleVerificationResult(
    bool IsValid,
    string SchemaVersion,
    string ExpectedBundleSha256,
    string ActualBundleSha256,
    string ExpectedReportExportSha256,
    string ActualReportExportSha256,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Errors);
