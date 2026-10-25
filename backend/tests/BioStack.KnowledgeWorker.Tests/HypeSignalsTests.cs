namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Hype;
using BioStack.KnowledgeWorker.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>Growth spec: Hype Gap data sources behind config, editor input only.</summary>
public sealed class HypeSignalsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private static string SeedPath => Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json");

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public void No_source_is_created_unless_enabled_with_its_secret()
    {
        var warnings = new List<string>();
        Assert.Empty(HypeSources.Create(new HypeOptions(), () => new HttpClient()));
        var enabledNoKey = new HypeOptions { X = { Enabled = true }, YouTube = { Enabled = true } };
        Assert.Empty(HypeSources.Create(enabledNoKey, () => new HttpClient(), warnings.Add));
        Assert.Equal(2, warnings.Count);
        var configured = new HypeOptions { X = { Enabled = true, BearerToken = "t" }, YouTube = { Enabled = true, ApiKey = "k" } };
        Assert.Equal(["x", "youtube"], HypeSources.Create(configured, () => new HttpClient()).Select(s => s.Name));
    }

    [Fact]
    public async Task X_source_reads_total_recent_count_with_bearer_auth()
    {
        var handler = new StubHandler(_ => Json("{\"meta\":{\"total_tweet_count\":1234}}"));
        var source = new XRecentCountsSource(new HttpClient(handler), new XSourceOptions { BearerToken = "secret" });
        var signal = await source.CountAsync("\"BPC-157\"", Now, 7, CancellationToken.None);
        Assert.Equal(1234, signal.Count);
        var req = Assert.Single(handler.Requests);
        Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
        Assert.StartsWith("https://api.x.com/2/tweets/counts/recent?query=", req.RequestUri!.ToString());
    }

    [Fact]
    public async Task YouTube_source_reads_total_results_and_reports_http_errors()
    {
        var ok = new YouTubeSearchSource(new HttpClient(new StubHandler(_ => Json("{\"pageInfo\":{\"totalResults\":88}}"))), new YouTubeSourceOptions { ApiKey = "k" });
        Assert.Equal(88, (await ok.CountAsync("q", Now, 7, CancellationToken.None)).Count);
        var bad = new YouTubeSearchSource(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden))), new YouTubeSourceOptions { ApiKey = "k" });
        var signal = await bad.CountAsync("q", Now, 7, CancellationToken.None);
        Assert.Null(signal.Count);
        Assert.Equal("HTTP 403", signal.Error);
    }

    [Theory]
    [InlineData(10, "Rare")]
    [InlineData(100, "Niche")]
    [InlineData(1000, "Common")]
    [InlineData(10000, "Everywhere")]
    [InlineData(100000, "Viral")]
    public void Suggested_rubric_level_follows_thresholds(long count, string level) =>
        Assert.Equal(level, HypeSources.SuggestedLevel(count, [50, 500, 5000, 50000]));

    [Fact]
    public async Task Job_is_a_noop_without_sources_and_writes_an_editor_report_with_them()
    {
        var root = Path.Combine(Path.GetTempPath(), "hype-" + Guid.NewGuid().ToString("N"));
        try
        {
            var worker = new WorkerOptions { RunMode = RunMode.HypeSignals, SeedFilePath = SeedPath };
            var options = new HypeOptions { OutputDirectory = root };
            var none = await new HypeSignalsJob(worker, options, []).RunAsync(new IngestionContext(worker, NullLogger.Instance));
            Assert.True(none.Success);
            Assert.False(Directory.Exists(root));

            var x = new XRecentCountsSource(new HttpClient(new StubHandler(_ => Json("{\"meta\":{\"total_tweet_count\":600}}"))), new XSourceOptions { BearerToken = "t" });
            var result = await new HypeSignalsJob(worker, options, [x], () => Now).RunAsync(new IngestionContext(worker, NullLogger.Instance));
            Assert.True(result.Success, result.ErrorMessage);
            var report = JsonNode.Parse(await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(root))))!;
            var compounds = report["compounds"]!.AsArray();
            Assert.Contains(compounds, c => c!["compound"]!.GetValue<string>() == "BPC-157");
            Assert.All(compounds, c => Assert.Equal("Common", c!["suggestedLevel"]!.GetValue<string>()));
            Assert.Contains("Editor input only", report["note"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void HypeSignals_is_a_database_free_run_mode() => Assert.True(WorkerRunModePolicy.IsDatabaseFree(RunMode.HypeSignals));
}
