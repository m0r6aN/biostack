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

        // Lane H: central user-facing output safety gate + the high-risk category classifier it uses.
        // The high-risk gate is stateless and deterministic (singleton); the output gate issues
        // receipts via the scoped IRuntimeReceiptFactory, so it is registered scoped.
        services.AddSingleton<HighRiskCategoryGate>();
        services.AddScoped<IUserFacingIntelligenceGate, UserFacingIntelligenceGate>();
        return services;
    }
}
