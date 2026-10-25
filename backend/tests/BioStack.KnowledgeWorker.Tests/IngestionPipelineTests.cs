namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// End-to-end receipts: prove the full load → validate → normalize → trust-gate →
/// canonicalize pipeline accepts the canonical seed, rejects a schema-broken record,
/// and emits accept/reject counts suitable for operator log review.
/// </summary>
public class IngestionPipelineTests
{
    private static readonly string SchemaPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "substance-record.schema.json");

    private static readonly string CanonicalSeedPath =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "substances-seed.json");

    [Fact]
    public void Pipeline_Prepares_Every_Qualifying_Seed_Record_Without_Database()
    {
        var pipeline = BuildPipeline();
        var seedPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));
        var result = pipeline.Run(seedPath);

        Assert.Equal(156, result.Accepted.Count);
        Assert.Empty(result.Rejected);
        Assert.Equal(156, result.Accepted.Select(row => row.Entry.CanonicalName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(result.Accepted, row => Assert.False(string.IsNullOrWhiteSpace(row.Entry.CanonicalName)));
    }

    [Fact]
    public void Pipeline_Carries_Full_Retatrutide_Record_Into_Details()
    {
        var pipeline = BuildPipeline();
        var seedPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));
        var row = Assert.Single(pipeline.Run(seedPath).Accepted, r => r.Entry.CanonicalName == "Retatrutide");
        var details = Assert.IsType<BioStack.Domain.Entities.KnowledgeEntryDetails>(row.Entry.Details);

        Assert.Contains("not FDA-approved", row.Entry.RegulatoryStatus);
        Assert.Equal(9, details.DoseReports.Count(d => d.Side == "study"));
        Assert.Empty(details.DoseReports.Where(d => d.Side == "reported"));   // rendered as n/a
        Assert.All(details.DoseReports, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.SourceUrl));
            Assert.False(string.IsNullOrWhiteSpace(d.Grade));
        });
        Assert.Contains(details.Sources, s => s.Url.Contains("42814954"));
        Assert.Contains(details.Sources, s => s.Url.Contains("NCT05931367"));
        Assert.NotEmpty(details.Safety.Warnings);
        Assert.Contains(details.StudiedUses, u => u.LabelStatus == "not-indicated");
        Assert.DoesNotContain("ClassB", row.Entry.Notes);
    }

    [Fact]
    public void Pipeline_Accepts_Canonical_Seed_Record()
    {
        var pipeline = BuildPipeline();

        var result = pipeline.Run(CanonicalSeedPath);

        Assert.Empty(result.Rejected);
        Assert.Single(result.Accepted);

        var prepared = result.Accepted[0];
        Assert.Equal("Tesamorelin", prepared.Record.Identity.CanonicalName);
        Assert.Equal(TrustClass.A,  prepared.TrustClass);
        Assert.Empty(prepared.StrippedFields);
        Assert.Equal("tesamorelin", prepared.Entry.CanonicalName.ToLowerInvariant());
    }

    [Fact]
    public void Pipeline_Rejects_Record_With_Missing_Required_Field()
    {
        var tempPath = BuildMixedSeed(includeValid: false, includeBroken: true);
        try
        {
            var pipeline = BuildPipeline();
            var result   = pipeline.Run(tempPath);

            Assert.Empty(result.Accepted);
            Assert.Single(result.Rejected);

            var rejected = result.Rejected[0];
            Assert.NotEmpty(rejected.Errors);
        }
        finally { File.Delete(tempPath); }
    }

    [Fact]
    public void Pipeline_Produces_Accept_And_Reject_Counts_For_Mixed_Seed()
    {
        var tempPath = BuildMixedSeed(includeValid: true, includeBroken: true);
        try
        {
            var pipeline = BuildPipeline();
            var result   = pipeline.Run(tempPath);

            Assert.Single(result.Accepted);
            Assert.Single(result.Rejected);
        }
        finally { File.Delete(tempPath); }
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static IngestionPipeline BuildPipeline()
    {
        return new IngestionPipeline(
            loader:        new SubstanceRecordLoader(),
            validator:     SubstanceRecordValidator.LoadFromFile(SchemaPath),
            normalizer:    new SubstanceRecordNormalizer(),
            trustGate:     new TrustGate(),
            canonicalizer: new SubstanceCanonicalizer(),
            logger:        NullLogger<IngestionPipeline>.Instance);
    }

    /// <summary>
    /// Writes a temporary seed file containing any combination of
    /// (valid canonical record) + (deliberately schema-broken record),
    /// returning its absolute path. Caller deletes.
    /// </summary>
    private static string BuildMixedSeed(bool includeValid, bool includeBroken)
    {
        var canonical = JsonNode.Parse(File.ReadAllText(CanonicalSeedPath))!.AsArray();
        var valid     = JsonNode.Parse(canonical[0]!.ToJsonString())!;
        var arr       = new JsonArray();

        if (includeValid)
        {
            arr.Add(valid);
        }

        if (includeBroken)
        {
            var broken = JsonNode.Parse(canonical[0]!.ToJsonString())!;
            broken["identity"]!.AsObject().Remove("canonicalName");
            broken["recordType"] = "not-a-real-record-type";
            arr.Add(broken);
        }

        var path = Path.Combine(
            Path.GetTempPath(),
            $"biostack-seed-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, arr.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        return path;
    }
}
