namespace BioStack.Api.Tests.Integration;

using BioStack.Application.Governance;
using BioStack.Application.Services;
using BioStack.Infrastructure.Governance;
using BioStack.Infrastructure.Keon;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Regression guard for PR #246, where a merge silently dropped governance DI registrations.
///
/// That class of defect does not fail the build and does not fail at startup in Production
/// (ValidateOnBuild defaulted to Development-only) — it surfaces as a 500 on the first request
/// that injects the missing service, i.e. the user discovers the safety gate is gone.
///
/// Program.cs now enables ValidateOnBuild/ValidateScopes in every environment; this test asserts
/// the governance graph actually resolves, so a dropped registration fails in CI instead.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GovernanceDependencyInjectionSmokeTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public GovernanceDependencyInjectionSmokeTests()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"di-smoke-{Guid.NewGuid():N}.db");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Jwt:Secret", "test-secret-key-at-least-32-chars-long!!");
                builder.UseSetting("Jwt:Issuer", "biostack");
                builder.UseSetting("Jwt:Audience", "biostack-ui");
            });
    }

    /// <summary>Every service on the governed output path. Adding a gate? Add it here.</summary>
    public static TheoryData<Type> GovernanceServices =>
    [
        typeof(IUserFacingIntelligenceGate),
        typeof(IRuntimeReceiptFactory),
        typeof(ISpineRepository),
        typeof(IKeonRuntimeClient),
        typeof(IEvidenceGate),
        typeof(PolicyGate),
        typeof(HighRiskCategoryGate),
        typeof(DoctrineSanitizer),
        typeof(KeonRuntimeOptions),
    ];

    [Theory]
    [MemberData(nameof(GovernanceServices))]
    public void Governance_service_resolves_from_the_container(Type serviceType)
    {
        using var scope = _factory.Services.CreateScope();

        var resolved = scope.ServiceProvider.GetService(serviceType);

        Assert.True(
            resolved is not null,
            $"{serviceType.Name} did not resolve. A governance registration is missing from "
            + "Program.cs / AddGovernance / AddKeonRuntime — see PR #246.");
    }

    [Fact]
    public void Host_builds_with_container_validation_enabled()
    {
        // Forcing the host to build exercises ValidateOnBuild across the whole graph:
        // a captive dependency or unregistered constructor argument anywhere fails here.
        Assert.NotNull(_factory.Services);

        using var scope = _factory.Services.CreateScope();
        var gate = scope.ServiceProvider.GetRequiredService<IUserFacingIntelligenceGate>();
        Assert.NotNull(gate);
    }

    public void Dispose() => _factory.Dispose();
}
