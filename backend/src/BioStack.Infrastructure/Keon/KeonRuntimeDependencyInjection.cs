namespace BioStack.Infrastructure.Keon;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class KeonRuntimeDependencyInjection
{
    public static IServiceCollection AddKeonRuntime(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(KeonRuntimeOptions.SectionName)
            .Get<KeonRuntimeOptions>() ?? new KeonRuntimeOptions();

        services.AddSingleton(options);

        if (options.LiveMode && !string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            services.AddHttpClient(KeonRuntimeClient.HttpClientName, http =>
            {
                http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                http.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMs);
                if (!string.IsNullOrWhiteSpace(options.BearerToken))
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.BearerToken);
            });
            services.AddSingleton<IKeonRuntimeClient, KeonRuntimeClient>();
        }
        else
        {
            services.AddSingleton<IKeonRuntimeClient, KeonRuntimeClientStub>();
        }

        return services;
    }
}
