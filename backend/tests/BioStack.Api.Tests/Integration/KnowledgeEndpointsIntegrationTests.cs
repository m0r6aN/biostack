namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Text.Json;
using BioStack.Api;
using BioStack.Infrastructure.Knowledge;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "Integration")]
public class KnowledgeEndpointsIntegrationTests : IAsyncLifetime
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
    public async Task GetAllCompounds_ReturnsOkWithSeededKnowledge()
    {
        var response = await _client.GetAsync("/api/v1/knowledge/compounds");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0);
    }
}
