namespace BioStack.Infrastructure.Governance;

using BioStack.Domain.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class SpineImmutabilityViolationException(string receiptUri)
    : Exception($"Spine entry for receipt '{receiptUri}' already exists. Receipts are immutable.");

public sealed class SpineChainContentionException(string message) : Exception(message);

public interface ISpineRepository
{
    Task<SpineEntry> AppendAsync(SpineEntry entry, CancellationToken ct = default);
    Task<SpineEntry?> GetByReceiptUriAsync(string receiptUri, CancellationToken ct = default);
    Task<IReadOnlyList<SpineEntry>> GetBySubjectAsync(string subjectUri, CancellationToken ct = default);
    Task<IReadOnlyList<SpineEntry>> GetByActorAsync(string actorId, CancellationToken ct = default);

    /// <summary>
    /// Walk the chain from genesis and confirm every entry rehashes and links correctly (F3).
    /// Reports the earliest break rather than just a boolean, so an operator can see where the
    /// ledger diverged.
    /// </summary>
    Task<SpineChainVerificationResult> VerifyChainAsync(CancellationToken ct = default);
}

public sealed class SpineRepository(
    BioStackDbContext db,
    IServiceProvider services,
    IOptions<SpineCheckpointOptions> checkpointOptions,
    ILogger<SpineRepository> logger) : ISpineRepository
{
    /// <summary>
    /// Concurrent appends read the same chain head, so the loser of the race violates the unique
    /// index on SequenceNumber. That is correct behaviour — it is what keeps the chain linear —
    /// so retry a bounded number of times before surfacing contention.
    /// </summary>
    private const int MaxAppendAttempts = 5;

    /// <summary>
    /// H2/Finding A: guards the one-time, process-wide "truncation detection is explicitly
    /// disabled" warning so opting out is loud (logged) without spamming every append/verify.
    /// </summary>
    private static int _disabledWatermarkWarningEmitted;

    /// <summary>
    /// Test-only seam: xUnit runs many <see cref="SpineRepository"/> instances in one process, so
    /// the one-shot warning guard above must be resettable between tests that specifically assert
    /// on it (<c>InternalsVisibleTo</c> scopes this to BioStack.Api.Tests; it has no production
    /// caller).
    /// </summary>
    internal static void ResetDisabledWatermarkWarningGuardForTests()
        => Interlocked.Exchange(ref _disabledWatermarkWarningEmitted, 0);

    /// <summary>
    /// H2 (AC1): resolve the effective watermark path for this call. Returns null only when an
    /// operator has explicitly set <see cref="SpineCheckpointOptions.DisableTruncationWatermark"/>
    /// to true — that is the sole opt-out, and it is logged loudly (once per process) rather
    /// than silently taking effect. Otherwise an explicit <see
    /// cref="SpineCheckpointOptions.WatermarkFilePath"/> wins; failing that, a path is derived
    /// automatically from the database connection, using provider-specific rules (H2-R2/Finding
    /// C) that keep the result both unique per database/catalog and rooted/absolute regardless of
    /// which provider's <c>DataSource</c> shape is in play — see <see
    /// cref="SpineHeadWatermarkStore.ResolveDefaultPath"/> — so detection is on by default for
    /// every supported provider, not only SQLite.
    /// </summary>
    private string? ResolveWatermarkPath()
    {
        var opts = checkpointOptions.Value;

        if (opts.DisableTruncationWatermark)
        {
            if (Interlocked.CompareExchange(ref _disabledWatermarkWarningEmitted, 1, 0) == 0)
            {
                logger.LogWarning(
                    "Governed Spine truncation/rollback detection is explicitly DISABLED via "
                    + "SpineCheckpoint:DisableTruncationWatermark=true. This is a reduced-posture "
                    + "opt-out: a deleted tail will once again be silently accepted as an intact "
                    + "chain (the original R1 failure mode). Unset this to restore default "
                    + "protection.");
            }

            return null;
        }

        if (!string.IsNullOrWhiteSpace(opts.WatermarkFilePath))
            return opts.WatermarkFilePath;

        var connection = db.Database.GetDbConnection();
        return SpineHeadWatermarkStore.ResolveDefaultPath(
            connection.DataSource, connection.ConnectionString, opts.WatermarkBaseDirectory);
    }

    public async Task<SpineEntry> AppendAsync(SpineEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        for (var attempt = 1; attempt <= MaxAppendAttempts; attempt++)
        {
            var exists = await db.SpineEntries
                .AnyAsync(e => e.ReceiptUri == entry.ReceiptUri, ct);

            if (exists)
                throw new SpineImmutabilityViolationException(entry.ReceiptUri);

            // Chain head: highest sequence number wins. Null means we are writing genesis.
            var head = await db.SpineEntries
                .AsNoTracking()
                .OrderByDescending(e => e.SequenceNumber)
                .FirstOrDefaultAsync(ct);

            var sequenceNumber = head is null
                ? SpineChain.GenesisSequenceNumber
                : head.SequenceNumber + 1;

            var previousEntryHash = head?.EntryHash ?? SpineChain.GenesisPreviousHash;

            // Rebuild rather than mutate: the caller's entry carries no chain position, and the
            // hash must cover the sequence number we just claimed.
            var linked = new SpineEntry
            {
                Id = entry.Id,
                ReceiptUri = entry.ReceiptUri,
                SubjectUri = entry.SubjectUri,
                TenantId = entry.TenantId,
                ActorId = entry.ActorId,
                TimestampUtc = entry.TimestampUtc,
                Decision = entry.Decision,
                ReceiptClass = entry.ReceiptClass,
                PolicyHashValue = entry.PolicyHashValue,
                PolicyHashVersion = entry.PolicyHashVersion,
                InputHash = entry.InputHash,
                EvidenceRefsJson = entry.EvidenceRefsJson,
                EffectStatus = entry.EffectStatus,
                CreatedAt = entry.CreatedAt,
                SequenceNumber = sequenceNumber,
                PreviousEntryHash = previousEntryHash,
            };

            var withHash = WithEntryHash(linked);

            db.SpineEntries.Add(withHash);

            try
            {
                await db.SaveChangesAsync(ct);

                // R1/H2: advance the local truncation watermark (no-op only when explicitly
                // disabled; otherwise on by default, see ResolveWatermarkPath). Must happen on
                // the write path, not only during verification, so a later delete of this row
                // has something to be caught against.
                SpineHeadWatermarkStore.Advance(
                    ResolveWatermarkPath(),
                    withHash.SequenceNumber,
                    withHash.EntryHash);

                await MaybeAutoCheckpointAsync(withHash.SequenceNumber, ct);
                return withHash;
            }
            catch (DbUpdateException) when (attempt < MaxAppendAttempts)
            {
                // Another append claimed this slot. Detach and re-read the head.
                db.Entry(withHash).State = EntityState.Detached;
            }
        }

        throw new SpineChainContentionException(
            $"Could not append receipt '{entry.ReceiptUri}' to the Governed Spine after "
            + $"{MaxAppendAttempts} attempts due to concurrent writes.");
    }

    /// <summary>
    /// F3+: every N appends, snapshot the chain head with a signed checkpoint when configured.
    /// Resolved lazily so checkpoint service can depend on this repository without a ctor cycle.
    /// </summary>
    private async Task MaybeAutoCheckpointAsync(long sequenceNumber, CancellationToken ct)
    {
        var every = checkpointOptions.Value.AutoCheckpointEveryNEntries;
        if (every <= 0)
            return;

        // Sequence is 0-based; checkpoint after genesis and every N thereafter at N-1, 2N-1, …
        if ((sequenceNumber + 1) % every != 0)
            return;

        try
        {
            var checkpoints = services.GetRequiredService<ISpineCheckpointService>();
            await checkpoints.CreateCheckpointAsync(
                note: $"auto-every-{every}-entries",
                ct);
        }
        catch (Exception ex)
        {
            // Checkpoint failure must not roll back a successful receipt append.
            logger.LogWarning(
                ex,
                "Auto spine checkpoint failed after sequence {Sequence}",
                sequenceNumber);
        }
    }

    public async Task<SpineChainVerificationResult> VerifyChainAsync(CancellationToken ct = default)
    {
        var entries = await db.SpineEntries
            .AsNoTracking()
            .OrderBy(e => e.SequenceNumber)
            .ToListAsync(ct);

        var expectedPrevious = SpineChain.GenesisPreviousHash;
        var expectedSequence = SpineChain.GenesisSequenceNumber;
        long verified = 0;

        foreach (var entry in entries)
        {
            if (entry.SequenceNumber != expectedSequence)
            {
                return SpineChainVerificationResult.Broken(
                    verified, entry.ReceiptUri,
                    $"Sequence gap: expected {expectedSequence}, found {entry.SequenceNumber}. "
                    + "An entry was removed or inserted out of order.");
            }

            if (!string.Equals(entry.PreviousEntryHash, expectedPrevious, StringComparison.Ordinal))
            {
                return SpineChainVerificationResult.Broken(
                    verified, entry.ReceiptUri,
                    "Broken linkage: this entry does not point at its predecessor's hash.");
            }

            var recomputed = SpineChain.ComputeEntryHash(entry, entry.PreviousEntryHash);
            if (!string.Equals(recomputed, entry.EntryHash, StringComparison.Ordinal))
            {
                return SpineChainVerificationResult.Broken(
                    verified, entry.ReceiptUri,
                    "Hash mismatch: this entry's contents were altered after it was written.");
            }

            expectedPrevious = entry.EntryHash;
            expectedSequence++;
            verified++;
        }

        // R1/H2 remediation: a deleted tail leaves no gap among the surviving rows above -- the
        // loop above cannot see it. Compare against the local watermark (on by default; see
        // ResolveWatermarkPath and SpineHeadWatermarkStore for the precise local-vs-external
        // distinction this is and is not proving).
        var observedHeadSequence = expectedSequence - 1; // last verified sequence, -1 if empty
        var watermark = SpineHeadWatermarkStore.TryRead(ResolveWatermarkPath());
        if (watermark is { } mark)
        {
            if (mark.SequenceNumber > observedHeadSequence)
            {
                return SpineChainVerificationResult.Broken(
                    verified,
                    entries.Count > 0 ? entries[^1].ReceiptUri : null,
                    $"Truncation detected: the local watermark previously observed chain sequence "
                    + $"{mark.SequenceNumber}, but the chain now ends at sequence {observedHeadSequence}. "
                    + "Entries were deleted after they were written (rollback/truncation).");
            }

            // H2/Finding B: the sequence number alone is not enough -- a holder who deletes the
            // true tail entry and appends one forged replacement through the legitimate API
            // restores the sequence count without advancing past it. The watermark also records
            // the hash of the entry it last observed at that sequence, so compare that too.
            if (mark.SequenceNumber == observedHeadSequence && entries.Count > 0)
            {
                var head = entries[^1];
                if (!string.Equals(head.EntryHash, mark.EntryHash, StringComparison.Ordinal))
                {
                    return SpineChainVerificationResult.Broken(
                        verified,
                        head.ReceiptUri,
                        $"Tail substitution detected: the chain head at sequence {observedHeadSequence} "
                        + "does not match the local watermark's recorded hash for that sequence. "
                        + "The original entry was deleted and replaced with different content at the "
                        + "same position in the chain.");
                }
            }
        }

        return SpineChainVerificationResult.Intact(verified);
    }

    public Task<SpineEntry?> GetByReceiptUriAsync(string receiptUri, CancellationToken ct = default)
        => db.SpineEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ReceiptUri == receiptUri, ct);

    public async Task<IReadOnlyList<SpineEntry>> GetBySubjectAsync(string subjectUri, CancellationToken ct = default)
        => await db.SpineEntries
            .AsNoTracking()
            .Where(e => e.SubjectUri == subjectUri)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SpineEntry>> GetByActorAsync(string actorId, CancellationToken ct = default)
        => await db.SpineEntries
            .AsNoTracking()
            .Where(e => e.ActorId == actorId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(ct);

    private static SpineEntry WithEntryHash(SpineEntry entry)
    {
        var hash = SpineChain.ComputeEntryHash(entry, entry.PreviousEntryHash);

        return new SpineEntry
        {
            Id = entry.Id,
            ReceiptUri = entry.ReceiptUri,
            SubjectUri = entry.SubjectUri,
            TenantId = entry.TenantId,
            ActorId = entry.ActorId,
            TimestampUtc = entry.TimestampUtc,
            Decision = entry.Decision,
            ReceiptClass = entry.ReceiptClass,
            PolicyHashValue = entry.PolicyHashValue,
            PolicyHashVersion = entry.PolicyHashVersion,
            InputHash = entry.InputHash,
            EvidenceRefsJson = entry.EvidenceRefsJson,
            EffectStatus = entry.EffectStatus,
            CreatedAt = entry.CreatedAt,
            SequenceNumber = entry.SequenceNumber,
            PreviousEntryHash = entry.PreviousEntryHash,
            EntryHash = hash,
        };
    }
}
