namespace BioStack.Contracts.Responses;

// Protocol Operations export bundle contract. This is an in-memory,
// future-downloadable envelope around the observational report export.
public sealed record ProtocolOperationsExportBundleMetadata(
    string SchemaVersion,
    DateTime GeneratedAtUtc,
    Guid ProfileId,
    Guid? ProtocolId);

public sealed record ProtocolOperationsExportBundleIntegrity(
    string HashAlgorithm,
    string BundleContentHash,
    string ReportExportContentHash);

public sealed record ProtocolOperationsExportBundleArtifact(
    string ArtifactId,
    string MediaType,
    string Role,
    string SchemaVersion,
    string ContentHash);

public sealed record ProtocolOperationsExportBundle(
    ProtocolOperationsExportBundleMetadata Metadata,
    ProtocolOperationsReportExport ReportExport,
    IReadOnlyList<ProtocolOperationsExportBundleArtifact> Artifacts,
    ProtocolOperationsExportBundleIntegrity Integrity,
    string Disclaimer);
