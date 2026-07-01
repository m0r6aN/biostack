namespace BioStack.Contracts.Responses;

// ─── Protocol Operations Report export contract ─────────────────────────────
// Deterministic, export-ready wrapper around the observational Protocol
// Operations Report: schema version, generation metadata, the embedded
// report, and a content-integrity hash. Strictly non-medical — no
// recommendations, dosing instructions, diagnosis, or treatment advice.
// No PDF/file generation; this is a data contract only. Independent of
// Protocol Intelligence (offline/build-time artifact evaluation); see
// docs/architecture/protocol-intelligence-offline-boundary.md.

public sealed record ProtocolOperationsReportExportMetadata(
    string SchemaVersion,
    DateTime GeneratedAtUtc,
    Guid ProfileId,
    Guid? ProtocolId);

public sealed record ProtocolOperationsReportExportIntegrity(
    string HashAlgorithm,
    string ContentHash);

public sealed record ProtocolOperationsReportExport(
    ProtocolOperationsReportExportMetadata Metadata,
    ProtocolOperationsReport Report,
    ProtocolOperationsReportExportIntegrity Integrity,
    string Disclaimer);
