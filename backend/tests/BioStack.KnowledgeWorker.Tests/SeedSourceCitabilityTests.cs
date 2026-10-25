namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Source replacements 2026-10-25: every seed source must be publicly citable, so nothing is silently hidden from readers.</summary>
public sealed class SeedSourceCitabilityTests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    [Fact]
    public void Every_seed_provenance_source_is_public_citable()
    {
        var hidden = new List<string>();
        foreach (var record in JsonNode.Parse(File.ReadAllText(SeedPath))!.AsArray())
        {
            var slug = record!["identity"]!["slug"]!.GetValue<string>();
            foreach (var source in record["provenance"]!["sourceRecords"]!.AsArray())
            {
                var url = source!["url"]?.GetValue<string>();
                var publisher = source["publisher"]?.GetValue<string>();
                if (!PublicSourcePolicy.IsPublicCitable(url, publisher)) hidden.Add($"{slug}: {url}");
            }
        }
        Assert.Empty(hidden);
    }
}
