namespace BioStack.Application.ScientificResearch;

using BioStack.Application.Abstractions.ScientificResearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ScientificResearchDependencyInjection
{
    public static IServiceCollection AddScientificResearchProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(ScientificResearchSidecarOptions.SectionName)
            .Get<ScientificResearchSidecarOptions>()
            ?? new ScientificResearchSidecarOptions();

        services.AddSingleton(options);

        if (options.Enabled && !string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            services.AddHttpClient(ScientificResearchSidecarClient.HttpClientName, http =>
            {
                http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                http.Timeout = TimeSpan.FromMilliseconds(Math.Max(1_000, options.TimeoutMs));
                if (!string.IsNullOrWhiteSpace(options.ServiceToken))
                {
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(
                            "Bearer",
                            options.ServiceToken);
                }
            });
            services.AddSingleton<IScientificResearchProvider, ScientificResearchSidecarClient>();
        }
        else
        {
            services.AddSingleton<IScientificResearchProvider, DisabledScientificResearchProvider>();
        }

        services.AddScoped<IScientificResearchCandidateStagingService, ScientificResearchCandidateStagingService>();
        return services;
    }
}
