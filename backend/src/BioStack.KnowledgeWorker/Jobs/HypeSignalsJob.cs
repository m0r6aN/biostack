namespace BioStack.KnowledgeWorker.Jobs;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Hype;
using Microsoft.Extensions.Logging;

public interface IHypeSignalsJob : IIngestionJob { }

/// <summary>
/// RunMode=HypeSignals (DB-free). For each seed compound with public claims, asks every
/// configured hype source for a public-volume count of the compound's name, then writes
/// research/hype-signals/hype-signals-{yyyyMMdd}.json for editors. It never edits claim
/// records: hype scores stay editorial (02 §5) and publish through the AI council.
/// </summary>
public sealed class HypeSignalsJob(WorkerOptions worker, HypeOptions options, IReadOnlyList<IHypeSignalSource> sources, Func<DateTimeOffset>? clock = null) : IHypeSignalsJob
{
    public async Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        if (sources.Count == 0)
        {
            context.Logger.LogInformation("[HypeSignals] No hype sources configured (Hype:X / Hype:YouTube). Nothing to do.");
            return JobRunResult.NoOp();
        }
        var seedPath = Path.IsPathRooted(worker.SeedFilePath) ? worker.SeedFilePath : Path.Combine(AppContext.BaseDirectory, worker.SeedFilePath);
        if (!File.Exists(seedPath)) return JobRunResult.Failure($"Seed not found at '{seedPath}'.");
        var seed = JsonNode.Parse(await File.ReadAllTextAsync(seedPath, cancellationToken)) as JsonArray ?? [];
        var now = (clock ?? (() => DateTimeOffset.UtcNow))();

        var compounds = seed
            .Where(r => (r?["evidence"]?["publicClaims"] as JsonArray)?.Count > 0)
            .Select(r => r!["identity"]!["canonicalName"]!.GetValue<string>())
            .Take(options.MaxCompounds)
            .ToList();

        var rows = new JsonArray();
        foreach (var name in compounds)
        {
            var signals = new List<HypeSignal>();
            foreach (var source in sources)
                signals.Add(await source.CountAsync($"\"{name}\"", now, options.WindowDays, cancellationToken));
            var x = signals.FirstOrDefault(s => s.Source == "x");
            rows.Add(new JsonObject
            {
                ["compound"] = name,
                ["signals"] = JsonSerializer.SerializeToNode(signals, JsonOpts),
                ["suggestedLevel"] = HypeSources.SuggestedLevel(x?.Count, options.X.RubricThresholds),
            });
        }

        var report = new JsonObject
        {
            ["generatedAt"] = now.ToString("O"),
            ["windowDays"] = options.WindowDays,
            ["note"] = "Editor input only. Counts measure public talk, not use. Hype scores are set by editors and published through the AI council.",
            ["compounds"] = rows,
        };
        if (context.DryRun)
        {
            context.Logger.LogInformation("[HypeSignals] DryRun: {Count} compound(s), nothing written.", rows.Count);
            return JobRunResult.NoOp();
        }
        var dir = Path.IsPathRooted(options.OutputDirectory) ? options.OutputDirectory : Path.Combine(Directory.GetCurrentDirectory(), options.OutputDirectory);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"hype-signals-{now:yyyyMMdd}.json");
        await File.WriteAllTextAsync(path, report.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        context.Logger.LogInformation("[HypeSignals] Wrote {Path} ({Count} compounds).", path, rows.Count);
        return new JobRunResult(true, rows.Count, rows.Count, 0, 0, 0, 0);
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
