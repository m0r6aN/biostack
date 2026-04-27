namespace BioStack.Api.Tests.Integration;

using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using BioStack.Api;
using BioStack.Infrastructure.Persistence;

[Trait("Category", "Integration")]
public class LeadEndpointsIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CaptureLead_WithValidPayload_ReturnsNoContentAndPersistsNormalizedLead()
    {
        var source = $"reconstitution-calculator-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/v1/leads/capture", new
        {
            Email = "  User@Example.com  ",
            Source = source
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();
        var stored = dbContext.LeadCaptures.Single(lead => lead.Source == source);

        Assert.Equal("user@example.com", stored.Email);
        Assert.Equal(source, stored.Source);
    }

    [Fact]
    public async Task CaptureLead_WithDuplicatePayload_IsIdempotent()
    {
        var source = $"reconstitution-calculator-{Guid.NewGuid():N}";
        var payload = new
        {
            Email = "user@example.com",
            Source = source
        };

        await _client.PostAsJsonAsync("/api/v1/leads/capture", payload);
        var second = await _client.PostAsJsonAsync("/api/v1/leads/capture", payload);

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BioStackDbContext>();

        Assert.Equal(1, dbContext.LeadCaptures.Count(lead => lead.Source == source));
    }

    [Fact]
    public async Task CaptureLead_WithBlankFields_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/leads/capture", new
        {
            Email = "   ",
            Source = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
