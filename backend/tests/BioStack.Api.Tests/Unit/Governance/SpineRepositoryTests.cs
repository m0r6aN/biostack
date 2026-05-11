namespace BioStack.Api.Tests.Unit.Governance;

using BioStack.Domain.Governance;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

[Trait("Category", "Unit")]
public class SpineRepositoryTests : IDisposable
{
    private readonly BioStackDbContext _db;
    private readonly SpineRepository _sut;

    public SpineRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source=file:spine-unit-{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        _db = new BioStackDbContext(options);
        _db.Database.EnsureCreated();
        _sut = new SpineRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private static SpineEntry MakeEntry(string receiptUri = "keon://receipt/test-001") =>
        new()
        {
            ReceiptUri = receiptUri,
            SubjectUri = "biostack://srb/proto-001",
            TenantId = "tenant-a",
            ActorId = "actor-a",
            TimestampUtc = DateTime.UtcNow,
            Decision = "commentary-only",
            PolicyHashValue = "abc123",
            PolicyHashVersion = "v1",
            InputHash = "sha256:deadbeef",
            EvidenceRefsJson = "[]",
            EffectStatus = "commentary-only",
        };

    [Fact]
    public async Task AppendAsync_FirstTime_ReturnsEntry()
    {
        var entry = MakeEntry();
        var result = await _sut.AppendAsync(entry);

        Assert.Equal(entry.ReceiptUri, result.ReceiptUri);
        Assert.Equal(entry.SubjectUri, result.SubjectUri);
    }

    [Fact]
    public async Task AppendAsync_DuplicateReceiptUri_ThrowsSpineImmutabilityViolationException()
    {
        var entry = MakeEntry("keon://receipt/dup-test");
        await _sut.AppendAsync(entry);

        var duplicate = MakeEntry("keon://receipt/dup-test");

        var ex = await Assert.ThrowsAsync<SpineImmutabilityViolationException>(
            () => _sut.AppendAsync(duplicate));

        Assert.Contains("keon://receipt/dup-test", ex.Message);
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task GetByReceiptUriAsync_ExistingEntry_ReturnsEntry()
    {
        var entry = MakeEntry("keon://receipt/get-test");
        await _sut.AppendAsync(entry);

        var result = await _sut.GetByReceiptUriAsync("keon://receipt/get-test");

        Assert.NotNull(result);
        Assert.Equal("keon://receipt/get-test", result.ReceiptUri);
    }

    [Fact]
    public async Task GetByReceiptUriAsync_UnknownUri_ReturnsNull()
    {
        var result = await _sut.GetByReceiptUriAsync("keon://receipt/does-not-exist");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySubjectAsync_ReturnsAllEntriesForSubject()
    {
        await _sut.AppendAsync(MakeEntry("keon://receipt/sub-001"));
        await _sut.AppendAsync(MakeEntry("keon://receipt/sub-002"));
        // Different subject — should not appear
        await _sut.AppendAsync(new SpineEntry
        {
            ReceiptUri = "keon://receipt/other-sub",
            SubjectUri = "biostack://srb/other",
            TenantId = "t",
            ActorId = "a",
            TimestampUtc = DateTime.UtcNow,
            Decision = "commentary-only",
            PolicyHashValue = "x",
            PolicyHashVersion = "v1",
            InputHash = "y",
            EffectStatus = "commentary-only",
        });

        var results = await _sut.GetBySubjectAsync("biostack://srb/proto-001");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("biostack://srb/proto-001", r.SubjectUri));
    }
}
