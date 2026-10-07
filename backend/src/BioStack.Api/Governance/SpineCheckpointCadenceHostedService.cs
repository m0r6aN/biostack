namespace BioStack.Api.Governance;

using BioStack.Infrastructure.Governance;
using Microsoft.Extensions.Options;

/// <summary>
/// F3+: periodic chain-head checkpoint when the ledger has advanced (cadence).
/// </summary>
public sealed class SpineCheckpointCadenceHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<SpineCheckpointOptions> options,
    ILogger<SpineCheckpointCadenceHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = options.Value.CadenceMinutes;
        if (minutes <= 0)
        {
            logger.LogInformation("Spine checkpoint cadence is disabled (CadenceMinutes <= 0).");
            return;
        }

        var delay = TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 24 * 60));
        logger.LogInformation("Spine checkpoint cadence every {Minutes} minute(s).", delay.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var checkpoints = scope.ServiceProvider.GetRequiredService<ISpineCheckpointService>();
                var created = await checkpoints.CheckpointIfAdvancedAsync(stoppingToken);
                if (created is not null)
                {
                    logger.LogInformation(
                        "Cadence checkpoint {Id} at sequence {Sequence}",
                        created.Id, created.SequenceNumber);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Spine checkpoint cadence tick failed.");
            }
        }
    }
}
