namespace BioStack.Application.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Contracts.Responses;

public sealed class ProtocolOperationsExportBundleService : IProtocolOperationsExportBundleService
{
    public const string SchemaVersion = "1.0.0";
    public const string HashAlgorithmName = "SHA-256";
    public const string Disclaimer = "This non-medical export bundle is an observational record package for review only. It does not provide clinical guidance or care planning.";

    private static readonly JsonSerializerOptions CanonicalSerializerOptions = new()
    {
        WriteIndented = false,
    };

    private readonly IProtocolOperationsReportExportService _exportService;
    private readonly Func<DateTime> _utcNow;

    public ProtocolOperationsExportBundleService(
        IProtocolOperationsReportExportService exportService,
        Func<DateTime>? utcNow = null)
    {
        _exportService = exportService;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    public async Task<ProtocolOperationsExportBundle> GetBundleAsync(Guid profileId, CancellationToken ct = default)
    {
        var export = await _exportService.GetExportAsync(profileId, ct);
        var metadata = new ProtocolOperationsExportBundleMetadata(
            SchemaVersion,
            _utcNow(),
            export.Metadata.ProfileId,
            export.Metadata.ProtocolId);
        var artifacts = new List<ProtocolOperationsExportBundleArtifact>
        {
            new(
                "protocol-operations-report-export-json",
                "application/json",
                "report-export",
                export.Metadata.SchemaVersion,
                export.Integrity.ContentHash),
        };
        var bundleContentHash = ComputeBundleContentHash(metadata, export, artifacts, Disclaimer);
        var integrity = new ProtocolOperationsExportBundleIntegrity(
            HashAlgorithmName,
            bundleContentHash,
            export.Integrity.ContentHash);

        return new ProtocolOperationsExportBundle(metadata, export, artifacts, integrity, Disclaimer);
    }

    public static string ComputeBundleContentHash(
        ProtocolOperationsExportBundleMetadata metadata,
        ProtocolOperationsReportExport reportExport,
        IReadOnlyList<ProtocolOperationsExportBundleArtifact> artifacts,
        string disclaimer)
    {
        var canonicalContent = new ProtocolOperationsExportBundleCanonicalContent(
            metadata,
            reportExport,
            artifacts,
            disclaimer,
            reportExport.Integrity.ContentHash);
        var canonical = JsonSerializer.Serialize(canonicalContent, CanonicalSerializerOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record ProtocolOperationsExportBundleCanonicalContent(
        ProtocolOperationsExportBundleMetadata Metadata,
        ProtocolOperationsReportExport ReportExport,
        IReadOnlyList<ProtocolOperationsExportBundleArtifact> Artifacts,
        string Disclaimer,
        string ReportExportContentHash);
}

public interface IProtocolOperationsExportBundleService
{
    Task<ProtocolOperationsExportBundle> GetBundleAsync(Guid profileId, CancellationToken ct = default);
}
