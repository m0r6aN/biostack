namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using BioStack.KnowledgeWorker.Workers;
using Xunit;

public sealed class ProtocolIntelligenceEvaluationWorkerTests
{
    [Fact]
    public async Task RunAsync_WritesDeterministicEvaluationResults_ForRequiredProtocolIntelligenceChecks()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"protocol-intelligence-eval-{Guid.NewGuid():N}.json");
        var worker = new ProtocolIntelligenceEvaluationWorker();

        var result = await worker.RunAsync(new ProtocolIntelligenceEvaluationRequest(outputPath));

        Assert.False(result.ShouldFailReleaseGate);
        Assert.True(File.Exists(outputPath));

        var requiredChecks = new[]
        {
            "retrieval_citation_presence",
            "forbidden_output_absence",
            "license_boundary_state",
            "review_gate_state",
            "faers_caveat",
            "clinicaltrials_registry_vs_outcome",
            "wada_stale_source_blocking",
            "retatrutide_investigational_handling",
        };

        foreach (var checkId in requiredChecks)
        {
            var check = Assert.Single(result.Checks, item => item.Id == checkId);
            Assert.True(check.Passed, check.FailureReason);
        }

        using var json = await JsonDocument.ParseAsync(File.OpenRead(outputPath));
        Assert.False(json.RootElement.GetProperty("shouldFailReleaseGate").GetBoolean());
        Assert.Equal(requiredChecks.Length, json.RootElement.GetProperty("checks").GetArrayLength());
    }

    [Fact]
    public async Task RunAndFailOnSafetyCriticalFailureAsync_WritesResultsThenThrows_WhenForbiddenOutputIsDetected()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"protocol-intelligence-eval-fail-{Guid.NewGuid():N}.json");
        var worker = new ProtocolIntelligenceEvaluationWorker();
        var input = ProtocolIntelligenceEvaluationInput.Default with
        {
            RenderedOutput = "you should start this post-cycle therapy",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            worker.RunAndFailOnSafetyCriticalFailureAsync(new ProtocolIntelligenceEvaluationRequest(outputPath, input)));

        Assert.Contains("safety-critical", ex.Message);
        Assert.True(File.Exists(outputPath));

        using var json = await JsonDocument.ParseAsync(File.OpenRead(outputPath));
        Assert.True(json.RootElement.GetProperty("shouldFailReleaseGate").GetBoolean());
        var forbiddenCheck = json.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == "forbidden_output_absence");
        Assert.False(forbiddenCheck.GetProperty("passed").GetBoolean());
        Assert.True(forbiddenCheck.GetProperty("safetyCritical").GetBoolean());
    }
}
