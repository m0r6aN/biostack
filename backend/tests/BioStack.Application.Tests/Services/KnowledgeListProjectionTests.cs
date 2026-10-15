namespace BioStack.Application.Tests.Services;

using System.Data;
using System.Data.Common;
using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

public sealed class KnowledgeListProjectionTests : IDisposable
{
    private static readonly string[] PublicColumns =
    [
        "Id",
        "CanonicalName",
        "Aliases",
        "Classification",
        "RegulatoryStatus",
        "MechanismSummary",
        "EvidenceTier",
        "SourceReferences",
        "Notes",
        "Pathways",
        "Benefits",
        "AvoidWith",
        "DrugInteractions",
    ];

    private static readonly string[] ExcludedColumns =
    [
        "RecommendedDosage",
        "Frequency",
        "PreferredTimeOfDay",
        "StandardDosageRange",
        "MaxReportedDose",
        "WeeklyDosageSchedule",
        "IncrementalEscalationSteps",
        "TieredDosing",
        "PairsWellWith",
        "CompatibleBlends",
        "VialCompatibility",
        "OptimizationProtein",
        "OptimizationCarbs",
        "OptimizationSupplements",
        "OptimizationSleep",
        "OptimizationExercise",
    ];

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SelectCommandCapture _capture = new();

    public KnowledgeListProjectionTests()
    {
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task GetAllCompoundsAsync_CallsListPublicCompoundsAsync_AndReturnsStoredIntegerElevenAsAminoAcid()
    {
        var source = new Mock<IKnowledgeSource>();
        source
            .Setup(service => service.ListPublicCompoundsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<KnowledgeEntry>
            {
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-00000000000b"),
                    CanonicalName = "Glycine",
                    Classification = (CompoundCategory)11,
                },
            });

        var result = Assert.Single(await new KnowledgeService(source.Object).GetAllCompoundsAsync());

        Assert.Equal(CompoundCategory.AminoAcid, result.Classification);
        Assert.Equal("AminoAcid", result.Classification.ToString());
        source.Verify(service => service.ListPublicCompoundsAsync(It.IsAny<CancellationToken>()), Times.Once);
        source.Verify(service => service.GetAllCompoundsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DatabaseProjection_SelectsPublicColumns_AndReadsStoredIntegerElevenAsAminoAcid()
    {
        var id = Guid.NewGuid();
        await using (var seed = CreateContext(captureSql: false))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.KnowledgeEntries.Add(StoredEntry(id));
            await seed.SaveChangesAsync();
        }

        await using (var verify = CreateContext(captureSql: false))
        {
            Assert.Equal(11, await ReadClassificationAsync(verify));
        }

        await using var context = CreateContext(captureSql: true);
        var source = new DatabaseKnowledgeSource(context);
        var listed = Assert.Single(await source.ListPublicCompoundsAsync());

        Assert.Equal(id, listed.Id);
        Assert.Equal("Glycine", listed.CanonicalName);
        Assert.Equal(new[] { "Gly" }, listed.Aliases);
        Assert.Equal(CompoundCategory.AminoAcid, listed.Classification);
        Assert.Equal(11, (int)listed.Classification);
        Assert.Equal("Research", listed.RegulatoryStatus);
        Assert.Equal("Amino acid mechanism", listed.MechanismSummary);
        Assert.Equal(EvidenceTier.Moderate, listed.EvidenceTier);
        Assert.Equal(new[] { "Source A", "Source B" }, listed.SourceReferences);
        Assert.Equal("Notes", listed.Notes);
        Assert.Equal(new[] { "glycine-pathway" }, listed.Pathways);
        Assert.Equal(new[] { "Observed benefit" }, listed.Benefits);
        Assert.Equal(new[] { "Reported caution" }, listed.AvoidWith);
        Assert.Equal(new[] { "Observed interaction" }, listed.DrugInteractions);
        AssertExcludedColumnsAreDefault(listed);

        var listSql = Assert.Single(_capture.Commands);
        var selectClause = listSql[..listSql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase)];
        foreach (var column in PublicColumns)
        {
            Assert.Contains($"\"{column}\"", selectClause, StringComparison.Ordinal);
        }

        foreach (var column in ExcludedColumns)
        {
            Assert.DoesNotContain(column, selectClause, StringComparison.Ordinal);
        }

        var full = Assert.Single(await source.GetAllCompoundsAsync());
        Assert.Equal("250 mcg", full.RecommendedDosage);
        Assert.Equal("Daily", full.Frequency);
        Assert.Equal("Morning", full.PreferredTimeOfDay);
        Assert.Equal("250-500 mcg", full.StandardDosageRange);
        Assert.Equal("500 mcg", full.MaxReportedDose);
        Assert.Equal(new[] { "Week 1" }, full.WeeklyDosageSchedule);
        Assert.Equal(new[] { "Step" }, full.IncrementalEscalationSteps);
        Assert.NotNull(full.TieredDosing);
        Assert.Equal(new[] { "Should not load" }, full.PairsWellWith);
        Assert.Equal(new[] { "Blend" }, full.CompatibleBlends);
        Assert.Equal("Vial", full.VialCompatibility);
        Assert.Equal("Protein", full.OptimizationProtein);
        Assert.Equal("Carbs", full.OptimizationCarbs);
        Assert.Equal(new[] { "Supplement" }, full.OptimizationSupplements);
        Assert.Equal("Sleep", full.OptimizationSleep);
        Assert.Equal("Exercise", full.OptimizationExercise);
    }

