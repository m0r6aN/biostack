namespace BioStack.Infrastructure.Governance;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// F3+ local truncation/rollback anchor (R1 remediation, default-on posture + hash comparison
/// added by H2).</summary>
/// <remarks>
///
/// The hash chain (<see cref="BioStack.Domain.Governance.SpineChain"/>) is tamper-EVIDENT for
/// in-place edits and for gaps in the middle of the chain: <c>VerifyChainAsync</c> rehashes every
/// surviving row and checks sequence continuity. But a holder who deletes the TAIL of the
/// chain — the most recent rows, via a raw <c>DELETE</c> against the SQLite file — leaves no gap
/// for that walk to find. The surviving rows still link correctly to each other and to genesis;
/// the chain simply looks like it never advanced past the truncation point. Without an anchor
/// held somewhere other than the row set itself, "the chain never advanced past entry 7" and
/// "the chain advanced past entry 7 and someone deleted everything written after it" are
/// indistinguishable from the database's contents alone — this was finding R1
/// (BIO-LOCAL-004 retro-review 2): deleting trailing rows rolled the chain back to a
/// stale-but-genuine checkpoint and verification reported <c>IsFullyValid = true</c>.
///
/// This file is that anchor: a plain-text <c>"{sequenceNumber}|{entryHash}"</c> record of the
/// highest chain head this process has observed, written to a path outside the SQLite database.
///
/// What this IS proven to do, locally, in this repository's test suite:
///   • Catch exactly the threat model <c>SpineChainIntegrityTests</c> and
///     <c>SpineCheckpointTests</c> already exercise — tampering via a raw SQL statement against
///     the <c>SpineEntries</c> table ("the way someone with a SQLite browser would") — because
///     that statement does not touch this file. Deleting trailing rows now leaves the watermark
///     ahead of the recomputed chain head, which <c>SpineRepository.VerifyChainAsync</c> reports
///     as a broken chain instead of silently accepting the rollback.
///
/// What this IS NOT proven to do, and must not be read as proving:
///   • It is NOT external/off-box anchoring. A holder with filesystem write access to the SQLite
///     file has, by construction, the same access to this file. They can delete it, edit it, or
///     restore both files together from an earlier backup, and the rollback will not be detected.
///     This raises the bar from "one SQL statement" to "also find and edit/delete a second file
///     consistently with the database" — it does not raise it to cryptographic impossibility.
///   • It is NOT a substitute for a signed, externally-held-key checkpoint
///     (<see cref="BioStack.Domain.Governance.SpineChain.CheckpointSourceServerHmac"/>). A
///     checkpoint signed with a key the holder never possesses is the only mechanism in this
///     module that survives the holder controlling every local file.
///   • It does NOT persist across moving/cloning the database to a new machine without also
///     moving this file (or, for the default derived path, moving it alongside the database —
///     see <see cref="ResolveDefaultPath"/>).
///   • (H2/Finding B residual) It compares both sequence number and entry hash at the watermark's
///     recorded sequence, which closes the exact "delete tail, append one forged replacement at
///     the same sequence" probe. It does NOT detect a holder who deletes the tail and then
///     replays <see cref="SpineHeadWatermarkStore.Advance"/> themselves with the forged entry's
///     real hash (i.e. forges a consistent watermark write, not just a consistent row) — that
///     requires the same filesystem access as editing the SQLite file, is the same "holder
///     controls every local file" precondition already disclosed above, and remains bounded by
///     the same external, signed checkpoint caveat (<see
///     cref="BioStack.Domain.Governance.SpineChain.CheckpointSourceServerHmac"/>) as the rest of
///     this local anchor.
/// </remarks>
public static class SpineHeadWatermarkStore
{
    /// <summary>
    /// H2 (AC1, Finding A): default watermark location when an operator has not set
    /// <see cref="SpineCheckpointOptions.WatermarkFilePath"/> explicitly, so truncation detection
    /// is ON out of the box instead of requiring configuration first.
    ///
    /// For a file-backed SQLite database, the watermark is placed next to the database file
    /// itself (<c>{dbFile}.spine-watermark</c>) — the same directory a consistent backup of the
    /// database would already have to include, and the one piece of durable, writable storage
    /// this process is already known to have. For an in-memory or otherwise pathless connection
    /// (as used by this module's own test suite), there is no database file to sit beside, so the
    /// watermark is placed in the temp directory, keyed by a hash of the full connection string —
    /// stable for the lifetime of that connection string, and naturally isolated per distinct
    /// in-memory database (e.g. each test's unique <c>Data Source</c>) without any coordination.
    /// </summary>
    public static string ResolveDefaultPath(string? dataSource, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(dataSource) && !dataSource.Contains("mode=memory"))
            return dataSource + ".spine-watermark";

        var key = string.IsNullOrEmpty(connectionString) ? dataSource ?? string.Empty : connectionString;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..32].ToLowerInvariant();
        return Path.Combine(Path.GetTempPath(), "biostack-spine-watermarks", $"{hash}.watermark");
    }

    /// <summary>The highest (sequence number, entry hash) pair this process has observed.</summary>
    public readonly record struct Watermark(long SequenceNumber, string EntryHash);

    /// <summary>
    /// Read the current watermark, or <c>null</c> when disabled, missing, or unreadable.
    /// Unreadable/malformed content is treated as "no watermark yet" rather than thrown — a
    /// corrupted anchor file must not itself become a denial-of-service against a healthy chain.
    /// </summary>
    public static Watermark? TryRead(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var text = File.ReadAllText(path);
            var separatorIndex = text.IndexOf('|');
            if (separatorIndex <= 0)
                return null;

            var sequencePart = text[..separatorIndex];
            var hashPart = text[(separatorIndex + 1)..].TrimEnd();
            if (!long.TryParse(sequencePart, out var sequenceNumber) || hashPart.Length == 0)
                return null;

            return new Watermark(sequenceNumber, hashPart);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Record a new observed head, but only if it advances the watermark. The watermark never
    /// moves backwards through this API: an attacker who can write this file at all could roll it
    /// back together with the database, so "only ever advance" at least keeps a benign operation
    /// (e.g. restoring an older full-disk backup that happens to include this file) from quietly
    /// lowering the bar without the operator noticing. No-op when disabled.
    /// </summary>
    public static void Advance(string? path, long sequenceNumber, string entryHash)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var current = TryRead(path);
        if (current is { } existing && existing.SequenceNumber >= sequenceNumber)
            return;

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, $"{sequenceNumber}|{entryHash}");
        }
        catch (IOException)
        {
            // Best-effort local anchor: a write failure here must not fail the append it is
            // observing. A missed Advance only narrows this mitigation's window; it does not
            // reintroduce a false "fully valid" result for tampering that already happened.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
