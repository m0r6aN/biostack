namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// F3: the Governed Spine must be tamper-EVIDENT, not merely duplicate-resistant.
///
/// Before this, append-only was enforced by an application-layer existence check plus a unique
/// index on ReceiptUri. Neither detects an out-of-band UPDATE or DELETE — and in a local-first
/// product the database file sits on the holder's own disk. These tests tamper with the ledger
/// directly, the way someone with a SQLite browser would, and assert the chain notices.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SpineChainIntegrityTests : IDisposable
{
    private readonly BioStackDbContext _db;
    private readonly SpineRepository _sut;

    public SpineChainIntegrityTests()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source=file:spine-chain-{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        _db = new BioStackDbContext(options);
        _db.Database.EnsureCreated();
        _sut = SpineTestHelpers.CreateRepository(_db);
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
        TimestampUtc = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc),
        Decision = "commentary-only",
        ReceiptClass = "safety.warning.surfaced",
        PolicyHashValue = "keon-policy-v1",
        PolicyHashVersion = "1.0.0",
        InputHash = "sha256:abc",
        EvidenceRefsJson = "[\"compound:creatine\"]",
        EffectStatus = "non-effecting",
        CreatedAt = new DateTime(2026, 8, 2, 12, 0, 1, DateTimeKind.Utc),
    };

    private async Task<List<SpineEntry>> SeedChainAsync(int count)
    {
        var appended = new List<SpineEntry>();
        for (var i = 0; i < count; i++)
            appended.Add(await _sut.AppendAsync(MakeEntry($"keon://receipt/chain-{i:D3}")));
        return appended;
    }

    [Fact]
    public async Task Genesis_entry_starts_the_chain()
    {
        var entry = await _sut.AppendAsync(MakeEntry("keon://receipt/genesis"));

        Assert.Equal(SpineChain.GenesisSequenceNumber, entry.SequenceNumber);
        Assert.Equal(SpineChain.GenesisPreviousHash, entry.PreviousEntryHash);
        Assert.StartsWith("sha256:", entry.EntryHash);
    }

    [Fact]
    public async Task Each_entry_commits_to_its_predecessor()
    {
        var entries = await SeedChainAsync(4);

        for (var i = 1; i < entries.Count; i++)
        {
            Assert.Equal((long)i, entries[i].SequenceNumber);
            Assert.Equal(entries[i - 1].EntryHash, entries[i].PreviousEntryHash);
        }
    }

    [Fact]
    public async Task Intact_chain_verifies()
    {
        await SeedChainAsync(5);

        var result = await _sut.VerifyChainAsync();

        Assert.True(result.IsIntact, result.Reason);
        Assert.Equal(5L, result.EntriesVerified);
    }

    [Fact]
    public async Task Empty_chain_verifies()
    {
        var result = await _sut.VerifyChainAsync();

        Assert.True(result.IsIntact);
        Assert.Equal(0L, result.EntriesVerified);
    }

    [Fact]
    public async Task Altering_a_recorded_decision_breaks_the_chain()
    {
        await SeedChainAsync(4);

        // The tamper an audit is meant to catch: quietly rewrite what was decided.
        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE SpineEntries SET Decision = 'allowed' WHERE SequenceNumber = 1;");

        var result = await _sut.VerifyChainAsync();

        Assert.False(result.IsIntact);
        Assert.Equal("keon://receipt/chain-001", result.FirstBrokenReceiptUri);
        Assert.Contains("altered", result.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Altering_evidence_refs_breaks_the_chain()
    {
        await SeedChainAsync(3);

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE SpineEntries SET EvidenceRefsJson = '[]' WHERE SequenceNumber = 0;");

        var result = await _sut.VerifyChainAsync();

        Assert.False(result.IsIntact);
        Assert.Equal("keon://receipt/chain-000", result.FirstBrokenReceiptUri);
    }

    [Fact]
    public async Task Deleting_a_middle_entry_breaks_the_chain()
    {
        await SeedChainAsync(5);

        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM SpineEntries WHERE SequenceNumber = 2;");

        var result = await _sut.VerifyChainAsync();

        Assert.False(result.IsIntact);
        // Entry 3 now sits where 2 should be, so the gap is reported there.
        Assert.Equal("keon://receipt/chain-003", result.FirstBrokenReceiptUri);
        Assert.Contains("Sequence gap", result.Reason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Verification_reports_the_earliest_break()
    {
        await SeedChainAsync(5);

        await _db.Database.ExecuteSqlRawAsync(
            "UPDATE SpineEntries SET ActorId = 'someone-else' WHERE SequenceNumber IN (1, 3);");

        var result = await _sut.VerifyChainAsync();

        Assert.False(result.IsIntact);
        Assert.Equal("keon://receipt/chain-001", result.FirstBrokenReceiptUri);
        Assert.Equal(1L, result.EntriesVerified);
    }

    [Fact]
    public async Task Duplicate_receipt_is_still_rejected()
    {
        await _sut.AppendAsync(MakeEntry("keon://receipt/dupe"));

        await Assert.ThrowsAsync<SpineImmutabilityViolationException>(
            () => _sut.AppendAsync(MakeEntry("keon://receipt/dupe")));
    }

    [Fact]
    public async Task Chain_survives_a_rejected_duplicate()
    {
        await SeedChainAsync(3);

        await Assert.ThrowsAsync<SpineImmutabilityViolationException>(
            () => _sut.AppendAsync(MakeEntry("keon://receipt/chain-001")));

        var result = await _sut.VerifyChainAsync();

        Assert.True(result.IsIntact, result.Reason);
        Assert.Equal(3L, result.EntriesVerified);
    }

    [Fact]
    public void Hash_is_sensitive_to_the_predecessor()
    {
        var entry = MakeEntry("keon://receipt/x");

        var a = SpineChain.ComputeEntryHash(entry, SpineChain.GenesisPreviousHash);
        var b = SpineChain.ComputeEntryHash(entry, "sha256:something-else");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Field_boundaries_cannot_be_forged()
    {
        // Length-prefixing means content shifted across a field boundary hashes differently,
        // instead of producing an identical concatenation ("a"+"bc" vs "ab"+"c").
        // Every other field — including Id and both timestamps — is pinned, so the boundary
        // is genuinely the only variable.
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var stamp = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

        SpineEntry Build(string receiptUri, string subjectUri) => new()
        {
            Id = id,
            ReceiptUri = receiptUri,
            SubjectUri = subjectUri,
            TenantId = "t",
            ActorId = "x",
            TimestampUtc = stamp,
            Decision = "d",
            ReceiptClass = "c",
            PolicyHashValue = "p",
            PolicyHashVersion = "v",
            InputHash = "i",
            EvidenceRefsJson = "[]",
            EffectStatus = "e",
            CreatedAt = stamp,
            SequenceNumber = 0,
            PreviousEntryHash = SpineChain.GenesisPreviousHash,
        };

        var one = SpineChain.ComputeEntryHash(Build("a", "bc"), SpineChain.GenesisPreviousHash);
        var two = SpineChain.ComputeEntryHash(Build("ab", "c"), SpineChain.GenesisPreviousHash);

        Assert.NotEqual(one, two);
    }

    [Fact]
    public void Identical_input_hashes_identically()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var stamp = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);

        SpineEntry Build() => new()
        {
            Id = id,
            ReceiptUri = "keon://receipt/same",
            SubjectUri = "s",
            TenantId = "t",
            ActorId = "x",
            TimestampUtc = stamp,
            Decision = "d",
            ReceiptClass = "c",
            PolicyHashValue = "p",
            PolicyHashVersion = "v",
            InputHash = "i",
            EvidenceRefsJson = "[]",
            EffectStatus = "e",
            CreatedAt = stamp,
            SequenceNumber = 3,
            PreviousEntryHash = "sha256:prev",
        };

        Assert.Equal(
            SpineChain.ComputeEntryHash(Build(), "sha256:prev"),
            SpineChain.ComputeEntryHash(Build(), "sha256:prev"));
    }
}
