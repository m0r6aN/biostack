namespace BioStack.Api.Tests;

using BioStack.Application.Services;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class StagedTranscriptCandidateReviewSchemaGuardrailTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public StagedTranscriptCandidateReviewSchemaGuardrailTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task StagedReviewTable_Exists()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='StagedTranscriptCandidateReviews';";
        var result = await cmd.ExecuteScalarAsync();

        Assert.Equal("StagedTranscriptCandidateReviews", result as string);
    }

    [Fact]
    public async Task StagedReviewTable_HasNoKnowledgeEntryIdColumn_AndNoForbiddenColumns()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA table_info('StagedTranscriptCandidateReviews');";
        using var reader = await cmd.ExecuteReaderAsync();

        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        Assert.DoesNotContain("KnowledgeEntryId", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(columns, c => c.Contains("promotion_job", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(columns, c => c.Contains("extraction", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(columns, c => c.Contains("summary", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(columns, c => c.Contains("safety", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(columns, c => c.Contains("medical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StagedReviewTable_HasNoForeignKeyToKnowledgeEntries()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_key_list('StagedTranscriptCandidateReviews');";
        using var reader = await cmd.ExecuteReaderAsync();

        var referencedTables = new List<string>();
        while (await reader.ReadAsync())
        {
            referencedTables.Add(reader.GetString(2));
        }

        Assert.DoesNotContain("KnowledgeEntries", referencedTables, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CanonicalityConstraint_RejectsCanonical()
    {
        using var db = CreateDbContext();
        var store = new StagedTranscriptCandidateReviewStore(db);

        var invalid = TranscriptCandidateReviewRecord.Create(
            artifactId: "transcript-candidate:constraint-canonical",
            canonicality: "non_canonical",
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: "video",
            sourceUrl: "https://example.test/video",
            provider: "fixture",
            isDeterministicFixture: true,
            segmentCount: 1,
            segmentSnapshotSignature: "constraint-canonical",
            sourceMetadata: new Dictionary<string, string>(),
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z") with { Canonicality = "canonical" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpsertAsync(invalid));
    }

    [Fact]
    public async Task ReviewStateConstraint_RejectsUnsupportedLifecycleState()
    {
        using var db = CreateDbContext();
        var store = new StagedTranscriptCandidateReviewStore(db);

        var invalid = TranscriptCandidateReviewRecord.Create(
            artifactId: "transcript-candidate:constraint-state",
            canonicality: "non_canonical",
            reviewState: TranscriptCandidateReviewState.PendingReview,
            sourceType: "video",
            sourceUrl: "https://example.test/video",
            provider: "fixture",
            isDeterministicFixture: true,
            segmentCount: 1,
            segmentSnapshotSignature: "constraint-state",
            sourceMetadata: new Dictionary<string, string>(),
            createdAtUtc: "2026-05-30T00:00:00Z",
            updatedAtUtc: "2026-05-30T00:00:00Z") with { ReviewState = "invalid_state" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.UpsertAsync(invalid));
    }

    [Fact]
    public void MigrationFile_OnlyTouchesStagedCandidatePersistenceSchema()
    {
        var migrationPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "BioStack.Infrastructure", "Persistence", "Migrations",
            "20260530000000_PR8_AddStagedTranscriptCandidateReviewPersistence.cs");

        var content = File.ReadAllText(Path.GetFullPath(migrationPath));

        Assert.Contains("CreateTable", content, StringComparison.Ordinal);
        Assert.Contains("StagedTranscriptCandidateReviews", content, StringComparison.Ordinal);
        Assert.DoesNotContain("KnowledgeEntries", content, StringComparison.Ordinal);
        Assert.DoesNotContain("KnowledgeEntryId", content, StringComparison.Ordinal);
        Assert.DoesNotContain("PromotionWorkflow", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Extraction", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Summary", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Safety", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Medical", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DiRegistration_ResolvesStoreContract()
    {
        // Program.cs is in API composition root; this guardrail keeps check local and non-networked.
        var programPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "BioStack.Api", "Program.cs");

        var content = File.ReadAllText(Path.GetFullPath(programPath));

        Assert.Contains(
            "AddScoped<ITranscriptCandidateReviewStore, StagedTranscriptCandidateReviewStore>()",
            content,
            StringComparison.Ordinal);
    }

    private BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new BioStackDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
