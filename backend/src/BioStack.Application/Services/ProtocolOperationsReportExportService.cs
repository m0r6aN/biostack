namespace BioStack.Application.Services;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioStack.Contracts.Responses;

/// <summary>
/// Packages the observational Protocol Operations Report into a deterministic,
/// export-ready artifact: schema version, generation metadata, the embedded
/// report, and a SHA-256 content-integrity hash. Does not write files, does
/// not generate PDF, and does not call external services. Purely a data
/// contract wrapper — no recommendations, dosing guidance, diagnosis, or
/// treatment advice, and independent of the offline-only Protocol
/// Intelligence evaluation.
/// </summary>
public sealed class ProtocolOperationsReportExportService : IProtocolOperationsReportExportService
{
    public const string SchemaVersion = "1.0.0";
    public const string HashAlgorithmName = "SHA-256";

    public const string Disclaimer =
        "This export is a factual, observational summary of logged protocol " +
        "activity counts and events. It is not medical guidance, does not direct " +
        "care decisions, and is not a substitute for professional consultation.";

    private static readonly JsonSerializerOptions CanonicalReportSerializerOptions = new()
    {
        WriteIndented = false,
    };

    private readonly IProtocolOperationsReportService _reportService;

    public ProtocolOperationsReportExportService(IProtocolOperationsReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<ProtocolOperationsReportExport> GetExportAsync(Guid profileId, CancellationToken ct = default)
    {
        var report = await _reportService.GetReportAsync(profileId, ct);

        var metadata = new ProtocolOperationsReportExportMetadata(
            SchemaVersion,
            DateTime.UtcNow,
            report.ProfileId,
            report.ProtocolId);

        var integrity = new ProtocolOperationsReportExportIntegrity(
            HashAlgorithmName,
            ComputeContentHash(report));

        return new ProtocolOperationsReportExport(metadata, report, integrity, Disclaimer);
    }

    public static string ComputeContentHash(ProtocolOperationsReport report)
    {
        var canonical = JsonSerializer.Serialize(report, CanonicalReportSerializerOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public interface IProtocolOperationsReportExportService
{
    Task<ProtocolOperationsReportExport> GetExportAsync(Guid profileId, CancellationToken ct = default);
}
