namespace BioStack.Infrastructure.Keon;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BioStack.Infrastructure.Governance;

public static class KeonRuntimeDependencyInjection
{
    /// <param name="isProduction">
    /// When true, a stubbed (ungoverned) Keon runtime fails startup unless
    /// <see cref="KeonRuntimeOptions.AllowStubInProduction"/> is explicitly set. This turns
    /// "silently serving production traffic without a governance runtime" into a boot failure.
    /// </param>
    public static IServiceCollection AddKeonRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isProduction = false)
    {
        var options = configuration
            .GetSection(KeonRuntimeOptions.SectionName)
            .Get<KeonRuntimeOptions>() ?? new KeonRuntimeOptions();

        var isLive = options.LiveMode && !string.IsNullOrWhiteSpace(options.BaseUrl);

        if (isProduction && !isLive && !options.AllowStubInProduction)
        {
            throw new InvalidOperationException(
                "KeonRuntime is not in live mode (LiveMode=false or BaseUrl empty) in a Production "
                + "environment. Governance receipts cannot be anchored. Configure "
                + "KeonRuntime:BaseUrl + KeonRuntime:LiveMode=true, or set "
                + "KeonRuntime:AllowStubInProduction=true to acknowledge running ungoverned.");
        }

        if (isProduction && options.StubAllowAll)
        {
            throw new InvalidOperationException(
                "KeonRuntime:StubAllowAll=true is not permitted in Production — it bypasses "
                + "fail-closed policy checks.");
        }

        services.AddSingleton(options);

        if (isLive)
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

        services.AddScoped<ISpineRepository, SpineRepository>();
        services.AddScoped<IRuntimeReceiptFactory, RuntimeReceiptFactory>();

        // F3+: signed chain-head checkpoints (signing key must not live in the Spine DB).
        // Cadence hosted worker is registered in the API host (needs Microsoft.Extensions.Hosting).
        services.Configure<SpineCheckpointOptions>(
            configuration.GetSection(SpineCheckpointOptions.SectionName));
        services.AddScoped<ISpineCheckpointService, SpineCheckpointService>();

        return services;
    }
}
