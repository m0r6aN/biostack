namespace Keon.Collective;

using Microsoft.Extensions.DependencyInjection;

public static class CognitiveDensityServiceCollectionExtensions
{
    /// <summary>
    /// Registers the keon.collective cognitive-density orchestrator.
    /// In v1 this wires the rule-based stub. Swap to the LLM-backed
    /// implementation once the Collective runtime is available.
    /// </summary>
    public static IServiceCollection AddCognitiveDensity(this IServiceCollection services)
    {
        services.AddSingleton<ICognitiveDensityOrchestrator, CognitiveDensityOrchestrator>();
        return services;
    }
}
