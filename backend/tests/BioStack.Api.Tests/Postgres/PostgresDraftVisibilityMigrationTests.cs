namespace BioStack.Api.Tests.Postgres;

using BioStack.Api;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using BioStack.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

/// <summary>
/// Owner ruling t101u: the IsPublic migration hides the auto-discovered drafts that already exist (it must take
/// effect on deploy), and nothing else. Run against a real Postgres because the data step is a Postgres-only
/// UPDATE over the serialized Details JSON.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PostgresDraftVisibilityMigrationTests
{
    private readonly PostgresServerFixture _server;

    public PostgresDraftVisibilityMigrationTests(PostgresServerFixture server) => _server = server;

    private static KnowledgeEntry Entry(string name, string family, string review, DateTime? retired = null)
        => new()
        {
            Id = Guid.NewGuid(),
            CanonicalName = name,
            RetiredAtUtc = retired,
            Details = new KnowledgeEntryDetails { CompoundFamily = family, ReviewStatus = review },
        };

    [PostgresFact]
    public async Task The_Data_Step_Hides_Only_Unreviewed_Auto_Discovered_Drafts_And_Is_Reversible()
    {
        var connectionString = await _server.CreateDatabaseAsync();
        try
        {
            var options = new DbContextOptionsBuilder<BioStackDbContext>().UseNpgsql(connectionString);
            ProductionMigrationBaselineConfiguration.Configure(options);
            await using var db = new BioStackDbContext(options.Options);
            await db.Database.MigrateAsync();

            // The column exists, NOT NULL, default true: rows written without a value are public.
            db.KnowledgeEntries.AddRange(
                Entry("Rituximab", "unknown (auto-discovered: biologic)", "pending-review"),
                Entry("blood sampling", "unknown (auto-discovered: incretin)", "pending-review"),
                Entry("Approved Draft", "unknown (auto-discovered: peptide)", "ai-council-reviewed"),
                Entry("Human Reviewed Draft", "unknown (auto-discovered: peptide)", "reviewed"),
                Entry("ER-100", "Epigenetic Therapy", "pending-review"),       // curated, pending review: stays public
                Entry("Semaglutide", "GLP-1 receptor agonist", "reviewed"),
                Entry("Retired Draft", "unknown (auto-discovered: peptide)", "pending-review", retired: DateTime.UtcNow));
            db.KnowledgeEntries.Add(new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = "No Details Row" });
            await db.SaveChangesAsync();
            Assert.All(await db.KnowledgeEntries.AsNoTracking().ToListAsync(), e => Assert.True(e.IsPublic));

            await db.Database.ExecuteSqlRawAsync(AddKnowledgeEntryIsPublic.HideUnreviewedDraftsSql);

            var hidden = await db.KnowledgeEntries.AsNoTracking().Where(e => !e.IsPublic).Select(e => e.CanonicalName).OrderBy(n => n).ToListAsync();
            Assert.Equal(new[] { "Rituximab", "blood sampling" }, hidden);

            // Reversible: one UPDATE restores today's behaviour.
            await db.Database.ExecuteSqlRawAsync("UPDATE \"KnowledgeEntries\" SET \"IsPublic\" = TRUE");
            Assert.Empty(await db.KnowledgeEntries.AsNoTracking().Where(e => !e.IsPublic).ToListAsync());
        }
        finally
        {
            await _server.DropDatabaseAsync(connectionString);
        }
    }

    [PostgresFact]
    public async Task The_Migration_Adds_A_Not_Null_Boolean_That_Defaults_To_Public()
    {
        var connectionString = await _server.CreateDatabaseAsync();
        try
        {
            var options = new DbContextOptionsBuilder<BioStackDbContext>().UseNpgsql(connectionString);
            ProductionMigrationBaselineConfiguration.Configure(options);
            await using (var db = new BioStackDbContext(options.Options))
            {
                await db.Database.MigrateAsync();
                Assert.Contains("20261030120000_AddKnowledgeEntryIsPublic", await db.Database.GetAppliedMigrationsAsync());
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = 'KnowledgeEntries' AND column_name = 'IsPublic'", connection);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("boolean", reader.GetString(0));
            Assert.Equal("NO", reader.GetString(1));
            Assert.Equal("true", reader.GetString(2));
        }
        finally
        {
            await _server.DropDatabaseAsync(connectionString);
        }
    }
}
