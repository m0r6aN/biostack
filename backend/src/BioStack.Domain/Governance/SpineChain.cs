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
/// head somewhere the holder does not control — see the findings doc.
/// </summary>
public static class SpineChain
{
    /// <summary>Sentinel predecessor for the first entry. A real hash never collides with it.</summary>
    public const string GenesisPreviousHash = "sha256:genesis";

    /// <summary>Sequence number of the first entry in the chain.</summary>
    public const long GenesisSequenceNumber = 0;

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
        Append(builder, entry.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
        Append(builder, entry.Decision);
        Append(builder, entry.ReceiptClass);
        Append(builder, entry.PolicyHashValue);
        Append(builder, entry.PolicyHashVersion);
        Append(builder, entry.InputHash);
        Append(builder, entry.EvidenceRefsJson);
        Append(builder, entry.EffectStatus);
        Append(builder, entry.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        Append(builder, entry.SequenceNumber.ToString(CultureInfo.InvariantCulture));
        Append(builder, previousEntryHash);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }

    private static void Append(StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        builder.Append(value.Length).Append(':').Append(value).Append('|');
    }
}
