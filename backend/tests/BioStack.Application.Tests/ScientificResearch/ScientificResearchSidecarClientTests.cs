namespace BioStack.Application.Tests.ScientificResearch;

using System.Net;
using System.Text;
using BioStack.Application.Abstractions.ScientificResearch;
using BioStack.Application.ScientificResearch;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class ScientificResearchSidecarClientTests
{
    [Fact]
    public async Task SubmitAsync_maps_handle_from_sidecar_json()
    {
        var json = """
            {
              "job_id": "job-1",
              "research_request_id": "req-1",
              "workflow": "resolve_compound_identity",
              "status": "partial",
              "submitted_at_utc": "2026-08-02T12:00:00Z",
              "correlation_id": "corr-1"
            }
            """;
        var client = CreateClient(HttpStatusCode.Accepted, json);
        var handle = await client.SubmitAsync(SampleRequest());

        Assert.Equal("job-1", handle.JobId);
        Assert.Equal("req-1", handle.ResearchRequestId);
        Assert.Equal(ResearchJobStatusCode.Partial, handle.Status);
        Assert.Equal("corr-1", handle.CorrelationId);
    }

    [Fact]
    public async Task SubmitAsync_rejects_non_public_classification()
    {
        var client = CreateClient(HttpStatusCode.Accepted, "{}");
        var request = SampleRequest() with { DataClassification = "user_health" };

        var ex = await Assert.ThrowsAsync<ScientificResearchProviderException>(
            () => client.SubmitAsync(request));
        Assert.Equal("data_classification_rejected", ex.ErrorCode);
    }

    [Fact]
    public async Task GetResultAsync_flattens_nested_provenance()
    {
        var json = """
            {
              "research_artifact_id": "artifact-1",
              "job_id": "job-1",
              "research_request_id": "req-1",
              "provider": "biostack-research-sidecar",
              "provider_version": "0.1.0",
              "workflow": "research_adverse_events",
              "workflow_version": "0.1.0",
              "tooluniverse_version": "1.4.0",
              "status": "partial",
              "partial": true,
              "started_at_utc": "2026-08-02T12:00:00Z",
              "finished_at_utc": "2026-08-02T12:00:01Z",
              "tools_invoked": ["FAERS_count_reactions_by_drug_event"],
              "warnings": ["candidate only"],
              "failure_details": null,
              "execution_device": "cpu",
              "provenance": {
                "scaffold": false,
                "tooluniverse_pin": "1.4.0",
                "approved_skills_for_workflow": ["tooluniverse-adverse-event-detection"]
              }
            }
            """;
        var client = CreateClient(HttpStatusCode.OK, json);
        var artifact = await client.GetResultAsync("job-1");

        Assert.Equal("artifact-1", artifact.ResearchArtifactId);
        Assert.Equal(ResearchJobStatusCode.Partial, artifact.Status);
        Assert.True(artifact.Partial);
        Assert.Contains("FAERS_count_reactions_by_drug_event", artifact.ToolsInvoked);
        Assert.Equal("1.4.0", artifact.Provenance["tooluniverse_pin"]);
        Assert.Equal("false", artifact.Provenance["scaffold"]);
        Assert.Contains("tooluniverse-adverse-event-detection", artifact.Provenance["approved_skills_for_workflow"]);
    }

    [Fact]
    public async Task Disabled_provider_throws_explicitly()
    {
        var provider = new DisabledScientificResearchProvider();
        await Assert.ThrowsAsync<ScientificResearchProviderDisabledException>(
            () => provider.SubmitAsync(SampleRequest()));
    }

    private static ScientificResearchRequest SampleRequest()
        => new(
            ResearchRequestId: "req-1",
            ResearchSubjectType: "compound",
            SubjectName: "aspirin",
            KnownIdentifiers: new Dictionary<string, string>(),
            Workflow: "resolve_compound_identity",
            EvidenceCategories: Array.Empty<string>(),
            SourceAllowlist: Array.Empty<string>(),
            MaximumSourceAgeDays: null,
            MaximumExecutionTime: TimeSpan.FromMinutes(5),
            MaximumSourceCount: 20,
            CorrelationId: "corr-1",
            RequestedByActor: "test",
            Purpose: "unit_test",
            Execution: new ScientificExecutionProfile(
                ScientificExecutionMode.CpuOnly,
                AllowGpu: false,
                AllowCpuFallback: true,
                AllowHostedFallback: false,
                MaximumGpuMemoryBytes: null,
                MaximumExecutionDuration: TimeSpan.FromMinutes(5),
                ApprovedModelProfile: null),
            DataClassification: "public_scientific",
            TaskClass: null,
            EvidenceRiskClass: EvidenceRiskClass.Medium,
            LocalInferencePermitted: true,
            HostedInferencePermitted: false,
            CompressionPermitted: false,
            CrossCheckRequired: false);

    private static ScientificResearchSidecarClient CreateClient(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        var factory = new StubHttpClientFactory(handler);
        var options = new ScientificResearchSidecarOptions
        {
            BaseUrl = "http://sidecar.test",
            Enabled = true,
            TimeoutMs = 5_000,
        };
        return new ScientificResearchSidecarClient(
            factory,
            options,
            NullLogger<ScientificResearchSidecarClient>.Instance);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            var client = new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://sidecar.test/"),
            };
            return client;
        }
    }
}
