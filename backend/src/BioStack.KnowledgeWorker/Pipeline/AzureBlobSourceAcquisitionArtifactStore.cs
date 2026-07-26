namespace BioStack.KnowledgeWorker.Pipeline;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

/// <summary>
/// Managed-identity-only durable store for the governed source-acquisition runtime.
/// Container creation and lifecycle deletion are intentionally outside this process.
/// </summary>
internal sealed partial class AzureBlobSourceAcquisitionArtifactStore
    : ISourceAcquisitionArtifactStore
{
    internal const int LeaseSeconds = 60;
    internal const int LeaseRenewalSeconds = 20;
    internal const int MaximumMetadataBytes = 64 * 1024;

    private readonly BlobContainerClient _container;
    private readonly string _cyclePrefix;

    public AzureBlobSourceAcquisitionArtifactStore(
        SourceAcquisitionRuntimeConfiguration configuration)
        : this(configuration, CreateContainerClient(configuration))
    {
    }

    internal AzureBlobSourceAcquisitionArtifactStore(
        SourceAcquisitionRuntimeConfiguration configuration,
        BlobContainerClient container)
    {
        ValidateConfiguration(configuration);
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _cyclePrefix = Join(
            NormalizePrefix(configuration.BlobPrefix),
            "v1",
            configuration.CycleId);
        Location =
            $"{configuration.BlobServiceUri!.TrimEnd('/')}/" +
            $"{configuration.BlobContainerName}/{_cyclePrefix}";
    }

    public string Location { get; }

    internal static void ValidateConfiguration(
        SourceAcquisitionRuntimeConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!Uri.TryCreate(
                configuration.BlobServiceUri,
                UriKind.Absolute,
                out var serviceUri)
            || serviceUri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(serviceUri.Query)
            || !string.IsNullOrEmpty(serviceUri.Fragment)
            || serviceUri.AbsolutePath.Trim('/').Length != 0)
        {
            throw new InvalidOperationException(
                "Azure Blob storage requires a fixed HTTPS account service URI.");
        }
        if (!ContainerNameRegex().IsMatch(
                configuration.BlobContainerName ?? string.Empty)
            || configuration.BlobContainerName!.Contains(
                "--",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Azure Blob storage requires a valid fixed private container name.");
        }
        _ = NormalizePrefix(configuration.BlobPrefix);
    }

    public async Task<ISourceAcquisitionRunLease> AcquireRunLeaseAsync(
        CancellationToken cancellationToken)
    {
        var blob = Blob(RunLockName());
        try
        {
            await blob.UploadAsync(
                BinaryData.FromBytes([]),
                new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions
                    {
                        IfNoneMatch = ETag.All,
                    },
                },
                cancellationToken);
        }
        catch (RequestFailedException exception)
            when (exception.Status is 409 or 412)
        {
            // The lock blob is durable. Exclusivity is provided by its lease.
        }

        var leaseClient = blob.GetBlobLeaseClient(Guid.NewGuid().ToString("N"));
        try
        {
            var acquired = await leaseClient.AcquireAsync(
                TimeSpan.FromSeconds(LeaseSeconds),
                cancellationToken: cancellationToken);
            return new RenewingBlobRunLease(
                leaseClient,
                acquired.Value.LeaseId);
        }
        catch (RequestFailedException exception)
            when (exception.Status is 409 or 412)
        {
            throw new InvalidOperationException(
                "Another source-acquisition runner holds the cycle lease.",
                exception);
        }
    }

    public async Task<SourceAcquisitionAttemptArtifact?> TryReadAttemptAsync(
        string intentId,
        SourceAcquisitionIntent intent,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionRuntimeConfiguration configuration,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            return await TryReadAttemptCoreAsync(
                intentId,
                intent,
                entry,
                bindings,
                configuration,
                nowUtc,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            try
            {
                await QuarantineIntentAsync(
                    intentId,
                    nowUtc,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Source acquisition intent '{intentId}' failed integrity validation; quarantine cleanup is incomplete.");
            }
            throw new InvalidOperationException(
                $"Source acquisition intent '{intentId}' failed integrity validation and was quarantined.");
        }
    }

    private async Task<SourceAcquisitionAttemptArtifact?> TryReadAttemptCoreAsync(
        string intentId,
        SourceAcquisitionIntent intent,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionRuntimeConfiguration configuration,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var priorMarkers = await ListAsync(
            QuarantinePrefix(intentId),
            cancellationToken);
        if (priorMarkers.Any(item =>
                item.Name.EndsWith(
                    "/quarantine-metadata.json",
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "This intent has an unresolved integrity quarantine.");
        }

        var tombstone = await TryReadTombstoneAsync(
            intentId,
            intent,
            entry,
            configuration,
            cancellationToken);
        if (tombstone is not null)
        {
            await RemoveExpiredContentAsync(
                intentId,
                tombstone,
                nowUtc,
                cancellationToken);
            return ToExpiredAttempt(tombstone, intent, bindings);
        }

        var attemptItem = await FindAttemptAsync(intentId, cancellationToken);
        if (attemptItem is null)
        {
            if (await ExistsAsync(CheckpointName(intentId), cancellationToken))
            {
                throw new InvalidOperationException(
                    "A checkpoint exists without its immutable attempt.");
            }
            return null;
        }

        var bytes = await ReadBoundedAsync(
            attemptItem.Value.Name,
            SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
            cancellationToken);
        var pathHash = attemptItem.Value.Name.Split('/')[^1][..^5];
        if (!string.Equals(pathHash, Sha256(bytes), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Source acquisition attempt '{intentId}' failed its content-address check.");
        }
        var attempt =
            JsonSerializer.Deserialize<SourceAcquisitionAttemptArtifact>(
                bytes,
                SourceAcquisitionRunner.JsonOptions)
            ?? throw new InvalidOperationException(
                $"Source acquisition attempt '{intentId}' is invalid.");
        if (!AttemptMatchesExpected(
                attempt,
                intentId,
                intent,
                entry,
                bindings,
                configuration))
        {
            throw new InvalidOperationException(
                $"Source acquisition attempt '{intentId}' failed its resume-boundary check.");
        }
        if (nowUtc >= attempt.RetainUntilUtc)
        {
            var removed = await TombstoneAndRemoveAsync(
                attempt,
                attemptItem.Value,
                nowUtc,
                cancellationToken);
            return ToExpiredAttempt(removed, intent, bindings);
        }

        var checkpoint = await TryReadBoundedAsync(
            CheckpointName(intentId),
            64 * 1024,
            cancellationToken);
        if (checkpoint is not null)
        {
            var value =
                JsonSerializer.Deserialize<SourceAcquisitionCheckpoint>(
                    checkpoint.Value.Bytes,
                    SourceAcquisitionRunner.JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Source acquisition checkpoint '{intentId}' is invalid.");
            if (value.IntentId != intentId
                || value.AttemptSha256 != Sha256(bytes))
            {
                throw new InvalidOperationException(
                    $"Source acquisition checkpoint '{intentId}' failed its integrity check.");
            }
        }
        return attempt;
    }

    public async Task WriteAttemptAndCheckpointAsync(
        SourceAcquisitionAttemptArtifact attempt,
        CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            attempt,
            SourceAcquisitionRunner.JsonOptions);
        if (bytes.Length > SourceAcquisitionRuntimeLimits.MaximumAttemptBytes)
        {
            throw new SourceAcquisitionException(
                "attempt-artifact-too-large",
                "The normalized attempt artifact exceeded the runtime size limit.");
        }
        await WriteImmutableAsync(
            AttemptName(attempt.IntentId, Sha256(bytes)),
            bytes,
            cancellationToken);
        await EnsureCheckpointAsync(attempt, cancellationToken);
    }

    public async Task EnsureCheckpointAsync(
        SourceAcquisitionAttemptArtifact attempt,
        CancellationToken cancellationToken)
    {
        var item = await FindAttemptAsync(attempt.IntentId, cancellationToken)
                   ?? throw new InvalidOperationException(
                       "The immutable attempt is missing.");
        var bytes = await ReadBoundedAsync(
            item.Name,
            SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
            cancellationToken);
        var checkpoint = new SourceAcquisitionCheckpoint(
            SourceAcquisitionRunner.SchemaVersion,
            attempt.CycleId,
            attempt.IntentId,
            attempt.Status,
            item.Name[(_cyclePrefix.Length + 1)..],
            Sha256(bytes),
            attempt.CompletedAtUtc);
        await WriteMutableAsync(
            CheckpointName(attempt.IntentId),
            JsonSerializer.SerializeToUtf8Bytes(
                checkpoint,
                SourceAcquisitionRunner.JsonOptions),
            cancellationToken);
    }

    public async Task WriteDerivedArtifactsAsync(
        SourceAcquisitionRunManifest manifest,
        SourceAcquisitionReviewQueue reviewQueue,
        CancellationToken cancellationToken)
    {
        await WriteMutableAsync(
            Name("run-manifest.json"),
            JsonSerializer.SerializeToUtf8Bytes(
                manifest,
                SourceAcquisitionRunner.JsonOptions),
            cancellationToken);
        await WriteMutableAsync(
            Name("source-acquisition-review-queue.json"),
            JsonSerializer.SerializeToUtf8Bytes(
                reviewQueue,
                SourceAcquisitionRunner.JsonOptions),
            cancellationToken);
    }

    private async Task<SourceAcquisitionTombstone?> TryReadTombstoneAsync(
        string intentId,
        SourceAcquisitionIntent intent,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionRuntimeConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var stored = await TryReadBoundedAsync(
            TombstoneName(intentId),
            MaximumMetadataBytes,
            cancellationToken);
        if (stored is null) return null;
        var tombstone =
            JsonSerializer.Deserialize<SourceAcquisitionTombstone>(
                stored.Value.Bytes,
                SourceAcquisitionRunner.JsonOptions)
            ?? throw new InvalidOperationException(
                $"Source acquisition tombstone '{intentId}' is invalid.");
        var retentionDays = tombstone.OriginalStatus
            is "completed" or "truncated"
            ? configuration.CandidateRetentionDays
            : configuration.ReceiptRetentionDays;
        var valid = tombstone.SchemaVersion == SourceAcquisitionRunner.SchemaVersion
                    && tombstone.CycleId == configuration.CycleId
                    && tombstone.IntentId == intentId
                    && tombstone.StableOrdinal == entry.StableOrdinal
                    && tombstone.SourceId == intent.SourceId
                    && tombstone.RequestId == intent.RequestId
                    && TerminalStatuses.Contains(tombstone.OriginalStatus)
                    && SourceAcquisitionRunner.IsSha256(tombstone.AttemptSha256)
                    && tombstone.CompletedAtUtc != default
                    && tombstone.CompletedAtUtc.Offset == TimeSpan.Zero
                    && tombstone.RetainUntilUtc
                    == tombstone.CompletedAtUtc.AddDays(retentionDays)
                    && tombstone.RemovedAtUtc >= tombstone.RetainUntilUtc
                    && tombstone.RemovedAtUtc.Offset == TimeSpan.Zero
                    && tombstone.RemovalReason == "retention-expired";
        if (!valid)
        {
            throw new InvalidOperationException(
                $"Source acquisition tombstone '{intentId}' failed its integrity check.");
        }
        return tombstone;
    }

    private async Task<SourceAcquisitionTombstone> TombstoneAndRemoveAsync(
        SourceAcquisitionAttemptArtifact attempt,
        StoredBlob attemptBlob,
        DateTimeOffset removedAtUtc,
        CancellationToken cancellationToken)
    {
        var attemptBytes = await ReadBoundedAsync(
            attemptBlob.Name,
            SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
            cancellationToken);
        var existingTombstone = await TryReadBoundedAsync(
            TombstoneName(attempt.IntentId),
            MaximumMetadataBytes,
            cancellationToken);
        if (existingTombstone is not null)
        {
            var existing =
                JsonSerializer.Deserialize<SourceAcquisitionTombstone>(
                    existingTombstone.Value.Bytes,
                    SourceAcquisitionRunner.JsonOptions)
                ?? throw new InvalidOperationException(
                    "An existing retention tombstone is invalid.");
            if (!TombstoneMatchesAttempt(
                    existing,
                    attempt,
                    Sha256(attemptBytes)))
            {
                throw new InvalidOperationException(
                    "An existing retention tombstone does not match the immutable attempt.");
            }
            await DeleteIfMatchAsync(attemptBlob, cancellationToken);
            var existingCheckpoint = await TryGetAsync(
                CheckpointName(attempt.IntentId),
                cancellationToken);
            if (existingCheckpoint is not null)
            {
                await DeleteIfMatchAsync(
                    existingCheckpoint.Value,
                    cancellationToken);
            }
            return existing;
        }
        var tombstone = new SourceAcquisitionTombstone(
            SourceAcquisitionRunner.SchemaVersion,
            attempt.CycleId,
            attempt.IntentId,
            attempt.StableOrdinal,
            attempt.SourceId,
            attempt.RequestId,
            attempt.Status,
            Sha256(attemptBytes),
            attempt.CompletedAtUtc,
            attempt.RetainUntilUtc,
            removedAtUtc,
            "retention-expired");
        await WriteImmutableAsync(
            TombstoneName(attempt.IntentId),
            JsonSerializer.SerializeToUtf8Bytes(
                tombstone,
                SourceAcquisitionRunner.JsonOptions),
            cancellationToken);

        // The tombstone is committed first. Every subsequent deletion is
        // conditional on the exact ETag observed before the tombstone.
        await DeleteIfMatchAsync(attemptBlob, cancellationToken);
        var checkpoint = await TryGetAsync(
            CheckpointName(attempt.IntentId),
            cancellationToken);
        if (checkpoint is not null)
        {
            try
            {
                await DeleteIfMatchAsync(checkpoint.Value, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Never widen a checkpoint ETag race into intent-wide deletion.
            }
        }
        return tombstone;
    }

    private static bool TombstoneMatchesAttempt(
        SourceAcquisitionTombstone tombstone,
        SourceAcquisitionAttemptArtifact attempt,
        string attemptSha256) =>
        tombstone.SchemaVersion == SourceAcquisitionRunner.SchemaVersion
        && tombstone.CycleId == attempt.CycleId
        && tombstone.IntentId == attempt.IntentId
        && tombstone.StableOrdinal == attempt.StableOrdinal
        && tombstone.SourceId == attempt.SourceId
        && tombstone.RequestId == attempt.RequestId
        && tombstone.OriginalStatus == attempt.Status
        && tombstone.AttemptSha256 == attemptSha256
        && tombstone.CompletedAtUtc == attempt.CompletedAtUtc
        && tombstone.RetainUntilUtc == attempt.RetainUntilUtc
        && tombstone.RemovedAtUtc >= attempt.RetainUntilUtc
        && tombstone.RemovedAtUtc.Offset == TimeSpan.Zero
        && tombstone.RemovalReason == "retention-expired";

    private async Task RemoveExpiredContentAsync(
        string intentId,
        SourceAcquisitionTombstone tombstone,
        DateTimeOffset quarantinedAtUtc,
        CancellationToken cancellationToken)
    {
        var checkpoint = await TryGetAsync(
            CheckpointName(intentId),
            cancellationToken);
        if (checkpoint is not null)
        {
            await DeleteIfMatchAsync(checkpoint.Value, cancellationToken);
        }
        var attempts = await ListAsync(AttemptPrefix(intentId), cancellationToken);
        foreach (var attempt in attempts)
        {
            try
            {
                var bytes = await ReadStoredBlobBoundedAsync(
                    attempt,
                    SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                    cancellationToken);
                var filename = attempt.Name.Split('/')[^1];
                var filenameSha256 = filename.EndsWith(
                    ".json",
                    StringComparison.Ordinal)
                    ? filename[..^5]
                    : string.Empty;
                var contentSha256 = Sha256(bytes);
                if (!SourceAcquisitionRunner.IsSha256(filenameSha256)
                    || filenameSha256 != contentSha256
                    || tombstone.AttemptSha256 != contentSha256)
                {
                    await TryQuarantineBlobAsync(
                        intentId,
                        attempt,
                        quarantinedAtUtc,
                        "tombstone-resume-attempt-mismatch",
                        cancellationToken);
                    continue;
                }
                try
                {
                    await DeleteIfMatchAsync(attempt, cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    // The exact ETag read is no longer current; leave the
                    // replacement for the next stable pass.
                }
            }
            catch (RequestFailedException exception)
                when (exception.Status is 404 or 412)
            {
                // A disappearance is already terminal. An ETag race is never
                // deleted using stale observations and is retried next pass.
            }
            catch (InvalidOperationException)
            {
                await TryQuarantineBlobAsync(
                    intentId,
                    attempt,
                    quarantinedAtUtc,
                    "tombstone-resume-attempt-invalid",
                    cancellationToken);
            }
        }
    }

    private async Task<bool> TryQuarantineBlobAsync(
        string intentId,
        StoredBlob suspect,
        DateTimeOffset quarantinedAtUtc,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await QuarantineBlobAsync(
                intentId,
                suspect,
                quarantinedAtUtc,
                reasonCode,
                cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // The marker or exact conditional deletion did not complete.
            // Leave the suspect for a later pass; never widen this into an
            // intent-wide quarantine.
            return false;
        }
    }

    private async Task QuarantineBlobAsync(
        string intentId,
        StoredBlob suspect,
        DateTimeOffset quarantinedAtUtc,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        var markerName = Join(
            QuarantinePrefix(intentId),
            $"{quarantinedAtUtc:yyyyMMddHHmmssfffffff}-{Guid.NewGuid():N}",
            "quarantine-metadata.json");
        var metadata = new
        {
            schemaVersion = SourceAcquisitionRunner.SchemaVersion,
            intentId,
            quarantinedAtUtc,
            reasonCode,
            artifactDisposition = "content-free-evidence-only",
            artifactCount = 1,
            artifacts = new[] { "000-redacted" },
        };
        await WriteImmutableAsync(
            markerName,
            JsonSerializer.SerializeToUtf8Bytes(
                metadata,
                SourceAcquisitionRunner.JsonOptions),
            cancellationToken);

        // The immutable content-free marker is durable before the exact
        // suspect observation is conditionally removed.
        await DeleteIfMatchAsync(suspect, cancellationToken);
    }

    private async Task QuarantineIntentAsync(
        string intentId,
        DateTimeOffset quarantinedAtUtc,
        CancellationToken cancellationToken)
    {
        await PurgeQuarantinePayloadsAsync(intentId, cancellationToken);
        var suspect = await ListAsync(IntentPrefix(intentId), cancellationToken);
        if (suspect.Count == 0) return;

        var markerName = Join(
            QuarantinePrefix(intentId),
            $"{quarantinedAtUtc:yyyyMMddHHmmssfffffff}-{Guid.NewGuid():N}",
            "quarantine-metadata.json");
        var metadata = new
        {
            schemaVersion = SourceAcquisitionRunner.SchemaVersion,
            intentId,
            quarantinedAtUtc,
            reasonCode = "integrity-validation-failed",
            artifactDisposition = "content-free-evidence-only",
            artifactCount = suspect.Count,
            artifacts = suspect
                .Select((_, index) => $"{index:D3}-redacted")
                .ToList(),
        };
        await WriteImmutableAsync(
            markerName,
            JsonSerializer.SerializeToUtf8Bytes(
                metadata,
                SourceAcquisitionRunner.JsonOptions),
            cancellationToken);

        // Marker-first and content-free: suspect bytes are never copied into
        // quarantine. Conditional deletion follows the immutable evidence marker.
        foreach (var item in suspect)
        {
            await DeleteIfMatchAsync(item, cancellationToken);
        }
    }

    private async Task PurgeQuarantinePayloadsAsync(
        string intentId,
        CancellationToken cancellationToken)
    {
        var blobs = await ListAsync(QuarantinePrefix(intentId), cancellationToken);
        foreach (var blob in blobs.Where(item =>
                     !item.Name.EndsWith(
                         "/quarantine-metadata.json",
                         StringComparison.Ordinal)))
        {
            await DeleteIfMatchAsync(blob, cancellationToken);
        }
    }

    private async Task<StoredBlob?> FindAttemptAsync(
        string intentId,
        CancellationToken cancellationToken)
    {
        var attempts = await ListAsync(AttemptPrefix(intentId), cancellationToken);
        return attempts.Count switch
        {
            0 => null,
            1 => attempts[0],
            _ => throw new InvalidOperationException(
                $"Intent '{intentId}' has multiple immutable attempts in one cycle."),
        };
    }

    private async Task WriteImmutableAsync(
        string name,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        var blob = Blob(name);
        try
        {
            await blob.UploadAsync(
                BinaryData.FromBytes(bytes),
                new BlobUploadOptions
                {
                    HttpHeaders = JsonHeaders,
                    Conditions = new BlobRequestConditions
                    {
                        IfNoneMatch = ETag.All,
                    },
                },
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            var maximumExistingBytes =
                name.EndsWith("/tombstone.json", StringComparison.Ordinal)
                || name.EndsWith(
                    "/quarantine-metadata.json",
                    StringComparison.Ordinal)
                    ? MaximumMetadataBytes
                    : SourceAcquisitionRuntimeLimits.MaximumAttemptBytes;
            var existing = await ReadBoundedAsync(
                name,
                maximumExistingBytes,
                cancellationToken);
            if (existing.AsSpan().SequenceEqual(bytes)) return;
            throw new InvalidOperationException(
                "An immutable source-acquisition artifact already exists with different content.",
                exception);
        }
    }

    private async Task WriteMutableAsync(
        string name,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        var current = await TryGetAsync(name, cancellationToken);
        var conditions = current is null
            ? new BlobRequestConditions { IfNoneMatch = ETag.All }
            : new BlobRequestConditions { IfMatch = current.Value.ETag };
        try
        {
            await Blob(name).UploadAsync(
                BinaryData.FromBytes(bytes),
                new BlobUploadOptions
                {
                    HttpHeaders = JsonHeaders,
                    Conditions = conditions,
                },
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            throw new InvalidOperationException(
                "A mutable source-acquisition artifact changed concurrently.",
                exception);
        }
    }

    private async Task DeleteIfMatchAsync(
        StoredBlob blob,
        CancellationToken cancellationToken)
    {
        try
        {
            await Blob(blob.Name).DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                new BlobRequestConditions { IfMatch = blob.ETag },
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            throw new InvalidOperationException(
                "A source-acquisition artifact changed before conditional deletion.",
                exception);
        }
    }

    private async Task<byte[]> ReadBoundedAsync(
        string name,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        var read = await TryReadBoundedAsync(name, maximumBytes, cancellationToken);
        return read?.Bytes
               ?? throw new InvalidOperationException(
                   "A required source-acquisition artifact is missing.");
    }

    private async Task<StoredBytes?> TryReadBoundedAsync(
        string name,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        var item = await TryGetAsync(name, cancellationToken);
        if (item is null) return null;
        if (item.Value.Length > maximumBytes)
        {
            throw new InvalidOperationException(
                $"Artifact '{name.Split('/')[^1]}' exceeds its size limit.");
        }
        var bytes = await ReadStoredBlobBoundedAsync(
            item.Value,
            maximumBytes,
            cancellationToken);
        return new StoredBytes(bytes, item.Value.ETag);
    }

    private async Task<byte[]> ReadStoredBlobBoundedAsync(
        StoredBlob item,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (item.Length < 0 || item.Length > maximumBytes)
        {
            throw new InvalidOperationException(
                $"Artifact '{item.Name.Split('/')[^1]}' exceeds its size limit.");
        }
        var response = await Blob(item.Name).DownloadStreamingAsync(
            new BlobDownloadOptions
            {
                Range = new HttpRange(0, maximumBytes + 1L),
                Conditions = new BlobRequestConditions
                {
                    IfMatch = item.ETag,
                },
            },
            cancellationToken);
        if (response.Value.Details.ETag != item.ETag)
        {
            throw new InvalidOperationException(
                "A source-acquisition artifact changed during its bounded read.");
        }
        await using var stream = response.Value.Content;
        using var output = new MemoryStream(
            Math.Min(maximumBytes, 64 * 1024));
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (output.Length + read > maximumBytes)
            {
                throw new InvalidOperationException(
                    $"Artifact '{item.Name.Split('/')[^1]}' exceeds its streamed size limit.");
            }
            output.Write(buffer, 0, read);
        }
        if (output.Length != item.Length)
        {
            throw new InvalidOperationException(
                "A source-acquisition artifact changed length during its bounded read.");
        }
        return output.ToArray();
    }

    private async Task<bool> ExistsAsync(
        string name,
        CancellationToken cancellationToken) =>
        await TryGetAsync(name, cancellationToken) is not null;

    private async Task<StoredBlob?> TryGetAsync(
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            var properties = await Blob(name).GetPropertiesAsync(
                cancellationToken: cancellationToken);
            return new StoredBlob(
                name,
                properties.Value.ETag,
                properties.Value.ContentLength);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<StoredBlob>> ListAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        var results = new List<StoredBlob>();
        await foreach (var item in _container.GetBlobsAsync(
                           traits: BlobTraits.None,
                           states: BlobStates.None,
                           prefix: prefix,
                           cancellationToken: cancellationToken))
        {
            results.Add(new StoredBlob(
                item.Name,
                item.Properties.ETag
                ?? throw new InvalidOperationException(
                    "A listed source-acquisition blob has no ETag."),
                item.Properties.ContentLength
                ?? throw new InvalidOperationException(
                    "A listed source-acquisition blob has no content length.")));
        }
        return results
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToList();
    }

    private BlobClient Blob(string name)
    {
        if (!name.StartsWith(
                _cyclePrefix + "/",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Source-acquisition Blob key escaped its fixed cycle prefix.");
        }
        return _container.GetBlobClient(name);
    }

    private string RunLockName() => Name("run.lock");

    private string AttemptName(string intentId, string sha256)
    {
        RequireIntentId(intentId);
        if (!SourceAcquisitionRunner.IsSha256(sha256))
        {
            throw new InvalidOperationException(
                "Attempt content hash must be lowercase SHA-256.");
        }
        return Join(AttemptPrefix(intentId), $"{sha256}.json");
    }

    private string AttemptPrefix(string intentId) =>
        Join(IntentPrefix(intentId), "attempts") + "/";

    private string CheckpointName(string intentId) =>
        Join(IntentPrefix(intentId), "checkpoint.json");

    private string TombstoneName(string intentId) =>
        Join(IntentPrefix(intentId), "tombstone.json");

    private string IntentPrefix(string intentId)
    {
        RequireIntentId(intentId);
        return Name("intents", intentId);
    }

    private string QuarantinePrefix(string intentId)
    {
        RequireIntentId(intentId);
        return Name("quarantine", intentId) + "/";
    }

    private string Name(params string[] segments) =>
        Join([_cyclePrefix, .. segments]);

    private static void RequireIntentId(string intentId)
    {
        if (!SourceAcquisitionRunner.IsSha256(intentId))
        {
            throw new InvalidOperationException(
                "Intent ID must be a lowercase SHA-256 value.");
        }
    }

    private static BlobContainerClient CreateContainerClient(
        SourceAcquisitionRuntimeConfiguration configuration)
    {
        ValidateConfiguration(configuration);
        var credential = string.IsNullOrWhiteSpace(
            configuration.ManagedIdentityClientId)
            ? new ManagedIdentityCredential(
                ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(
                    configuration.ManagedIdentityClientId));
        var clientOptions = new BlobClientOptions();
        clientOptions.Retry.MaxRetries = 0;
        clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);
        return new BlobServiceClient(
                new Uri(configuration.BlobServiceUri!, UriKind.Absolute),
                credential,
                clientOptions)
            .GetBlobContainerClient(configuration.BlobContainerName);
    }

    private static string NormalizePrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.StartsWith('/')
            || value.EndsWith('/')
            || value.Contains('\\')
            || value.Contains("//", StringComparison.Ordinal)
            || value.Split('/').Any(segment =>
                segment is "" or "." or ".."
                || !SafeSegmentRegex().IsMatch(segment)))
        {
            throw new InvalidOperationException(
                "Azure Blob storage requires a fixed safe relative prefix.");
        }
        return value;
    }

    private static string Join(params string[] segments) =>
        string.Join(
            '/',
            segments.Select(segment => segment.Trim('/')));

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static SourceAcquisitionAttemptArtifact ToExpiredAttempt(
        SourceAcquisitionTombstone tombstone,
        SourceAcquisitionIntent intent,
        SourceAcquisitionInputBindings bindings) =>
        new(
            SourceAcquisitionRunner.SchemaVersion,
            tombstone.CycleId,
            tombstone.IntentId,
            tombstone.StableOrdinal,
            tombstone.SourceId,
            intent.AdapterId,
            tombstone.RequestId,
            intent.CompoundName,
            intent.CandidateMethod,
            intent.RegistryBindingSha256,
            bindings,
            intent.SearchTerms.ToList(),
            intent.AuthorizedFieldUses
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList(),
            intent.RequiredProvenanceFields
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList(),
            "expired",
            tombstone.CompletedAtUtc,
            tombstone.RetainUntilUtc,
            false,
            null,
            null,
            null,
            tombstone.OriginalStatus,
            []);

    private static bool AttemptMatchesExpected(
        SourceAcquisitionAttemptArtifact attempt,
        string intentId,
        SourceAcquisitionIntent intent,
        SourceAcquisitionPreflightEntry entry,
        SourceAcquisitionInputBindings bindings,
        SourceAcquisitionRuntimeConfiguration configuration)
    {
        if (attempt.Candidates is null) return false;
        var valid = attempt.SchemaVersion == SourceAcquisitionRunner.SchemaVersion
                    && attempt.CycleId == configuration.CycleId
                    && attempt.IntentId == intentId
                    && attempt.StableOrdinal == entry.StableOrdinal
                    && attempt.SourceId == intent.SourceId
                    && attempt.AdapterId == intent.AdapterId
                    && attempt.RequestId == intent.RequestId
                    && attempt.CompoundName == intent.CompoundName
                    && attempt.CandidateMethod == intent.CandidateMethod
                    && attempt.RegistryBindingSha256
                    == intent.RegistryBindingSha256
                    && attempt.InputBindings == bindings
                    && attempt.SearchTerms.SequenceEqual(intent.SearchTerms)
                    && attempt.AuthorizedFieldUses.SequenceEqual(
                        intent.AuthorizedFieldUses.OrderBy(
                            value => value,
                            StringComparer.Ordinal))
                    && attempt.RequiredProvenanceFields.SequenceEqual(
                        intent.RequiredProvenanceFields.OrderBy(
                            value => value,
                            StringComparer.Ordinal))
                    && TerminalStatuses.Contains(attempt.Status)
                    && attempt.CompletedAtUtc != default
                    && attempt.CompletedAtUtc.Offset == TimeSpan.Zero
                    && attempt.RetainUntilUtc
                    == attempt.CompletedAtUtc.AddDays(
                        attempt.Status is "completed" or "truncated"
                            ? configuration.CandidateRetentionDays
                            : configuration.ReceiptRetentionDays)
                    && attempt.RetainUntilUtc.Offset == TimeSpan.Zero
                    && attempt.TombstoneOriginalStatus is null;
        if (!valid) return false;
        if (intent.CandidateMethod == "manual-review")
        {
            return attempt.Status == "manual-review-pending"
                   && attempt.Candidates.Count == 0;
        }
        if (attempt.Status is not "completed" and not "truncated"
            && attempt.Candidates.Count != 0)
        {
            return false;
        }
        if (attempt.Candidates.Count
            > SourceAcquisitionRuntimeLimits.MaximumCandidatesPerIntent)
        {
            return false;
        }
        try
        {
            var normalized = SourceAcquisitionRunner.NormalizeCandidates(
                attempt.Candidates,
                intent);
            return normalized.Select(candidate =>
                       (candidate.SourceRegistryId, candidate.SourceItemId))
                .SequenceEqual(attempt.Candidates.Select(candidate =>
                    (candidate.SourceRegistryId, candidate.SourceItemId)));
        }
        catch (SourceAcquisitionException)
        {
            return false;
        }
    }

    private sealed class RenewingBlobRunLease
        : ISourceAcquisitionRunLease
    {
        private readonly BlobLeaseClient _leaseClient;
        private readonly CancellationTokenSource _stop = new();
        private readonly CancellationTokenSource _lost = new();
        private readonly Task _renewal;

        public RenewingBlobRunLease(
            BlobLeaseClient leaseClient,
            string leaseId)
        {
            _leaseClient = leaseClient;
            LeaseId = leaseId;
            _renewal = RenewAsync();
        }

        public string LeaseId { get; }

        public CancellationToken LeaseLost => _lost.Token;

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            try
            {
                await _renewal;
            }
            catch (OperationCanceledException)
            {
            }
            try
            {
                await _leaseClient.ReleaseAsync();
            }
            catch (RequestFailedException)
            {
                // Lease expiry/loss is already represented by LeaseLost.
            }
            _stop.Dispose();
            _lost.Dispose();
        }

        private async Task RenewAsync()
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(LeaseRenewalSeconds));
            try
            {
                while (await timer.WaitForNextTickAsync(_stop.Token))
                {
                    try
                    {
                        await _leaseClient.RenewAsync(
                            cancellationToken: _stop.Token);
                    }
                    catch (OperationCanceledException)
                        when (_stop.IsCancellationRequested)
                    {
                        return;
                    }
                    catch
                    {
                        await _lost.CancelAsync();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
                when (_stop.IsCancellationRequested)
            {
            }
        }
    }

    private readonly record struct StoredBlob(
        string Name,
        ETag ETag,
        long Length);

    private readonly record struct StoredBytes(byte[] Bytes, ETag ETag);

    private static readonly BlobHttpHeaders JsonHeaders = new()
    {
        ContentType = "application/json",
    };

    private static readonly HashSet<string> TerminalStatuses =
        new(StringComparer.Ordinal)
        {
            "completed",
            "truncated",
            "no-match",
            "rate-limited",
            "backpressure",
            "error",
            "manual-review-pending",
            "not-attempted",
        };

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$")]
    private static partial Regex ContainerNameRegex();

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeSegmentRegex();
}
