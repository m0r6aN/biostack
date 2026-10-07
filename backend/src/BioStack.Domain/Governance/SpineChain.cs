namespace BioStack.Domain.Governance;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Outcome of verifying the Governed Spine hash chain.
/// </summary>
/// <param name="IsIntact">True when every entry links to its predecessor and rehashes correctly.</param>
/// <param name="EntriesVerified">How many entries were walked.</param>
/// <param name="FirstBrokenReceiptUri">Receipt URI of the earliest entry that failed, if any.</param>
/// <param name="Reason">Human-readable explanation of the first failure, if any.</param>
public sealed record SpineChainVerificationResult(
    bool IsIntact,
    long EntriesVerified,
    string? FirstBrokenReceiptUri,
    string? Reason)
{
    public static SpineChainVerificationResult Intact(long count)
        => new(true, count, null, null);

    public static SpineChainVerificationResult Broken(long count, string? receiptUri, string reason)
        => new(false, count, receiptUri, reason);
}

/// <summary>
/// Hash-chain primitives for the Governed Spine (F3).
///
/// Each entry commits to its predecessor's hash, so the ledger is tamper-EVIDENT rather than
/// merely duplicate-resistant: editing a field, changing a timestamp, or deleting a row breaks
/// the linkage for every entry that follows, and verification reports the earliest break.
///
/// This detects tampering; it does not prevent it. A holder with write access to the database
/// can still rewrite the whole chain consistently. Preventing that requires anchoring the chain
/// head somewhere the holder does not control — signed checkpoints (F3+) with a key held outside
/// the database file, and ideally server-side export of those checkpoints.
/// </summary>
public static class SpineChain
{
    /// <summary>Sentinel predecessor for the first entry. A real hash never collides with it.</summary>
    public const string GenesisPreviousHash = "sha256:genesis";

    /// <summary>Sequence number of the first entry in the chain.</summary>
    public const long GenesisSequenceNumber = 0;

    public const string CheckpointSignatureAlgorithmHmacSha256 = "HMAC-SHA256";
    public const string CheckpointSourceLocalHmac = "local-hmac";
    public const string CheckpointSourceServerHmac = "server-hmac";
    public const string CheckpointSourceUnsignedLocal = "unsigned-local";

    /// <summary>
    /// SHA-256 over the entry's governed fields plus its predecessor hash.
    ///
    /// Fields are length-prefixed before concatenation, so a value containing the delimiter
    /// cannot be crafted to imitate a different field layout ("a|b" + "c" and "a" + "b|c" hash
    /// differently). Timestamps use round-trip "O" format for culture- and precision-stability.
    /// </summary>
    public static string ComputeEntryHash(SpineEntry entry, string previousEntryHash)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var builder = new StringBuilder();
        Append(builder, entry.Id.ToString("N"));
        Append(builder, entry.ReceiptUri);
        Append(builder, entry.SubjectUri);
        Append(builder, entry.TenantId);
        Append(builder, entry.ActorId);
        Append(builder, Stamp(entry.TimestampUtc));
        Append(builder, entry.Decision);
        Append(builder, entry.ReceiptClass);
        Append(builder, entry.PolicyHashValue);
        Append(builder, entry.PolicyHashVersion);
        Append(builder, entry.InputHash);
        Append(builder, entry.EvidenceRefsJson);
        Append(builder, entry.EffectStatus);
        Append(builder, Stamp(entry.CreatedAt));
        Append(builder, entry.SequenceNumber.ToString(CultureInfo.InvariantCulture));
        Append(builder, previousEntryHash);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }

    /// <summary>
    /// Render a timestamp so it hashes identically before and after a database round-trip.
    ///
    /// Two things bite here, and both were caught by verification failing on an UNTAMPERED chain:
    ///   • <see cref="DateTimeKind"/> does not survive SQLite. A value written as Utc reads back
    ///     as Unspecified, and round-trip "O" format renders those differently ("...Z" vs no "Z"),
    ///     so every entry rehashed to a different digest than it was written with.
    ///   • Precision differs by provider. .NET ticks are 100ns; PostgreSQL timestamps are
    ///     microsecond-resolution, so sub-microsecond ticks are truncated on the way back.
    ///
    /// Normalising to UTC at microsecond precision is stable across both providers.
    /// </summary>
    private static string Stamp(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;

        return DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            .ToString("yyyy-MM-ddTHH:mm:ss.ffffff", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Canonical payload for a chain-head checkpoint. Same length-prefix discipline as entry hashes.
    /// </summary>
    public static string BuildCheckpointPayload(
        long sequenceNumber,
        string headEntryHash,
        DateTime checkpointedAtUtc)
    {
        var builder = new StringBuilder();
        Append(builder, sequenceNumber.ToString(CultureInfo.InvariantCulture));
        Append(builder, headEntryHash);
        Append(builder, Stamp(checkpointedAtUtc));
        return builder.ToString();
    }

    /// <summary>
    /// HMAC-SHA256 over the checkpoint payload. Key must not be stored in the Spine database.
    /// </summary>
    public static string SignCheckpointPayload(string payload, byte[] signingKey)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(signingKey);
        if (signingKey.Length == 0)
            throw new ArgumentException("Signing key must not be empty.", nameof(signingKey));

        var bytes = HMACSHA256.HashData(signingKey, Encoding.UTF8.GetBytes(payload));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }

    public static bool VerifyCheckpointSignature(
        long sequenceNumber,
        string headEntryHash,
        DateTime checkpointedAtUtc,
        string signature,
        byte[] signingKey)
    {
        if (string.IsNullOrWhiteSpace(signature) || signingKey.Length == 0)
            return false;

        var payload = BuildCheckpointPayload(sequenceNumber, headEntryHash, checkpointedAtUtc);
        var expected = SignCheckpointPayload(payload, signingKey);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(signature);
        if (expectedBytes.Length != actualBytes.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static void Append(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        builder.Append(value.Length).Append(':').Append(value).Append('|');
    }
}

/// <summary>Outcome of verifying the latest chain checkpoint against the live ledger head.</summary>
public sealed record SpineCheckpointVerificationResult(
    bool ChainIntact,
    bool CheckpointPresent,
    bool HeadMatchesCheckpoint,
    bool SignatureValid,
    bool ExternallyAnchored,
    long ChainEntriesVerified,
    long? CheckpointSequenceNumber,
    string? CheckpointHeadEntryHash,
    string? Reason)
{
    public bool IsFullyValid =>
        ChainIntact
        && CheckpointPresent
        && HeadMatchesCheckpoint
        && (!ExternallyAnchored || SignatureValid);
}

