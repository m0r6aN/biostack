namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class MarketInterestCandidateUniverseTests
{
    private static string RepositoryRoot => Directory.GetParent(TestPaths.BackendRoot())!.FullName;
    private static string CandidatePath => Path.Combine(RepositoryRoot, "research", "input", "candidates", "peptide-serm-sarm-market-interest.v1.json");
    private static string RequestPath => Path.Combine(RepositoryRoot, "research", "research-requests", "market-interest-coverage-2026-07-24.v1.json");

    [Fact]
    public void Candidate_And_Request_Artifacts_Are_Schema_Valid_And_One_To_One()
    {
        var loader = new ResearchArtifactLoader();
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());
        var candidate = loader.Load(ResearchArtifactKind.CompoundCandidateBatch, CandidatePath);
        var request = loader.Load(ResearchArtifactKind.ResearchRequestBatch, RequestPath);

        var candidateResult = validator.Validate(ResearchArtifactKind.CompoundCandidateBatch, candidate.Node);
        var requestResult = validator.Validate(ResearchArtifactKind.ResearchRequestBatch, request.Node);

        Assert.True(candidateResult.IsValid, candidateResult.Summary());
        Assert.True(requestResult.IsValid, requestResult.Summary());

        var candidates = candidate.Node["candidates"]!.AsArray();
        var requests = request.Node["requests"]!.AsArray();
        Assert.Equal(70, candidates.Count);
        Assert.Equal(70, requests.Count);

        var candidateNames = candidates.Select(x => x!["canonicalNameCandidate"]!.GetValue<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requestNames = requests.Select(x => x!["compoundName"]!.GetValue<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(70, candidateNames.Count);
        Assert.True(candidateNames.SetEquals(requestNames));

        foreach (var item in candidates)
        {
            var flags = item!["reviewFlags"]!.AsArray().Select(x => x!.GetValue<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("source-registry-pending", flags);
            Assert.Contains("human-review-required", flags);
        }

        foreach (var name in new[] { "Semaglutide", "BPC-157", "Emideltide", "Enclomiphene", "Ostarine", "Ibutamoren" })
        {
            Assert.Contains(name, candidateNames);
        }
    }

    [Fact]
    public void Canonical_Names_And_Aliases_Are_Unambiguous_And_Adjacent_Compounds_Are_Not_Sarms()
    {
        var candidate = new ResearchArtifactLoader().Load(ResearchArtifactKind.CompoundCandidateBatch, CandidatePath);
        var candidates = candidate.Node["candidates"]!.AsArray();
        var aliasOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in candidates)
        {
            var name = item!["canonicalNameCandidate"]!.GetValue<string>();
            foreach (var alias in item["aliases"]!.AsArray().Select(x => x!.GetValue<string>()))
            {
                Assert.True(aliasOwners.TryAdd(alias.Trim(), name), $"Duplicate alias '{alias}' owned by '{aliasOwners[alias.Trim()]}' and '{name}'.");
            }
        }

        foreach (var name in new[] { "Ibutamoren", "Cardarine", "Stenabolic" })
        {
            var item = candidates.Single(x => x!["canonicalNameCandidate"]!.GetValue<string>().Equals(name, StringComparison.OrdinalIgnoreCase))!;
            Assert.NotEqual("SARM", item["classification"]!.GetValue<string>());
            Assert.Contains(item["reviewFlags"]!.AsArray(), x => x!.GetValue<string>() == "commercially-misclassified-as-sarm");
        }
    }
}
