namespace BioStack.KnowledgeWorker.Tests;

using System.Security.Cryptography;
using System.Text.Json;
using Azure;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class SourceAcquisitionStorageTests
{
    private static readonly DateTimeOffset CompletedAt =
        new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly string IntentId = new('a', 64);

    [Fact]
    public async Task File_retention_is_source_free_tombstones_first_and_is_idempotent()
    {
        using var root = new TemporaryDirectory();
        var attempt = CreateAttempt();
        var path = await WriteAttemptAsync(root.Path, attempt);
        var checkpoint = Path.Combine(
            Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName,
            "checkpoint.json");
        await File.WriteAllTextAsync(checkpoint, "{}");
        var service = new SourceAcquisitionRetentionService(
            new ManualTimeProvider(CompletedAt.AddDays(31)));
        var options = FileOptions(root.Path);

        var first = await service.EnforceAsync(
            options,
            isProduction: false);

        Assert.Equal(1, first.ScannedCount);
        Assert.Equal(1, first.RemovedCount);
        Assert.False(File.Exists(path));
        Assert.False(File.Exists(checkpoint));
        var tombstonePath = Path.Combine(
            Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName,
            "tombstone.json");
        var tombstone =
            JsonSerializer.Deserialize<SourceAcquisitionTombstone>(
                await File.ReadAllBytesAsync(tombstonePath),
                SourceAcquisitionRunner.JsonOptions);
        Assert.NotNull(tombstone);
        Assert.Equal("retention-expired", tombstone.RemovalReason);
        Assert.Equal(Sha256(
            JsonSerializer.SerializeToUtf8Bytes(
                attempt,
                SourceAcquisitionRunner.JsonOptions)),
            tombstone.AttemptSha256);

        var second = await service.EnforceAsync(
            options,
            isProduction: false);
        Assert.Equal(new SourceAcquisitionRetentionResult(0, 0), second);
    }

    [Fact]
    public async Task File_retention_preserves_unexpired_content()
    {
        using var root = new TemporaryDirectory();
        var path = await WriteAttemptAsync(root.Path, CreateAttempt());
        var service = new SourceAcquisitionRetentionService(
            new ManualTimeProvider(CompletedAt.AddDays(29)));

        var result = await service.EnforceAsync(
            FileOptions(root.Path),
            isProduction: false);

        Assert.Equal(new SourceAcquisitionRetentionResult(1, 0), result);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task File_retention_resumes_after_tombstone_before_delete_crash()
    {
        using var root = new TemporaryDirectory();
        var attempt = CreateAttempt();
        var path = await WriteAttemptAsync(root.Path, attempt);
        var attemptBytes = await File.ReadAllBytesAsync(path);
        var intentDirectory =
            Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName;
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
            CompletedAt.AddDays(30).AddMinutes(1),
            "retention-expired");
        await File.WriteAllBytesAsync(
            Path.Combine(intentDirectory, "tombstone.json"),
            JsonSerializer.SerializeToUtf8Bytes(
                tombstone,
                SourceAcquisitionRunner.JsonOptions));
        var service = new SourceAcquisitionRetentionService(
            new ManualTimeProvider(CompletedAt.AddDays(31)));

        var result = await service.EnforceAsync(
            FileOptions(root.Path),
            isProduction: false);

        Assert.Equal(new SourceAcquisitionRetentionResult(1, 1), result);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task File_retention_quarantines_only_content_address_mismatch()
    {
        using var root = new TemporaryDirectory();
        var path = await WriteAttemptAsync(root.Path, CreateAttempt());
        const string collisionBytes = "collision-bytes-must-not-enter-marker";
        await File.WriteAllTextAsync(path, collisionBytes);
        var service = new SourceAcquisitionRetentionService(
            new ManualTimeProvider(CompletedAt.AddDays(31)));

        var result = await service.EnforceAsync(
            FileOptions(root.Path),
            isProduction: false);

        Assert.Equal(new SourceAcquisitionRetentionResult(1, 0, 0, 1), result);
        Assert.False(File.Exists(path));
        var marker = Assert.Single(Directory.EnumerateFiles(
            Path.Combine(root.Path, "source-acquisition", "quarantine"),
            "quarantine-metadata.json",
            SearchOption.AllDirectories));
        Assert.DoesNotContain(
            collisionBytes,
            await File.ReadAllTextAsync(marker),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task File_retention_continues_after_poisoned_item()
    {
        using var root = new TemporaryDirectory();
        var poisonPath = await WriteAttemptAsync(root.Path, CreateAttempt());
        await File.WriteAllTextAsync(poisonPath, "{}");
        var valid = CreateAttempt() with { IntentId = new string('f', 64) };
        var validPath = await WriteAttemptAsync(root.Path, valid);
        var service = new SourceAcquisitionRetentionService(
            new ManualTimeProvider(CompletedAt.AddDays(31)));

        var result = await service.EnforceAsync(
            FileOptions(root.Path),
            isProduction: false);

        Assert.Equal(new SourceAcquisitionRetentionResult(2, 1, 0, 1), result);
        Assert.False(File.Exists(poisonPath));
        Assert.False(File.Exists(validPath));
    }

    [Fact]
    public async Task File_retention_quarantines_crash_resume_replacement()
    {
        using var root = new TemporaryDirectory();
        var attempt = CreateAttempt();
        var path = await WriteAttemptAsync(root.Path, attempt);
        var originalBytes = await File.ReadAllBytesAsync(path);
        var intentDirectory =
            Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName;
        var tombstone = new SourceAcquisitionTombstone(
            SourceAcquisitionRunner.SchemaVersion,
            attempt.CycleId,
            attempt.IntentId,
            attempt.StableOrdinal,
            attempt.SourceId,
            attempt.RequestId,
            attempt.Status,
            Sha256(originalBytes),
            attempt.CompletedAtUtc,
            attempt.RetainUntilUtc,
            CompletedAt.AddDays(31),
            "retention-expired");
        await File.WriteAllBytesAsync(
            Path.Combine(intentDirectory, "tombstone.json"),
            JsonSerializer.SerializeToUtf8Bytes(
                tombstone,
                SourceAcquisitionRunner.JsonOptions));
        await File.WriteAllTextAsync(path, "{\"replacement\":true}");
        var service = new SourceAcquisitionRetentionService(
            new ManualTimeProvider(CompletedAt.AddDays(31)));

        var result = await service.EnforceAsync(
            FileOptions(root.Path),
            isProduction: false);

        Assert.Equal(new SourceAcquisitionRetentionResult(1, 0, 0, 1), result);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(Path.Combine(intentDirectory, "tombstone.json")));
    }

    [Fact]
    public async Task File_retention_rejects_nested_directory_links_when_supported()
    {
        using var root = new TemporaryDirectory();
        using var outside = new TemporaryDirectory();
        var versionRoot = Path.Combine(
            root.Path,
            "source-acquisition",
            "v1");
        Directory.CreateDirectory(versionRoot);
        var link = Path.Combine(versionRoot, "linked-cycle");
        try
        {
            Directory.CreateSymbolicLink(link, outside.Path);
        }
        catch (Exception exception)
            when (exception is UnauthorizedAccessException
                or IOException
                or PlatformNotSupportedException)
        {
            return;
        }
        try
        {
            var service = new SourceAcquisitionRetentionService(
                new ManualTimeProvider(CompletedAt.AddDays(31)));

            var result = await service.EnforceAsync(
                FileOptions(root.Path),
                isProduction: false);

            Assert.Equal(new SourceAcquisitionRetentionResult(0, 0, 1, 0), result);
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Theory]
    [InlineData("File", 30, 30)]
    [InlineData("AzureBlob", 29, 30)]
    [InlineData("AzureBlob", 30, 31)]
    public void Production_retention_is_exactly_blob_30_30(
        string provider,
        int candidateDays,
        int receiptDays)
    {
        var options = ValidBlobOptions();
        options.SourceAcquisitionStorageProvider = provider;
        options.SourceAcquisitionCandidateRetentionDays = candidateDays;
        options.SourceAcquisitionReceiptRetentionDays = receiptDays;

        Assert.Throws<InvalidOperationException>(() =>
            SourceAcquisitionRetentionService.ValidatePolicy(
                options,
                isProduction: true));
    }

    [Fact]
    public void Retention_mode_is_database_free()
    {
        Assert.True(WorkerRunModePolicy.IsDatabaseFree(
            RunMode.SourceAcquisitionRetention));
    }

    [Fact]
    public void Blob_policy_accepts_only_fixed_https_managed_identity_scope()
    {
        var valid = ValidBlobConfiguration();
        AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(valid);

        Assert.Throws<InvalidOperationException>(() =>
            AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
                valid with { BlobServiceUri = "http://acct.blob.core.windows.net" }));
        Assert.Throws<InvalidOperationException>(() =>
            AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
                valid with { BlobServiceUri = "https://acct.blob.core.windows.net/path" }));
        Assert.Throws<InvalidOperationException>(() =>
            AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
                valid with { BlobContainerName = "Bad_Name" }));
        Assert.Throws<InvalidOperationException>(() =>
            AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
                valid with { BlobPrefix = "../escape" }));
        AzureBlobSourceAcquisitionArtifactStore.ValidateConfiguration(
            valid with { ManagedIdentityClientId = null });
    }

    [Fact]
    public void Blob_lease_contract_is_60_seconds_renewed_every_20_seconds()
    {
        Assert.Equal(60, AzureBlobSourceAcquisitionArtifactStore.LeaseSeconds);
        Assert.Equal(
            20,
            AzureBlobSourceAcquisitionArtifactStore.LeaseRenewalSeconds);
    }

    [Fact]
    public void Blob_read_contract_rejects_oversize_and_etag_races()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SourceAcquisitionRetentionService.ValidateBlobContentLength(
                SourceAcquisitionRuntimeLimits.MaximumAttemptBytes + 1L,
                SourceAcquisitionRuntimeLimits.MaximumAttemptBytes));
        Assert.Throws<InvalidOperationException>(() =>
            SourceAcquisitionRetentionService.ValidateStableBlobRead(
                new ETag("\"before\""),
                new ETag("\"after\""),
                expectedLength: 10,
                actualLength: 10));
        Assert.Throws<InvalidOperationException>(() =>
            SourceAcquisitionRetentionService.ValidateStableBlobRead(
                new ETag("\"same\""),
                new ETag("\"same\""),
                expectedLength: 10,
                actualLength: 9));
    }

    private static WorkerOptions FileOptions(string root) =>
        new()
        {
            ResearchOutputDirectory = root,
            SourceAcquisitionStorageProvider = "File",
            SourceAcquisitionCandidateRetentionDays = 30,
            SourceAcquisitionReceiptRetentionDays = 30,
        };

    private static WorkerOptions ValidBlobOptions() =>
        new()
        {
            ResearchOutputDirectory = "unused",
            SourceAcquisitionStorageProvider = "AzureBlob",
            SourceAcquisitionCandidateRetentionDays = 30,
            SourceAcquisitionReceiptRetentionDays = 30,
            SourceAcquisitionBlobServiceUri =
                "https://acct.blob.core.windows.net",
            SourceAcquisitionBlobContainerName = "source-artifacts",
            SourceAcquisitionBlobPrefix = "source-acquisition",
            SourceAcquisitionManagedIdentityClientId =
                "00000000-0000-0000-0000-000000000001",
        };

    private static SourceAcquisitionRuntimeConfiguration
        ValidBlobConfiguration() =>
        new(
            "unused",
            "cycle-1",
            30,
            30,
            "AzureBlob",
            "https://acct.blob.core.windows.net",
            "source-artifacts",
            "source-acquisition",
            "00000000-0000-0000-0000-000000000001",
            IsProduction: true);

    private static SourceAcquisitionAttemptArtifact CreateAttempt() =>
        new(
            SourceAcquisitionRunner.SchemaVersion,
            "cycle-1",
            IntentId,
            1,
            "fda",
            "fda-planning-v1",
            "request-1",
            "Example",
            "api",
            new string('b', 64),
            new SourceAcquisitionInputBindings(
                new string('c', 64),
                new string('d', 64),
                new string('e', 64)),
            ["Example"],
            ["identity"],
            ["sourceItemId"],
            "no-match",
            CompletedAt,
            CompletedAt.AddDays(30),
            false,
            null,
            null,
            null,
            null,
            []);

    private static async Task<string> WriteAttemptAsync(
        string root,
        SourceAcquisitionAttemptArtifact attempt)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            attempt,
            SourceAcquisitionRunner.JsonOptions);
        var path = Path.Combine(
            root,
            "source-acquisition",
            "v1",
            attempt.CycleId,
            "intents",
            attempt.IntentId,
            "attempts",
            $"{Sha256(bytes)}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class ManualTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"biostack-storage-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
