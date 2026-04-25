namespace BioStack.Application.Services;

using BioStack.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class AnalyzerPrewarmService : BackgroundService
{
    private static readonly string[] SeedInputs =
    [
        "BPC-157 500mcg daily; TB-500 2mg twice weekly",
        "Semaglutide weekly",
        "Tirzepatide weekly",
        "Retatrutide weekly",
        "NAD+ 100mg 2x weekly; MOTS-C 5mg 3x weekly",
        "CJC-1295 300mcg nightly; Ipamorelin 300mcg nightly"
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnalyzerPrewarmService> _logger;

    public AnalyzerPrewarmService(IServiceProvider serviceProvider, ILogger<AnalyzerPrewarmService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var analyzer = scope.ServiceProvider.GetRequiredService<IProtocolAnalyzerService>();

        foreach (var input in SeedInputs)
        {
            try
            {
                await analyzer.AnalyzeAsync(new AnalyzeProtocolRequest(input), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Analyzer prewarm failed for seed input.");
            }
        }
    }
}
