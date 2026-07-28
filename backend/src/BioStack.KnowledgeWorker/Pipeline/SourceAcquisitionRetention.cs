namespace BioStack.KnowledgeWorker.Pipeline;

using System.Security.Cryptography;
using System.Text.Json;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BioStack.KnowledgeWorker.Config;

public sealed record SourceAcquisitionRetentionResult(
    int ScannedCount,
    int RemovedCount,
    int FailedCount = 0,
    int QuarantinedCount = 0);

public interface ISourceAcquisitionRetentionService
{
    Task<SourceAcquisitionRetentionResult> EnforceAsync(
        WorkerOptions options,
        bool isProduction,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A source-free retention pass. It reads only artifact metadata/content from the
/// configured artifact store and never resolves acquisition adapters or a database.
/// </summary>
public sealed partial class SourceAcquisitionRetentionService
    : ISourceAcquisitionRetentionService
{
    internal const int MaximumTombstoneBytes = 64 * 1024;

    private readonly TimeProvider _timeProvider;

    public SourceAcquisitionRetentionService()
        : this(TimeProvider.System)
    {
    }

    internal SourceAcquisitionRetentionService(TimeProvider timeProvider)
    {
        _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<SourceAcquisitionRetentionResult> EnforceAsync(
        WorkerOptions options,
        bool isProduction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePolicy(options, isProduction);
        return string.Equals(
            options.SourceAcquisitionStorageProvider,
            "AzureBlob",
            StringComparison.OrdinalIgnoreCase)
            ? EnforceBlobAsync(options, isProduction, cancellationToken)
            : EnforceFileAsync(options, isProduction, cancellationToken);
    }

    internal static void ValidatePolicy(
        WorkerOptions options,
        bool isProduction)
    {
        if (options.SourceAcquisitionCandidateRetentionDays is null or <= 0
            || options.SourceAcquisitionReceiptRetentionDays is null or <= 0)
        {
            throw new InvalidOperationException(
                "Source-acquisition retention requires explicit positive candidate and receipt retention.");
        }
        if (isProduction
            && (options.SourceAcquisitionCandidateRetentionDays != 30
                || options.SourceAcquisitionReceiptRetentionDays != 30
                || !string.Equals(
                    options.SourceAcquisitionStorageProvider,
                    "AzureBlob",
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Production retention requires AzureBlob storage and exact 30/30-day retention.");
        }
        if (!string.Equals(
                options.SourceAcquisitionStorageProvider,
                "File",
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                options.SourceAcquisitionStorageProvider,
                "AzureBlob",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Source-acquisition retention storage provider must be File or AzureBlob.");
        }
    }

    private async Task<SourceAcquisitionRetentionResult> EnforceFileAsync(
        WorkerOptions options,
        bool isProduction,
        CancellationToken cancellationToken)
    {
        var root = ResolveSafeFileRoot(options.ResearchOutputDirectory);
        var versionRoot = Path.Combine(root, "source-acquisition", "v1");
        if (!Directory.Exists(versionRoot))
        {
            return new SourceAcquisitionRetentionResult(0, 0);
        }

        var scanned = 0;
        var removed = 0;
        var failed = 0;
        var quarantined = 0;
        IReadOnlyList<string> attempts;
        try
        {
            attempts = EnumerateAttemptFilesSafely(root, versionRoot);
        }
        catch (InvalidOperationException)
        {
            return new SourceAcquisitionRetentionResult(0, 0, 1, 0);
        }
        foreach (var path in attempts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scanned++;
            try
            {
                EnsureSafeFilePath(root, path);
                var bytes = await ReadFileBoundedAsync(
                    root,
                    path,
                    SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                    cancellationToken);
                var attempt = ParseAndValidateAttempt(
                    bytes,
                    Path.GetFileNameWithoutExtension(path),
                    options,
                    isProduction);
                if (_timeProvider.GetUtcNow() < attempt.RetainUntilUtc) continue;

                var intentDirectory =
                    Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName;
                EnsureSafeFilePath(root, intentDirectory, requireFile: false);
                var tombstonePath =
                    Path.Combine(intentDirectory, "tombstone.json");
                if (File.Exists(tombstonePath))
                {
                    try
                    {
                        var tombstoneBytes = await ReadFileBoundedAsync(
                            root,
                            tombstonePath,
                            MaximumTombstoneBytes,
                            cancellationToken);
                        ValidateExistingTombstone(
                            tombstoneBytes,
                            attempt,
                            bytes);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        if (await QuarantineFileAsync(
                                root,
                                tombstonePath,
                                attempt.IntentId,
                                "retention-tombstone-integrity-failed",
                                cancellationToken))
                        {
                            quarantined++;
                        }
                        else
                        {
                            failed++;
                        }
                        continue;
                    }
                }
                else
                {
                    var tombstoneBytes = TombstoneBytes(
                        attempt,
                        bytes,
                        _timeProvider.GetUtcNow());
                    await WriteFileImmutableAsync(
                        root,
                        tombstonePath,
                        tombstoneBytes,
                        cancellationToken);
                }

                // Tombstone is durable before content removal. Re-check every
                // component immediately before deleting the exact artifact.
                EnsureSafeFilePath(root, path);
                File.Delete(path);
                removed++;
                var checkpoint = Path.Combine(intentDirectory, "checkpoint.json");
                if (File.Exists(checkpoint))
                {
                    try
                    {
                        EnsureSafeFilePath(root, checkpoint);
                        File.Delete(checkpoint);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        if (await QuarantineFileAsync(
                                root,
                                checkpoint,
                                attempt.IntentId,
                                "retention-checkpoint-integrity-failed",
                                cancellationToken))
                        {
                            quarantined++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                var intentId = TryGetIntentId(path);
                if (await QuarantineFileAsync(
                        root,
                        path,
                        intentId,
                        "retention-attempt-integrity-failed",
                        cancellationToken))
                {
                    quarantined++;
                }
                else
                {
                    failed++;
                }
            }
        }
        return new SourceAcquisitionRetentionResult(
            scanned,
            removed,
            failed,
            quarantined);
    }

    private async Task<SourceAcquisitionRetentionResult> EnforceBlobAsync(
        WorkerOptions options,
        bool isProduction,
        CancellationToken cancellationToken)
    {
        var configuration = new SourceAcquisitionRuntimeConfiguration(
            options.ResearchOutputDirectory,
            "retention-validation",
            options.SourceAcquisitionCandidateRetentionDays!.Value,
            options.SourceAcquisitionReceiptRetentionDays!.Value,
            options.SourceAcquisitionStorageProvider,
            options.SourceAcquisitionBlobServiceUri,
            options.SourceAcquisitionBlobContainerName,
            options.SourceAcquisitionBlobPrefix,
            options.SourceAcquisitionManagedIdentityClientId,
            isProduction);
        AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
            configuration);

        var credential = string.IsNullOrWhiteSpace(
            options.SourceAcquisitionManagedIdentityClientId)
            ? new ManagedIdentityCredential(
                ManagedIdentityId.SystemAssigned)
            : new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(
                    options.SourceAcquisitionManagedIdentityClientId));
        var clientOptions = new BlobClientOptions();
        clientOptions.Retry.MaxRetries = 0;
        clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);
        var container = new BlobServiceClient(
                new Uri(options.SourceAcquisitionBlobServiceUri!),
                credential,
                clientOptions)
            .GetBlobContainerClient(
                options.SourceAcquisitionBlobContainerName);
        var prefix =
            $"{options.SourceAcquisitionBlobPrefix.Trim('/')}/v1/";
        var scanned = 0;
        var removed = 0;
        var failed = 0;
        var quarantined = 0;
        await foreach (var item in container.GetBlobsAsync(
                           traits: BlobTraits.None,
                           states: BlobStates.None,
                           prefix: prefix,
                           cancellationToken: cancellationToken))
        {
            if (!AttemptBlobRegex().IsMatch(item.Name[prefix.Length..]))
            {
                continue;
            }
            scanned++;
            var blob = container.GetBlobClient(item.Name);
            try
            {
                var itemEtag = item.Properties.ETag
                               ?? throw new InvalidOperationException(
                                   "A retention candidate has no ETag.");
                var itemLength = item.Properties.ContentLength
                                 ?? throw new InvalidOperationException(
                                     "A retention candidate has no content length.");
                var bytes = await ReadBlobBoundedAsync(
                    blob,
                    itemEtag,
                    itemLength,
                    SourceAcquisitionRuntimeLimits.MaximumAttemptBytes,
                    cancellationToken);
                var filenameHash = item.Name.Split('/')[^1][..^5];
                var attempt = ParseAndValidateAttempt(
                    bytes,
                    filenameHash,
                    options,
                    isProduction);
                if (_timeProvider.GetUtcNow() < attempt.RetainUntilUtc) continue;

                var intentPrefix =
                    item.Name[..item.Name.LastIndexOf(
                        "/attempts/",
                        StringComparison.Ordinal)];
                var tombstone = container.GetBlobClient(
                    $"{intentPrefix}/tombstone.json");
                if (await TryGetBlobPropertiesAsync(
                        tombstone,
                        cancellationToken) is { } tombstoneProperties)
                {
                    try
                    {
                        var tombstoneBytes = await ReadBlobBoundedAsync(
                            tombstone,
                            tombstoneProperties.ETag,
                            tombstoneProperties.ContentLength,
                            MaximumTombstoneBytes,
                            cancellationToken);
                        ValidateExistingTombstone(
                            tombstoneBytes,
                            attempt,
                            bytes);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        if (await QuarantineBlobAsync(
                                container,
                                prefix,
                                new RetentionBlob(
                                    tombstone.Name,
                                    tombstoneProperties.ETag),
                                "retention-tombstone-integrity-failed",
                                cancellationToken))
                        {
                            quarantined++;
                        }
                        else
                        {
                            failed++;
                        }
                        continue;
                    }
                }
                else
                {
                    var tombstoneBytes = TombstoneBytes(
                        attempt,
                        bytes,
                        _timeProvider.GetUtcNow());
                    await WriteBlobImmutableAsync(
                        tombstone,
                        tombstoneBytes,
                        cancellationToken);
                }

                // Tombstone-before-delete, and every delete is conditional on
                // the same ETag used by the hard-capped read.
                await DeleteBlobIfMatchAsync(
                    blob,
                    itemEtag,
                    cancellationToken);
                removed++;
                var checkpoint = container.GetBlobClient(
                    $"{intentPrefix}/checkpoint.json");
                if (await TryGetBlobPropertiesAsync(
                        checkpoint,
                        cancellationToken) is { } checkpointProperties)
                {
                    try
                    {
                        await DeleteBlobIfMatchAsync(
                            checkpoint,
                            checkpointProperties.ETag,
                            cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        if (await QuarantineBlobAsync(
                                container,
                                prefix,
                                new RetentionBlob(
                                    checkpoint.Name,
                                    checkpointProperties.ETag),
                                "retention-checkpoint-integrity-failed",
                                cancellationToken))
                        {
                            quarantined++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                var etag = item.Properties.ETag;
                if (etag is not null
                    && await QuarantineBlobAsync(
                        container,
                        prefix,
                        new RetentionBlob(item.Name, etag.Value),
                        "retention-attempt-integrity-failed",
                        cancellationToken))
                {
                    quarantined++;
                }
                else
                {
                    failed++;
                }
            }
        }
        return new SourceAcquisitionRetentionResult(
            scanned,
            removed,
            failed,
            quarantined);
    }

    private static SourceAcquisitionAttemptArtifact ParseAndValidateAttempt(
        byte[] bytes,
        string filenameSha256,
        WorkerOptions options,
        bool isProduction)
    {
        if (!SourceAcquisitionRunner.IsSha256(filenameSha256)
            || filenameSha256 != Sha256(bytes))
        {
            throw new InvalidOperationException(
                "A retention candidate failed its immutable content-address check.");
        }
        var attempt =
            JsonSerializer.Deserialize<SourceAcquisitionAttemptArtifact>(
                bytes,
                SourceAcquisitionRunner.JsonOptions)
            ?? throw new InvalidOperationException(
                "A retention candidate is invalid.");
        var retentionDays = attempt.Status is "completed" or "truncated"
            ? options.SourceAcquisitionCandidateRetentionDays!.Value
            : options.SourceAcquisitionReceiptRetentionDays!.Value;
        if (attempt.SchemaVersion != SourceAcquisitionRunner.SchemaVersion
            || !SourceAcquisitionRunner.IsSha256(attempt.IntentId)
            || attempt.CompletedAtUtc == default
            || attempt.CompletedAtUtc.Offset != TimeSpan.Zero
            || attempt.RetainUntilUtc
            != attempt.CompletedAtUtc.AddDays(retentionDays)
            || attempt.RetainUntilUtc.Offset != TimeSpan.Zero
            || (isProduction && retentionDays != 30))
        {
            throw new InvalidOperationException(
                "A retention candidate failed its retention-boundary check.");
        }
        return attempt;
    }

    private static byte[] TombstoneBytes(
        SourceAcquisitionAttemptArtifact attempt,
        byte[] attemptBytes,
        DateTimeOffset removedAtUtc)
    {
        if (removedAtUtc < attempt.RetainUntilUtc
            || removedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Retention cannot remove content before its UTC retention boundary.");
        }
        return JsonSerializer.SerializeToUtf8Bytes(
            new SourceAcquisitionTombstone(
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
                "retention-expired"),
            SourceAcquisitionRunner.JsonOptions);
    }

    private static void ValidateExistingTombstone(
        byte[] tombstoneBytes,
        SourceAcquisitionAttemptArtifact attempt,
        byte[] attemptBytes)
    {
        var tombstone =
            JsonSerializer.Deserialize<SourceAcquisitionTombstone>(
                tombstoneBytes,
                SourceAcquisitionRunner.JsonOptions)
            ?? throw new InvalidOperationException(
                "An existing retention tombstone is invalid.");
        if (tombstone.SchemaVersion != SourceAcquisitionRunner.SchemaVersion
            || tombstone.CycleId != attempt.CycleId
            || tombstone.IntentId != attempt.IntentId
            || tombstone.StableOrdinal != attempt.StableOrdinal
            || tombstone.SourceId != attempt.SourceId
            || tombstone.RequestId != attempt.RequestId
            || tombstone.OriginalStatus != attempt.Status
            || tombstone.AttemptSha256 != Sha256(attemptBytes)
            || tombstone.CompletedAtUtc != attempt.CompletedAtUtc
            || tombstone.RetainUntilUtc != attempt.RetainUntilUtc
            || tombstone.RemovedAtUtc < attempt.RetainUntilUtc
            || tombstone.RemovedAtUtc.Offset != TimeSpan.Zero
            || tombstone.RemovalReason != "retention-expired")
        {
            throw new InvalidOperationException(
                "An existing retention tombstone does not match the immutable attempt.");
        }
    }

    private static async Task WriteBlobImmutableAsync(
        BlobClient blob,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        try
        {
            await blob.UploadAsync(
                BinaryData.FromBytes(bytes),
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "application/json",
                    },
                    Conditions = new BlobRequestConditions
                    {
                        IfNoneMatch = ETag.All,
                    },
                },
                cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            var properties = await blob.GetPropertiesAsync(
                cancellationToken: cancellationToken);
            var existing = await ReadBlobBoundedAsync(
                blob,
                properties.Value.ETag,
                properties.Value.ContentLength,
                MaximumTombstoneBytes,
                cancellationToken);
            if (existing.AsSpan().SequenceEqual(bytes)) return;
            throw new InvalidOperationException(
                "An immutable retention tombstone exists with different content.",
                exception);
        }
    }

    private static async Task WriteFileImmutableAsync(
        string root,
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        EnsureSafeFilePath(root, Path.GetDirectoryName(path)!, requireFile: false);
        if (File.Exists(path))
        {
            var existing = await ReadFileBoundedAsync(
                root,
                path,
                MaximumTombstoneBytes,
                cancellationToken);
            if (existing.AsSpan().SequenceEqual(bytes)) return;
            throw new InvalidOperationException(
                "An immutable retention tombstone exists with different content.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        EnsureSafeFilePath(root, Path.GetDirectoryName(path)!, requireFile: false);
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        stream.Flush(flushToDisk: true);
    }

    private static async Task<byte[]> ReadFileBoundedAsync(
        string root,
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        EnsureSafeFilePath(root, path);
        var info = new FileInfo(path);
        if (info.Length < 0 || info.Length > maximumBytes)
        {
            throw new InvalidOperationException(
                "A retention artifact exceeded its fixed size limit.");
        }
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var bytes = await ReadStreamBoundedAsync(
            stream,
            maximumBytes,
            cancellationToken);
        EnsureSafeFilePath(root, path);
        if (new FileInfo(path).Length != info.Length)
        {
            throw new InvalidOperationException(
                "A retention artifact changed during its bounded read.");
        }
        return bytes;
    }

    private static async Task<byte[]> ReadBlobBoundedAsync(
        BlobClient blob,
        ETag etag,
        long contentLength,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        ValidateBlobContentLength(contentLength, maximumBytes);
        var response = await blob.DownloadStreamingAsync(
            new BlobDownloadOptions
            {
                Range = new HttpRange(0, maximumBytes + 1L),
                Conditions = new BlobRequestConditions { IfMatch = etag },
            },
            cancellationToken);
        await using var content = response.Value.Content;
        var bytes = await ReadStreamBoundedAsync(
            content,
            maximumBytes,
            cancellationToken);
        ValidateStableBlobRead(
            etag,
            response.Value.Details.ETag,
            contentLength,
            bytes.LongLength);
        return bytes;
    }

    internal static void ValidateBlobContentLength(
        long contentLength,
        int maximumBytes)
    {
        if (contentLength < 0 || contentLength > maximumBytes)
        {
            throw new InvalidOperationException(
                "A retention Blob exceeded its fixed size limit.");
        }
    }

    internal static void ValidateStableBlobRead(
        ETag expectedEtag,
        ETag actualEtag,
        long expectedLength,
        long actualLength)
    {
        if (actualEtag != expectedEtag || actualLength != expectedLength)
        {
            throw new InvalidOperationException(
                "A retention Blob changed during its ETag-stable bounded read.");
        }
    }

    private static async Task<byte[]> ReadStreamBoundedAsync(
        Stream stream,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
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
                    "A retention artifact exceeded its streamed size limit.");
            }
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static async Task<BlobProperties?> TryGetBlobPropertiesAsync(
        BlobClient blob,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await blob.GetPropertiesAsync(
                cancellationToken: cancellationToken)).Value;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    private static async Task DeleteBlobIfMatchAsync(
        BlobClient blob,
        ETag etag,
        CancellationToken cancellationToken)
    {
        await blob.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            new BlobRequestConditions { IfMatch = etag },
            cancellationToken);
    }

    private async Task<bool> QuarantineBlobAsync(
        BlobContainerClient container,
        string versionPrefix,
        RetentionBlob suspect,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var relative = suspect.Name[versionPrefix.Length..];
            var cycleEnd = relative.IndexOf('/');
            if (cycleEnd <= 0)
            {
                throw new InvalidOperationException(
                    "A retention Blob escaped the version prefix.");
            }
            var cycleRoot =
                $"{versionPrefix}{relative[..cycleEnd]}";
            var marker = container.GetBlobClient(
                $"{cycleRoot}/quarantine/retention/" +
                $"{_timeProvider.GetUtcNow():yyyyMMddHHmmssfffffff}-" +
                $"{Guid.NewGuid():N}/quarantine-metadata.json");
            var metadata = JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    schemaVersion = SourceAcquisitionRunner.SchemaVersion,
                    quarantinedAtUtc = _timeProvider.GetUtcNow(),
                    reasonCode,
                    artifactDisposition = "content-free-evidence-only",
                    artifactCount = 1,
                    artifacts = new[] { "000-redacted" },
                },
                SourceAcquisitionRunner.JsonOptions);
            await WriteBlobImmutableAsync(marker, metadata, cancellationToken);
            await DeleteBlobIfMatchAsync(
                container.GetBlobClient(suspect.Name),
                suspect.ETag,
                cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> QuarantineFileAsync(
        string root,
        string suspectPath,
        string intentId,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        try
        {
            EnsureSafeFilePath(root, suspectPath);
            var markerDirectory = Path.Combine(
                root,
                "source-acquisition",
                "quarantine",
                intentId,
                $"{_timeProvider.GetUtcNow():yyyyMMddHHmmssfffffff}-" +
                Guid.NewGuid().ToString("N"));
            EnsureSafeFilePath(
                root,
                Path.GetDirectoryName(markerDirectory)!,
                requireFile: false);
            Directory.CreateDirectory(markerDirectory);
            EnsureSafeFilePath(root, markerDirectory, requireFile: false);
            var markerPath = Path.Combine(
                markerDirectory,
                "quarantine-metadata.json");
            var metadata = JsonSerializer.SerializeToUtf8Bytes(
                new
                {
                    schemaVersion = SourceAcquisitionRunner.SchemaVersion,
                    intentId,
                    quarantinedAtUtc = _timeProvider.GetUtcNow(),
                    reasonCode,
                    artifactDisposition = "content-free-evidence-only",
                    artifactCount = 1,
                    artifacts = new[] { "000-redacted" },
                },
                SourceAcquisitionRunner.JsonOptions);
            await WriteFileImmutableAsync(
                root,
                markerPath,
                metadata,
                cancellationToken);
            EnsureSafeFilePath(root, suspectPath);
            File.Delete(suspectPath);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static IReadOnlyList<string> EnumerateAttemptFilesSafely(
        string root,
        string versionRoot)
    {
        EnsureSafeFilePath(root, versionRoot, requireFile: false);
        var results = new List<string>();
        var pending = new Stack<string>();
        pending.Push(versionRoot);
        while (pending.Count != 0)
        {
            var directory = pending.Pop();
            EnsureSafeFilePath(root, directory, requireFile: false);
            foreach (var child in Directory.EnumerateFileSystemEntries(
                         directory,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                var attributes = File.GetAttributes(child);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new InvalidOperationException(
                        "File retention rejects every nested reparse point.");
                }
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    EnsureSafeFilePath(root, child, requireFile: false);
                    pending.Push(child);
                    continue;
                }
                EnsureSafeFilePath(root, child);
                if (child.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    && child.Contains(
                        $"{Path.DirectorySeparatorChar}attempts" +
                        $"{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal))
                {
                    results.Add(child);
                }
            }
        }
        return results.OrderBy(path => path, StringComparer.Ordinal).ToList();
    }

    private static void EnsureSafeFilePath(
        string root,
        string path,
        bool requireFile = true)
    {
        var canonicalRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(root));
        var canonicalPath = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(canonicalRoot, canonicalPath);
        if (Path.IsPathRooted(relative)
            || relative == ".."
            || relative.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A retention path escaped its canonical output root.");
        }

        var current = canonicalRoot;
        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!Directory.Exists(current) && !File.Exists(current))
            {
                break;
            }
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException(
                    "File retention rejects every nested reparse point.");
            }
        }
        if (requireFile && !File.Exists(canonicalPath))
        {
            throw new InvalidOperationException(
                "A retention artifact disappeared before access.");
        }
    }

    private static string TryGetIntentId(string path)
    {
        var intentDirectory =
            Directory.GetParent(Path.GetDirectoryName(path)!)?.Name;
        return SourceAcquisitionRunner.IsSha256(intentDirectory ?? string.Empty)
            ? intentDirectory!
            : Sha256(System.Text.Encoding.UTF8.GetBytes(path));
    }

    private static string ResolveSafeFileRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "ResearchOutputDirectory is required for file retention.");
        }
        var root = Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path));
        if (!Directory.Exists(root)
            || new DirectoryInfo(root).Attributes.HasFlag(
                FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException(
                "File retention requires an existing non-reparse-point output root.");
        }
        return root;
    }

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private readonly record struct RetentionBlob(string Name, ETag ETag);

    [System.Text.RegularExpressions.GeneratedRegex(
        "^[A-Za-z0-9._-]+/intents/[a-f0-9]{64}/attempts/[a-f0-9]{64}\\.json$")]
    private static partial System.Text.RegularExpressions.Regex AttemptBlobRegex();
}
