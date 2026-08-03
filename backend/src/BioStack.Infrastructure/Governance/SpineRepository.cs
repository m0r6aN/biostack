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
