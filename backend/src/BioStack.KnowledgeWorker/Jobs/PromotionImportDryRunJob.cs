namespace BioStack.KnowledgeWorker.Jobs;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

public interface IPromotionImportDryRunJob : IIngestionJob { }

public sealed class PromotionImportDryRunJob : IPromotionImportDryRunJob
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly WorkerOptions _options;
    private readonly ISubstanceRecordValidator _validator;

    public PromotionImportDryRunJob(WorkerOptions options, ISubstanceRecordValidator validator)
    {
        _options = options;
        _validator = validator;
    }

    public Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        var outputDir = ResolveOutputDirectory(_options.PromotionImportDryRunOutputDirectory);
        Directory.CreateDirectory(outputDir);
        var reportPath = Path.Combine(outputDir, "promotion-import-dry-run-report.json");

        try
        {
            var previewPath = RequiredPath(_options.PromotionImportPreviewPath, "Worker:PromotionImportPreviewPath");
            var aggregatePath = RequiredPath(_options.PromotionImportAggregatePath, "Worker:PromotionImportAggregatePath");
            var preview = LoadPreview(previewPath);
            var exported = LoadArray(aggregatePath);
            var items = new List<PromotionImportDryRunItem>();

            foreach (var node in exported.Where(n => n is not null))
            {
                cancellationToken.ThrowIfCancellationRequested();
                context.IncrementScanned();
                var validation = _validator.Validate(node!);
                var name = ReadString(node!["identity"]?["canonicalName"]);
                var slug = ReadString(node!["identity"]?["slug"]);
                var action = preview.Items.FirstOrDefault(i =>
                    i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || i.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))?.Action ?? "unknown";
                items.Add(new PromotionImportDryRunItem(
                    Name: name,
                    Slug: slug,
                    PlannedAction: action,
                    SchemaValid: validation.IsValid,
                    Reasons: validation.IsValid ? Array.Empty<string>() : new[] { validation.Summary() }));
                if (!validation.IsValid) context.IncrementFailed();
                else if (action == "create") context.IncrementCreated();
                else if (action == "update") context.IncrementUpdated();
                else context.IncrementUnchanged();
            }

            var refusalReasons = RefusalReasons(preview, exported.Count, items).ToList();
            foreach (var _ in refusalReasons) context.IncrementFailed();
            var report = new PromotionImportDryRunReport(
                ReportVersion: "1.0.0",
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                PreviewPath: previewPath,
                AggregatePath: aggregatePath,
                SafeToApply: refusalReasons.Count == 0,
                RefusalReasons: refusalReasons,
                PreviewCounts: preview.Counts,
                Items: items);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));
            context.LogSummary("PromotionImportDryRunJob");
            return Task.FromResult(JobRunResult.FromContext(context));
        }
        catch (Exception ex)
        {
            context.IncrementFailed();
            var report = new PromotionImportDryRunReport(
                ReportVersion: "1.0.0",
                GeneratedAtUtc: DateTimeOffset.UtcNow,
                PreviewPath: _options.PromotionImportPreviewPath ?? string.Empty,
                AggregatePath: _options.PromotionImportAggregatePath ?? string.Empty,
                SafeToApply: false,
                RefusalReasons: new[] { ex.Message },
                PreviewCounts: null,
                Items: Array.Empty<PromotionImportDryRunItem>());
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));
            context.LogSummary("PromotionImportDryRunJob");
            return Task.FromResult(JobRunResult.FromContext(context) with { ErrorMessage = ex.Message });
        }
    }

    private static IEnumerable<string> RefusalReasons(
        PromotionImportPreview preview,
        int aggregateCount,
        IReadOnlyList<PromotionImportDryRunItem> items)
    {
        if (preview.Counts.WouldSkip > 0) yield return $"Preview contains {preview.Counts.WouldSkip} skipped record(s).";
        if (preview.Counts.SchemaInvalid > 0) yield return $"Preview contains {preview.Counts.SchemaInvalid} schema-invalid record(s).";
        if (preview.Counts.ActiveRecords > 0) yield return $"Preview contains {preview.Counts.ActiveRecords} active exported record(s).";
        if (preview.Counts.DuplicateSlugs > 0) yield return $"Preview contains {preview.Counts.DuplicateSlugs} duplicate slug bucket(s).";
        if (preview.Counts.DuplicateCanonicalIds > 0) yield return $"Preview contains {preview.Counts.DuplicateCanonicalIds} duplicate canonicalId bucket(s).";
        if (preview.Counts.TotalExported != aggregateCount) yield return "Preview total does not match export aggregate count.";
        if (items.Any(i => !i.SchemaValid)) yield return "Export aggregate failed schema revalidation.";
        if (items.Any(i => i.PlannedAction is not ("create" or "update"))) yield return "Export aggregate contains records without create/update preview actions.";
    }

    private static string RequiredPath(string? path, string optionName)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException($"{optionName} is required.");
        var resolved = ResolveInputPath(path);
        if (!File.Exists(resolved)) throw new FileNotFoundException($"Required file not found at '{resolved}'.", resolved);
        return resolved;
    }

    private static PromotionImportPreview LoadPreview(string path)
        => JsonSerializer.Deserialize<PromotionImportPreview>(File.ReadAllText(path))
           ?? throw new InvalidOperationException($"Could not deserialize promotion import preview at '{path}'.");

    private static JsonArray LoadArray(string path)
        => JsonNode.Parse(File.ReadAllText(path)) as JsonArray
           ?? throw new InvalidOperationException($"Expected JSON array at '{path}'.");

    private static string ResolveInputPath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static string ResolveOutputDirectory(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static string ReadString(JsonNode? node) => node?.GetValue<string>()?.Trim() ?? string.Empty;
}

public sealed record PromotionImportDryRunReport(
    string ReportVersion,
    DateTimeOffset GeneratedAtUtc,
    string PreviewPath,
    string AggregatePath,
    bool SafeToApply,
    IReadOnlyList<string> RefusalReasons,
    PromotionImportPreviewCounts? PreviewCounts,
    IReadOnlyList<PromotionImportDryRunItem> Items);

public sealed record PromotionImportDryRunItem(
    string Name,
    string Slug,
    string PlannedAction,
    bool SchemaValid,
    IReadOnlyList<string> Reasons);