namespace BioStack.Infrastructure.Governance;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// F3+ local truncation/rollback anchor (R1 remediation, default-on posture + hash comparison
/// added by H2; provider-safe derivation + discoverability hardening added by H2-R2).</summary>
/// <remarks>
///
/// The hash chain (<see cref="BioStack.Domain.Governance.SpineChain"/>) is tamper-EVIDENT for
/// in-place edits and for gaps in the middle of the chain: <c>VerifyChainAsync</c> rehashes every
/// surviving row and checks sequence continuity. But a holder who deletes the TAIL of the
/// chain — the most recent rows, via a raw <c>DELETE</c> against the database — leaves no gap
/// for that walk to find. The surviving rows still link correctly to each other and to genesis;
/// the chain simply looks like it never advanced past the truncation point. Without an anchor
/// held somewhere other than the row set itself, "the chain never advanced past entry 7" and
/// "the chain advanced past entry 7 and someone deleted everything written after it" are
/// indistinguishable from the database's contents alone — this was finding R1
/// (BIO-LOCAL-004 retro-review 2): deleting trailing rows rolled the chain back to a
/// stale-but-genuine checkpoint and verification reported <c>IsFullyValid = true</c>.
///
/// This file is that anchor: a plain-text <c>"{sequenceNumber}|{entryHash}"</c> record of the
/// highest chain head this process has observed, written to a path outside the governed database.
///
/// What this IS proven to do, locally, in this repository's test suite, for EVERY provider this
/// module derives a default path for (SQLite file-backed, SQLite in-memory, and Npgsql/Postgres —
/// see <see cref="ResolveDefaultPath"/> for the provider-specific derivation each case gets):
///   • Catch exactly the threat model <c>SpineChainIntegrityTests</c> and
///     <c>SpineCheckpointTests</c> already exercise — tampering via a raw SQL statement against
///     the <c>SpineEntries</c> table ("the way someone with a database browser would") — because
///     that statement does not touch this file. Deleting trailing rows now leaves the watermark
///     ahead of the recomputed chain head, which <c>SpineRepository.VerifyChainAsync</c> reports
///     as a broken chain instead of silently accepting the rollback.
///   • Derive a distinct path per database/catalog identity, not just per host:port — two
///     databases on the same Postgres server do not share, collide on, or clobber each other's
///     watermark (H2-R2/Finding C).
///   • Always derive a rooted (absolute) path — never one that resolves relative to the
///     process's current working directory and silently evaporates on a container redeploy
///     (H2-R2/Finding C).
///
/// What this IS NOT proven to do, and must not be read as proving:
///   • It is NOT external/off-box anchoring. A holder with filesystem write access to the
///     database file (or, for Postgres, to the watermark's configured/derived local path) has,
///     by construction, the access needed to also delete, edit, or restore this file consistently
///     with the database. This raises the bar from "one SQL statement" to "also find and
///     edit/delete a second file consistently with the database" — it does not raise it to
///     cryptographic impossibility, and for the default SQLite co-located path, "find" costs an
///     extra directory listing, not meaningful reconnaissance (H2-R2/Finding D — see
///     <see cref="ResolveDefaultPath"/> for the H2-R2 change that moved the default SQLite
///     watermark into a less self-describing sibling location; this is attack-surface narrowing,
///     not a cryptographic fix).
///   • It is NOT mutual binding: nothing durable outside this file records that a watermark
///     SHOULD exist. <c>VerifyChainAsync</c> cannot distinguish "this watermark file was deleted
///     after being established" from "this database already had entries before the watermark
///     feature was ever enabled here (e.g. an upgrade of a pre-H2 deployment, or a fresh restore
///     of just the database half of a consistent backup pair)" — both look identical from this
///     file's absence alone, and failing closed on the second case would turn every such restore
///     or upgrade into a false positive. Distinguishing them requires an independent, durable
///     marker outside this file — in practice, a column on a Spine database table recording "a
///     watermark has been established" or a digest of it — which is a database schema change,
///     out of scope for this module's file-only remediation (H2-R2/Finding D — STOP-AND-REPORT;
///     see the H2-R2 PR description for the residual and a recommended follow-up parcel). Until
///     that lands, deleting this file (distinct from editing its contents, which <c>Advance</c>'s
///     monotonic-only-forward write and the hash comparison below still partially defend) is
///     equivalent to the database never having been watermarked, and the next verification simply
///     has no anchor to compare against.
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
///     requires the same filesystem access as editing the database file, is the same "holder
///     controls every local file" precondition already disclosed above, and remains bounded by
///     the same external, signed checkpoint caveat (<see
///     cref="BioStack.Domain.Governance.SpineChain.CheckpointSourceServerHmac"/>) as the rest of
///     this local anchor.
/// </remarks>
public static class SpineHeadWatermarkStore
{
    /// <summary>
    /// H2 (AC1, Finding A) / H2-R2 (Finding C, provider safety; Finding D, discoverability):
    /// default watermark location when an operator has not set
    /// <see cref="SpineCheckpointOptions.WatermarkFilePath"/> explicitly, so truncation detection
    /// is ON out of the box instead of requiring configuration first.
    ///
    /// This function's behaviour is genuinely provider-specific; there is no single rule that is
    /// accurate for every ADO.NET provider's <c>DbConnection.DataSource</c>, so the three real
    /// cases this module has test coverage for are:
    ///
    ///   1. File-backed SQLite: <c>DataSource</c> is a rooted filesystem path pointing at the
    ///      actual <c>.db</c> file. This is the ONLY case where co-locating a sibling file next
    ///      to <c>DataSource</c> is safe — the path is both unique per database (it IS the
    ///      database file) and durable (same directory a consistent backup would already need to
    ///      include). H2-R2/Finding D: the sibling file is placed in a hidden
    ///      <c>.biostack-governance</c> subdirectory next to the database, named by a hash of the
    ///      database path rather than <c>{dbFile}.spine-watermark</c> — narrowing, not
    ///      eliminating, the "visible in the same directory listing with a self-describing name"
    ///      discoverability the original H2 naming had.
    ///   2. In-memory/pathless SQLite (this module's own test suite) and EVERY other provider
    ///      whose <c>DataSource</c> is not a rooted filesystem path — most importantly Npgsql,
    ///      whose <c>DataSource</c> is a non-path, non-unique-per-database endpoint descriptor
    ///      (<c>tcp://host:port</c> — identical for every database on that server, per
    ///      H2-R2/Finding C): there is no safe filesystem anchor to co-locate with, so the
    ///      watermark path is derived by hashing the FULL connection string (which, for every
    ///      provider that matters here, includes the database/catalog name — e.g. Npgsql's
    ///      <c>Database=</c> — so distinct databases on a shared host:port still derive distinct
    ///      paths) and placed under a configured, always-rooted base directory (<see
    ///      cref="SpineCheckpointOptions.WatermarkBaseDirectory"/>, defaulting to the OS temp
    ///      directory, itself always absolute — never the process's current working directory).
    ///
    /// A rooted <c>DataSource</c> is required, not merely a non-empty one, before case 1 applies:
    /// this is the exact fix for H2-R2/Finding C, where Npgsql's <c>tcp://host:port</c>
    /// previously qualified for case 1's naive string-concatenation and produced a CWD-relative,
    /// per-host:port-only (not per-database) path.
    /// </summary>
    public static string ResolveDefaultPath(
        string? dataSource, string connectionString, string? baseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(dataSource)
            && !dataSource.Contains("mode=memory", StringComparison.OrdinalIgnoreCase)
            && Path.IsPathRooted(dataSource))
        {
            var fullPath = Path.GetFullPath(dataSource);
            var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var hideDirectory = Path.Combine(directory, ".biostack-governance");
            return Path.Combine(hideDirectory, $"{ShortHash(fullPath)}.watermark");
        }

        var key = string.IsNullOrEmpty(connectionString) ? dataSource ?? string.Empty : connectionString;

        var root = string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.Combine(Path.GetTempPath(), "biostack-spine-watermarks")
            : baseDirectory;

        if (!Path.IsPathRooted(root))
        {
            throw new ArgumentException(
                $"{nameof(SpineCheckpointOptions.WatermarkBaseDirectory)} must be an absolute path "
                + "(a CWD-relative watermark base silently reopens the deploy-cycle gap this "
                + "derivation exists to close).",
                nameof(baseDirectory));
        }

        return Path.Combine(root, $"{ShortHash(key)}.watermark");
    }

    private static string ShortHash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..32].ToLowerInvariant();

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
