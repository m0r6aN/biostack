namespace BioStack.Api;

using BioStack.Application.Services;
using BioStack.Infrastructure.Repositories;

/// <summary>
/// Runs the internal catalog match for stored lookups in the background so the request that created one returns at once.
/// New lookups arrive through <see cref="ICatalogLookupQueue"/>; a periodic sweep also picks up any that were never queued
/// (queue full, restart, or a failed run), so a pending lookup is never stuck. Single replica by design (minReplicas 1);
/// processing is idempotent, so a second replica would only repeat free, local work.
/// </summary>
public sealed class CatalogLookupHostedService : BackgroundService
{
    private static readonly TimeSpan SweepEvery = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SweepMinAge = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly ICatalogLookupQueue _queue;
    private readonly ILogger<CatalogLookupHostedService> _logger;

    public CatalogLookupHostedService(IServiceScopeFactory scopes, ICatalogLookupQueue queue, ILogger<CatalogLookupHostedService> logger)
    {
        _scopes = scopes;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sweep = Task.Run(() => SweepLoopAsync(stoppingToken), stoppingToken);
        try
        {
            await foreach (var id in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessOneAsync(id, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }

        await Task.WhenAny(sweep, Task.Delay(TimeSpan.FromSeconds(1)));
    }

    private async Task SweepLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(SweepEvery);
        try
        {
            do
            {
                await SweepOnceAsync(ct);
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task SweepOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var lookups = scope.ServiceProvider.GetRequiredService<ICatalogLookupRepository>();
            var ids = await lookups.ListPendingIdsAsync(DateTime.UtcNow - SweepMinAge, 100, ct);
            foreach (var id in ids)
            {
                await ProcessOneAsync(id, ct);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog lookup sweep failed; it will run again.");
        }
    }

    private async Task ProcessOneAsync(Guid id, CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ICatalogLookupProcessor>().ProcessAsync(id, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Left pending: the sweep retries it. Nothing about the person or the typed name is logged.
            _logger.LogWarning(ex, "Catalog lookup {LookupId} could not be processed; the sweep will retry.", id);
        }
    }
}
