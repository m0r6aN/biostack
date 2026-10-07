namespace BioStack.Infrastructure.Governance;

using System.Text;
using BioStack.Domain.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Creates and verifies signed chain-head checkpoints (F3+).
/// </summary>
public interface ISpineCheckpointService
{
    Task<SpineChainCheckpoint> CreateCheckpointAsync(
        string? note = null,
        CancellationToken ct = default);

    Task<SpineChainCheckpoint?> GetLatestAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SpineChainCheckpoint>> ListAsync(
        int take = 20,
        CancellationToken ct = default);

    Task<SpineCheckpointVerificationResult> VerifyLatestAsync(CancellationToken ct = default);

    /// <summary>
    /// Export the latest checkpoint as a portable JSON document for off-box storage.
    /// </summary>
    Task<string?> ExportLatestManifestJsonAsync(CancellationToken ct = default);

    /// <summary>
    /// Create a checkpoint if the chain head is ahead of the latest checkpoint (cadence / auto).
    /// </summary>
    Task<SpineChainCheckpoint?> CheckpointIfAdvancedAsync(CancellationToken ct = default);
}

public sealed class SpineCheckpointService(
    BioStackDbContext db,
    ISpineRepository spine,
    IOptions<SpineCheckpointOptions> options,
    ILogger<SpineCheckpointService> logger) : ISpineCheckpointService
{
    public async Task<SpineChainCheckpoint> CreateCheckpointAsync(
        string? note = null,
        CancellationToken ct = default)
    {
        var head = await db.SpineEntries
            .AsNoTracking()
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync(ct);

        if (head is null)
            throw new InvalidOperationException(
                "Cannot checkpoint an empty Governed Spine — no entries have been written.");

        // Microsecond precision — matches SpineChain.Stamp so signatures survive DB round-trips.
        var utcNow = DateTime.UtcNow;
        var now = new DateTime(utcNow.Ticks - (utcNow.Ticks % 10), DateTimeKind.Utc);

        var opts = options.Value;
        var key = GetSigningKeyBytes(opts);
        string source;
        string algorithm;
        string signature;

        if (key.Length > 0)
        {
            var payload = SpineChain.BuildCheckpointPayload(
                head.SequenceNumber, head.EntryHash, now);
            signature = SpineChain.SignCheckpointPayload(payload, key);
            algorithm = SpineChain.CheckpointSignatureAlgorithmHmacSha256;
            source = opts.SigningKeyIsServerHeld
                ? SpineChain.CheckpointSourceServerHmac
                : SpineChain.CheckpointSourceLocalHmac;
        }
        else
        {
            signature = string.Empty;
            algorithm = string.Empty;
            source = SpineChain.CheckpointSourceUnsignedLocal;
            logger.LogWarning(
                "Creating unsigned Spine checkpoint at sequence {Sequence}. Configure "
                + "SpineCheckpoint:SigningKey for external anchoring (F3+).",
                head.SequenceNumber);
        }

        var checkpoint = new SpineChainCheckpoint
        {
            SequenceNumber = head.SequenceNumber,
            HeadEntryHash = head.EntryHash,
            CheckpointedAtUtc = now,
            Source = source,
            SignatureAlgorithm = algorithm,
            Signature = signature,
            Note = note,
        };

        db.SpineChainCheckpoints.Add(checkpoint);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Spine checkpoint {CheckpointId} at sequence {Sequence} source={Source}",
            checkpoint.Id, checkpoint.SequenceNumber, checkpoint.Source);

        return checkpoint;
    }

    public Task<SpineChainCheckpoint?> GetLatestAsync(CancellationToken ct = default)
        => db.SpineChainCheckpoints
            .AsNoTracking()
            .OrderByDescending(c => c.CheckpointedAtUtc)
            .ThenByDescending(c => c.SequenceNumber)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SpineChainCheckpoint>> ListAsync(
        int take = 20,
        CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        return await db.SpineChainCheckpoints
            .AsNoTracking()
            .OrderByDescending(c => c.CheckpointedAtUtc)
            .ThenByDescending(c => c.SequenceNumber)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<SpineCheckpointVerificationResult> VerifyLatestAsync(
        CancellationToken ct = default)
    {
        var chain = await spine.VerifyChainAsync(ct);
        if (!chain.IsIntact)
        {
            return new SpineCheckpointVerificationResult(
                ChainIntact: false,
                CheckpointPresent: false,
                HeadMatchesCheckpoint: false,
                SignatureValid: false,
                ExternallyAnchored: false,
                ChainEntriesVerified: chain.EntriesVerified,
                CheckpointSequenceNumber: null,
                CheckpointHeadEntryHash: null,
                Reason: chain.Reason ?? "Chain is not intact.");
        }

        var checkpoint = await GetLatestAsync(ct);
        if (checkpoint is null)
        {
            return new SpineCheckpointVerificationResult(
                ChainIntact: true,
                CheckpointPresent: false,
                HeadMatchesCheckpoint: false,
                SignatureValid: false,
                ExternallyAnchored: false,
                ChainEntriesVerified: chain.EntriesVerified,
                CheckpointSequenceNumber: null,
                CheckpointHeadEntryHash: null,
                Reason: "No checkpoint has been recorded yet.");
        }

        var head = await db.SpineEntries
            .AsNoTracking()
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync(ct);

        // Head match: the entry AT the checkpoint sequence must still carry that hash.
        // (Chain may have advanced after the checkpoint — that is fine.)
        var entryAtCheckpoint = await db.SpineEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SequenceNumber == checkpoint.SequenceNumber, ct);

        var headMatches = entryAtCheckpoint is not null
            && string.Equals(
                entryAtCheckpoint.EntryHash,
                checkpoint.HeadEntryHash,
                StringComparison.Ordinal);

        var externallyAnchored = checkpoint.Source is SpineChain.CheckpointSourceLocalHmac
            or SpineChain.CheckpointSourceServerHmac;

        var key = GetSigningKeyBytes(options.Value);
        bool signatureValid;
        if (!externallyAnchored)
        {
            signatureValid = false;
        }
        else if (key.Length == 0)
        {
            signatureValid = false;
        }
        else
        {
            signatureValid = SpineChain.VerifyCheckpointSignature(
                checkpoint.SequenceNumber,
                checkpoint.HeadEntryHash,
                checkpoint.CheckpointedAtUtc,
                checkpoint.Signature,
                key);
        }

        string? reason = null;
        if (!headMatches)
        {
            reason = entryAtCheckpoint is null
                ? $"Checkpoint sequence {checkpoint.SequenceNumber} is missing from the chain "
                  + "(entry deleted after checkpoint)."
                : "Checkpoint head hash does not match the entry at that sequence "
                  + "(ledger rewritten after checkpoint).";
        }
        else if (externallyAnchored && !signatureValid)
        {
            reason = key.Length == 0
                ? "Checkpoint is signed but SpineCheckpoint:SigningKey is not configured for verification."
                : "Checkpoint signature is invalid (wrong key or tampered checkpoint row).";
        }
        else if (!externallyAnchored)
        {
            reason = "Checkpoint is unsigned-local — chain matches, but there is no external anchor.";
        }

        // If chain advanced past checkpoint, still valid as long as historical head matches.
        _ = head;

        return new SpineCheckpointVerificationResult(
            ChainIntact: true,
            CheckpointPresent: true,
            HeadMatchesCheckpoint: headMatches,
            SignatureValid: signatureValid,
            ExternallyAnchored: externallyAnchored,
            ChainEntriesVerified: chain.EntriesVerified,
            CheckpointSequenceNumber: checkpoint.SequenceNumber,
            CheckpointHeadEntryHash: checkpoint.HeadEntryHash,
            Reason: reason);
    }

    public async Task<string?> ExportLatestManifestJsonAsync(CancellationToken ct = default)
    {
        var checkpoint = await GetLatestAsync(ct);
        if (checkpoint is null)
            return null;

        // Portable, stable field order for offline storage / server ingest.
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            schema = "biostack.spine-chain-checkpoint.v1",
            id = checkpoint.Id,
            sequenceNumber = checkpoint.SequenceNumber,
            headEntryHash = checkpoint.HeadEntryHash,
            checkpointedAtUtc = checkpoint.CheckpointedAtUtc.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ"),
            source = checkpoint.Source,
            signatureAlgorithm = checkpoint.SignatureAlgorithm,
            signature = checkpoint.Signature,
            note = checkpoint.Note,
        });
    }

    public async Task<SpineChainCheckpoint?> CheckpointIfAdvancedAsync(CancellationToken ct = default)
    {
        var head = await db.SpineEntries
            .AsNoTracking()
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync(ct);

        if (head is null)
            return null;

        var latest = await GetLatestAsync(ct);
        if (latest is not null && latest.SequenceNumber >= head.SequenceNumber)
            return null;

        return await CreateCheckpointAsync(note: "auto", ct);
    }

    private static byte[] GetSigningKeyBytes(SpineCheckpointOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.SigningKey))
            return [];
        return Encoding.UTF8.GetBytes(opts.SigningKey);
    }
}
