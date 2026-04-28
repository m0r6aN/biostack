namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BioStack.Api;
using BioStack.Infrastructure.Knowledge;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "Integration")]
public sealed class AnalyzeEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var knowledgeSource = scope.ServiceProvider.GetRequiredService<IKnowledgeSource>();
        var seedSource = new LocalKnowledgeSource();
        var seedEntries = await seedSource.GetAllCompoundsAsync();

        foreach (var entry in seedEntries)
        {
            await knowledgeSource.UpsertCompoundAsync(entry);
        }
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AnalyzeProtocol_ReturnsStructuredResponse()
    {
        var response = await _client.PostAsJsonAsync("/api/analyze/protocol", new
        {
            inputText = "BPC-157 500mcg daily"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(payload.RootElement.TryGetProperty("protocol", out var protocol));
        Assert.True(payload.RootElement.TryGetProperty("score", out var score));
        Assert.True(payload.RootElement.TryGetProperty("inputType", out var inputType));
        Assert.Equal(JsonValueKind.Array, protocol.ValueKind);
        Assert.True(score.GetInt32() >= 0);
        Assert.Equal("Paste", inputType.GetString());
    }
}