    [Fact]
    public async Task LocalListPublicCompoundsAsync_ReturnsPublicFields_AndLeavesFullReadIntact()
    {
        var source = new LocalKnowledgeSource();

        var listed = await source.ListPublicCompoundsAsync();
        var full = await source.GetAllCompoundsAsync();
        var listedBpc = Assert.Single(listed, entry => entry.CanonicalName == "BPC-157");
        var fullBpc = Assert.Single(full, entry => entry.CanonicalName == "BPC-157");

        Assert.Equal(fullBpc.Id, listedBpc.Id);
        Assert.Equal(fullBpc.Classification, listedBpc.Classification);
        Assert.Equal(fullBpc.MechanismSummary, listedBpc.MechanismSummary);
        Assert.Equal(fullBpc.RegulatoryStatus, listedBpc.RegulatoryStatus);
        Assert.Equal(fullBpc.EvidenceTier, listedBpc.EvidenceTier);
        Assert.Equal(fullBpc.SourceReferences, listedBpc.SourceReferences);
        Assert.Equal(fullBpc.Pathways, listedBpc.Pathways);
        Assert.Equal(fullBpc.Benefits, listedBpc.Benefits);
        Assert.Equal(fullBpc.AvoidWith, listedBpc.AvoidWith);
        Assert.Equal(fullBpc.DrugInteractions, listedBpc.DrugInteractions);
        Assert.Equal("250-500 mcg", fullBpc.RecommendedDosage);
        AssertExcludedColumnsAreDefault(listedBpc);
    }

    private BioStackDbContext CreateContext(bool captureSql)
    {
        var builder = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection);
        if (captureSql)
        {
            builder.AddInterceptors(_capture);
        }

        return new BioStackDbContext(builder.Options);
    }

    private static KnowledgeEntry StoredEntry(Guid id)
    {
        return new KnowledgeEntry
        {
            Id = id,
            CanonicalName = "Glycine",
            Aliases = new List<string> { "Gly" },
            Classification = CompoundCategory.AminoAcid,
            RegulatoryStatus = "Research",
            MechanismSummary = "Amino acid mechanism",
            EvidenceTier = EvidenceTier.Moderate,
            SourceReferences = new List<string> { "Source A", "Source B" },
            Notes = "Notes",
            Pathways = new List<string> { "glycine-pathway" },
            Benefits = new List<string> { "Observed benefit" },
            PairsWellWith = new List<string> { "Should not load" },
            AvoidWith = new List<string> { "Reported caution" },
            CompatibleBlends = new List<string> { "Blend" },
            VialCompatibility = "Vial",
            RecommendedDosage = "250 mcg",
            StandardDosageRange = "250-500 mcg",
            MaxReportedDose = "500 mcg",
            Frequency = "Daily",
            PreferredTimeOfDay = "Morning",
            WeeklyDosageSchedule = new List<string> { "Week 1" },
            IncrementalEscalationSteps = new List<string> { "Step" },
            TieredDosing = new TieredDosingData { Beginner = new DoseTier { StartDose = "1 mg" } },
            DrugInteractions = new List<string> { "Observed interaction" },
            OptimizationProtein = "Protein",
            OptimizationCarbs = "Carbs",
            OptimizationSupplements = new List<string> { "Supplement" },
            OptimizationSleep = "Sleep",
            OptimizationExercise = "Exercise",
        };
    }

    private static void AssertExcludedColumnsAreDefault(KnowledgeEntry entry)
    {
        Assert.Empty(entry.RecommendedDosage);
        Assert.Empty(entry.Frequency);
        Assert.Empty(entry.PreferredTimeOfDay);
        Assert.Empty(entry.StandardDosageRange);
        Assert.Empty(entry.MaxReportedDose);
        Assert.Empty(entry.WeeklyDosageSchedule);
        Assert.Empty(entry.IncrementalEscalationSteps);
        Assert.Null(entry.TieredDosing);
        Assert.Empty(entry.PairsWellWith);
        Assert.Empty(entry.CompatibleBlends);
        Assert.Empty(entry.VialCompatibility);
        Assert.Empty(entry.OptimizationProtein);
        Assert.Empty(entry.OptimizationCarbs);
        Assert.Empty(entry.OptimizationSupplements);
        Assert.Empty(entry.OptimizationSleep);
        Assert.Empty(entry.OptimizationExercise);
    }

    private static async Task<int> ReadClassificationAsync(BioStackDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Classification FROM KnowledgeEntries";
            var value = await command.ExecuteScalarAsync();
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private sealed class SelectCommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = new();

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Record(DbCommand command)
        {
            if (command.CommandText.Contains("KnowledgeEntries", StringComparison.Ordinal)
                && command.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                Commands.Add(command.CommandText);
            }
        }
    }
}
