namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// F3+: signed chain-head checkpoints detect whole-chain rewrites that pure rehashing would miss
/// when the holder rewrites consistently without the signing key.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SpineCheckpointTests : IDisposable
{
    private readonly BioStackDbContext _db;
    private readonly SpineRepository _spine;
    private readonly SpineCheckpointService _checkpoints;

    public SpineCheckpointTests()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source=file:spine-cp-{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        _db = new BioStackDbContext(options);
        _db.Database.EnsureCreated();
        (_spine, _checkpoints) = SpineTestHelpers.CreateWithCheckpoints(_db);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private static SpineEntry MakeEntry(string receiptUri) => new()
    {
        ReceiptUri = receiptUri,
        SubjectUri = "biostack://srb/proto-001",
        TenantId = "tenant-a",
        ActorId = "actor-a",
        TimestampUtc = new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc),
        Decision = "commentary-only",
        ReceiptClass = "safety.warning.surfaced",
        PolicyHashValue = "keon-policy-v1",
        PolicyHashVersion = "1.0.0",
        InputHash = "sha256:abc",
        EvidenceRefsJson = "[]",
        EffectStatus = "non-effecting",
        CreatedAt = new DateTime(2026, 8, 3, 12, 0, 1, DateTimeKind.Utc),
    };

    [Fact]
    public async Task Signed_checkpoint_verifies_against_intact_chain()
    {
        await _spine.AppendAsync(MakeEntry("keon://receipt/cp-0"));
        await _spine.AppendAsync(MakeEntry("keon://receipt/cp-1"));

        var checkpoint = await _checkpoints.CreateCheckpointAsync("test");
        Assert.Equal(SpineChain.CheckpointSourceLocalHmac, checkpoint.Source);
        Assert.StartsWith("sha256:", checkpoint.Signature);

        var result = await _checkpoints.VerifyLatestAsync();
        Assert.True(result.ChainIntact);
        Assert.True(result.CheckpointPresent);
        Assert.True(result.HeadMatchesCheckpoint);
        Assert.True(result.SignatureValid);
        Assert.True(result.ExternallyAnchored);
        Assert.True(result.IsFullyValid);
    }

    [Fact]
    public async Task Rewriting_entry_after_checkpoint_fails_head_match()
    {
        await _spine.AppendAsync(MakeEntry("keon://receipt/cp-a"));
        await _spine.AppendAsync(MakeEntry("keon://receipt/cp-b"));
        await _checkpoints.CreateCheckpointAsync();

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE SpineEntries SET Decision = 'allowed' WHERE SequenceNumber = 1;");

        var result = await _checkpoints.VerifyLatestAsync();
        Assert.False(result.ChainIntact);
        Assert.False(result.IsFullyValid);
    }

    [Fact]
    public async Task Signature_fails_when_signing_key_differs()
    {
        await _spine.AppendAsync(MakeEntry("keon://receipt/cp-key"));
        await _checkpoints.CreateCheckpointAsync();

        // Rebuild service with a different key against the same DB.
        var (_, other) = SpineTestHelpers.CreateWithCheckpoints(
            _db,
            new SpineCheckpointOptions
            {
                SigningKey = "a-completely-different-key-value",
                AutoCheckpointEveryNEntries = 0,
                CadenceMinutes = 0,
            });

        var result = await other.VerifyLatestAsync();
        Assert.True(result.ChainIntact);
        Assert.True(result.CheckpointPresent);
        Assert.True(result.HeadMatchesCheckpoint);
        Assert.True(result.ExternallyAnchored);
        Assert.False(result.SignatureValid);
        Assert.False(result.IsFullyValid);
    }

    [Fact]
    public async Task Export_manifest_is_portable_json()
    {
        await _spine.AppendAsync(MakeEntry("keon://receipt/cp-export"));
        await _checkpoints.CreateCheckpointAsync("export-me");

        var json = await _checkpoints.ExportLatestManifestJsonAsync();
        Assert.NotNull(json);
        Assert.Contains("biostack.spine-chain-checkpoint.v1", json);
        Assert.Contains("headEntryHash", json);
        Assert.Contains("signature", json);
    }

    [Fact]
    public async Task Domain_sign_and_verify_round_trip()
    {
        var key = System.Text.Encoding.UTF8.GetBytes("round-trip-key");
        var at = new DateTime(2026, 8, 3, 15, 30, 0, DateTimeKind.Utc);
        var payload = SpineChain.BuildCheckpointPayload(3, "sha256:deadbeef", at);
        var sig = SpineChain.SignCheckpointPayload(payload, key);

        Assert.True(SpineChain.VerifyCheckpointSignature(3, "sha256:deadbeef", at, sig, key));
        Assert.False(SpineChain.VerifyCheckpointSignature(4, "sha256:deadbeef", at, sig, key));
    }

    [Fact]
    public async Task Auto_checkpoint_fires_every_n_entries()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source=file:spine-auto-{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        await using var db = new BioStackDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var (spine, checkpoints) = SpineTestHelpers.CreateWithCheckpoints(
            db,
            new SpineCheckpointOptions
            {
                SigningKey = "auto-key",
                AutoCheckpointEveryNEntries = 2,
                CadenceMinutes = 0,
            });

        await spine.AppendAsync(MakeEntry("keon://receipt/auto-0"));
        Assert.Null(await checkpoints.GetLatestAsync());

        await spine.AppendAsync(MakeEntry("keon://receipt/auto-1"));
        var latest = await checkpoints.GetLatestAsync();
        Assert.NotNull(latest);
        Assert.Equal(1, latest.SequenceNumber);
        Assert.Contains("auto-every-2", latest.Note);
    }
}
