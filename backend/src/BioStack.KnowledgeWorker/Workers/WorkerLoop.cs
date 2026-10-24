namespace BioStack.KnowledgeWorker.Workers;

/// <summary>
/// Optional in-process schedule (owner ruling 7, 2026-10-24). When <c>Worker:LoopEnabled</c> is
/// true the worker repeats its RunMode every interval instead of exiting after one pass. Use it
/// for an always-on container or local runs; Azure Container App Job cron
/// (infra/azure/knowledge-schedules.bicep) remains the production scheduler.
/// </summary>
public static class WorkerLoop
{
    public static TimeSpan ResolveInterval(int refreshIntervalHours, int? loopIntervalMinutes)
    {
        if (loopIntervalMinutes is > 0) return TimeSpan.FromMinutes(loopIntervalMinutes.Value);
        return TimeSpan.FromHours(Math.Max(1, refreshIntervalHours));
    }

    /// <summary>Runs <paramref name="iteration"/> now, then once per <paramref name="interval"/>,
    /// until cancelled or <paramref name="maxIterations"/> is reached. A failing iteration is
    /// reported and the loop continues. Returns the number of completed iterations.</summary>
    public static async Task<int> RunAsync(
        Func<CancellationToken, Task<bool>> iteration,
        TimeSpan interval,
        Action<int, bool, Exception?> onIteration,
        int? maxIterations = null,
        CancellationToken cancellationToken = default)
    {
        var completed = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            bool ok; Exception? error = null;
            try { ok = await iteration(cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception ex) { ok = false; error = ex; }
            completed++;
            onIteration(completed, ok, error);
            if (maxIterations is not null && completed >= maxIterations) break;
            try { await Task.Delay(interval, cancellationToken); }
            catch (OperationCanceledException) { break; }
        }
        return completed;
    }
}
