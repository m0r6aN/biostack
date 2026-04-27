namespace BioStack.Cognition;

using BioStack.Cognition.CollectiveApi;
using Keon.Collective;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;

public static class CognitionServiceCollectionExtensions
{
    /// <summary>
    /// Registers BioStack Cognition services.
    /// Call <c>services.AddCognitiveDensity()</c> first to register the orchestrator.
    /// </summary>
    public static IServiceCollection AddBioStackCognition(this IServiceCollection services)
    {
        services.AddSingleton<IStackDeliberationTranslator, StackDeliberationTranslator>();
        services.AddSingleton<IStackReviewBoardService, StackReviewBoardService>();
        return services;
    }

    /// <summary>
    /// Unified Cognition + Collective integration registration.
    ///
    /// Live mode (KeonCollective:LiveMode = true AND ControlBaseUrl set):
    ///   Registers <see cref="CollectiveLiveOrchestrator"/> which calls
    ///   Keon Control /api/collective/live-runs.
    ///
    /// Stub/degraded mode (default):
    ///   Registers the rule-based <see cref="CognitiveDensityOrchestrator"/> adapter shim.
    /// </summary>
    public static IServiceCollection AddCollectiveIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new CollectiveApiOptions();
        configuration.GetSection(CollectiveApiOptions.SectionName).Bind(options);

        services.AddSingleton(options);

        if (options.LiveMode && !string.IsNullOrWhiteSpace(options.ControlBaseUrl))
        {
            // Live path: register a named HttpClient so ASP.NET Core's handler pool
            // manages socket lifecycle and DNS rotation (avoids socket exhaustion).
            // CollectiveLiveOrchestrator borrows from the pool on each RunAsync call.
            services.AddHttpClient(CollectiveLiveOrchestrator.HttpClientName, http =>
            {
                http.BaseAddress = new Uri(options.ControlBaseUrl.TrimEnd('/') + "/");
                http.Timeout     = TimeSpan.FromMilliseconds(options.TimeoutMs);
            });

            services.AddSingleton<ICognitiveDensityOrchestrator>(sp =>
                new CollectiveLiveOrchestrator(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    options,
                    sp.GetRequiredService<ILogger<CollectiveLiveOrchestrator>>()));
        }
        else
        {
            // Stub/degraded path: rule-based adapter shim
            services.AddCognitiveDensity();
        }

        return services.AddBioStackCognition();
    }
}
