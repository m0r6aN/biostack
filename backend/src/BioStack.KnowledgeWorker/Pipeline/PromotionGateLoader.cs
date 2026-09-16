namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;

/// <summary>
/// Loads the <see cref="ReviewDecisionIndex"/> that backs Refresh's promotion gate.
/// Fails closed by design: every failure mode (missing directory, no matching files, a
/// file that isn't valid JSON, a batch that fails schema validation) throws
/// <see cref="PromotionGateLoadException"/> rather than silently falling back to an empty
/// index — an empty index would make every compound look unpromoted, which is safe for
/// the upsert gate itself but would hide a configuration mistake (wrong path, empty
/// clone) behind a Refresh that quietly skips everything.
/// </summary>
public static class PromotionGateLoader
{
    private const string ReviewDecisionFileGlob = "review-decision-batch-*.json";

    /// <summary>
    /// Loads and schema-validates every configured review-decision batch file. The schema
    /// validator is supplied by the caller (rather than resolved from
    /// <c>AppContext.BaseDirectory</c> here) so this method behaves identically whether it
    /// runs from the published worker (schemas live next to the worker DLL — see
    /// <c>Config.RefreshPromotionGateStartup</c>) or from a test project with its own
    /// schema-directory resolution (see <c>TestPaths.WorkerSchemaDirectory()</c>).
    /// </summary>
    public static ReviewDecisionIndex LoadReviewDecisionIndexOrThrow(
        WorkerOptions options,
        IResearchArtifactValidator validator,
        IResearchArtifactLoader? loader = null)
    {
        var files = ResolveReviewDecisionFiles(options).ToList();
        if (files.Count == 0)
        {
            throw new PromotionGateLoadException(
                "Refresh's promotion gate found no review-decision batch files to load. Checked "
                + $"Worker:ReviewDecisionPath='{options.ReviewDecisionPath ?? "(unset)"}' and "
                + $"Worker:ReviewDecisionDirectory='{options.ReviewDecisionDirectory ?? "(unset)"}' "
                + $"resolved against the current directory '{Directory.GetCurrentDirectory()}'. "
                + "Refresh refuses to run without a loadable review-decision index (fail-closed). "
                + "Run from the repository root, or set --Worker:ReviewDecisionDirectory explicitly, "
                + "or pass --Worker:AllowUnpromoted=true for an explicit dev/local override.");
        }

        loader ??= new ResearchArtifactLoader();
        var batches = new List<JsonNode>();

        foreach (var file in files)
        {
            LoadedResearchArtifact loaded;
            try
            {
                loaded = loader.Load(ResearchArtifactKind.ReviewDecisionBatch, file);
            }
            catch (Exception ex)
            {
                throw new PromotionGateLoadException(
                    $"Refresh's promotion gate could not parse review-decision batch at '{file}': {ex.Message}", ex);
            }

            var validation = validator.Validate(ResearchArtifactKind.ReviewDecisionBatch, loaded.Node);
            if (!validation.IsValid)
            {
                throw new PromotionGateLoadException(
                    $"Refresh's promotion gate: review-decision batch at '{file}' failed schema "
                    + $"validation: {validation.Summary()}");
            }

            batches.Add(loaded.Node);
        }

        return ReviewDecisionIndex.FromBatches(batches);
    }

    private static IEnumerable<string> ResolveReviewDecisionFiles(WorkerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ReviewDecisionPath))
        {
            yield return ResolveInputPath(options.ReviewDecisionPath);
        }

        if (string.IsNullOrWhiteSpace(options.ReviewDecisionDirectory))
        {
            yield break;
        }

        var dir = ResolveInputPath(options.ReviewDecisionDirectory);
        if (!Directory.Exists(dir))
        {
            yield break;
        }

        foreach (var file in Directory
                     .EnumerateFiles(dir, ReviewDecisionFileGlob, SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f, StringComparer.Ordinal))
        {
            yield return file;
        }
    }

    /// <summary>
    /// Resolves against the current working directory, not <c>AppContext.BaseDirectory</c>
    /// — the review-decision corpus lives under the repo's <c>research/</c> tree, not next
    /// to the worker's published binaries, and the documented runbook always runs the
    /// worker from the repository root.
    /// </summary>
    private static string ResolveInputPath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(Directory.GetCurrentDirectory(), path);
}
