namespace BioStack.Application.Governance;

using Microsoft.Extensions.DependencyInjection;

public static class GovernanceDependencyInjection
{
    /// <summary>
    /// Registers application-layer governance services.
    /// Call this after <c>AddKeonRuntime</c> since <see cref="PolicyGate"/>
    /// depends on <see cref="BioStack.Infrastructure.Keon.IKeonRuntimeClient"/>.
    /// </summary>
    public static IServiceCollection AddGovernance(this IServiceCollection services)
    {
        services.AddScoped<PolicyGate>();
        return services;
    }
}
