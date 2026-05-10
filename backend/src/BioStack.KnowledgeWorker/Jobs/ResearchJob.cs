namespace BioStack.KnowledgeWorker.Jobs;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

public interface IResearchJob : IIngestionJob { }

public sealed class ResearchJob : IResearchJob
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly WorkerOptions _options;
    private readonly IResearchArtifactLoader _loader;
    private readonly IResearchArtifactValidator _researchValidator;
    private readonly IEvidencePacketPreprocessor _preprocessor;
    private readonly ISourceRegistryAuthorizer _sourceRegistryAuthorizer;
    private readonly IEvidencePacketSubstanceRecordCompiler _compiler;
    private readonly IResearchReviewQueueBuilder _reviewQueueBuilder;
    private readonly IResearchSummaryBuilder _summaryBuilder;
    private readonly IPromotionManifestBuilder _promotionManifestBuilder;
    private readonly IReviewResolutionPlanBuilder _reviewResolutionPlanBuilder;
    private readonly IPromotionExporter _promotionExporter;
    private readonly IPromotionImportPreviewBuilder _promotionImportPreviewBuilder;
    private readonly ISubstanceRecordValidator _substanceValidator;

    public ResearchJob(
        WorkerOptions options,
        IResearchArtifactLoader loader,
        IResearchArtifactValidator researchValidator,
        IEvidencePacketPreprocessor preprocessor,
        ISourceRegistryAuthorizer sourceRegistryAuthorizer,
        IEvidencePacketSubstanceRecordCompiler compiler,
        IResearchReviewQueueBuilder reviewQueueBuilder,
        IResearchSummaryBuilder summaryBuilder,
        IPromotionManifestBuilder promotionManifestBuilder,
        IReviewResolutionPlanBuilder reviewResolutionPlanBuilder,
        IPromotionExporter promotionExporter,
        IPromotionImportPreviewBuilder promotionImportPreviewBuilder,
        ISubstanceRecordValidator substanceValidator)
    {
        _options = options;
        _loader = loader;
        _researchValidator = researchValidator;
        _preprocessor = preprocessor;
        _sourceRegistryAuthorizer = sourceRegistryAuthorizer;
        _compiler = compiler;
        _reviewQueueBuilder = reviewQueueBuilder;
        _summaryBuilder = summaryBuilder;
        _promotionManifestBuilder = promotionManifestBuilder;
        _reviewResolutionPlanBuilder = reviewResolutionPlanBuilder;
        _promotionExporter = promotionExporter;
        _promotionImportPreviewBuilder = promotionImportPreviewBuilder;
        _substanceValidator = substanceValidator;
    }

    public Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        var outputDir = ResolveOutputDirectory(_options.ResearchOutputDirectory);
        Directory.CreateDirectory(outputDir);

        var report = new ResearchRunReport(
            StartedAtUtc: DateTimeOffset.UtcNow,
            OutputDirectory: outputDir,
            Inputs: new List<string>(),
            Records: new List<ResearchRunRecord>(),
            Outputs: new Dictionary<string, string>());

        ValidateOptionalArtifact(context, report, ResearchArtifactKind.CompoundCandidateBatch, _options.ResearchCandidateFilePath);
        var sourceRegistry = ValidateOptionalArtifact(context, report, ResearchArtifactKind.SourceRegistry, _options.ResearchSourceRegistryFilePath);

        var evidenceFiles = ResolveEvidenceFiles(_options).ToList();
        if (evidenceFiles.Count == 0)
        {
            context.IncrementFailed();
            report.Records.Add(ResearchRunRecord.Failed(
                "evidence-packets",
                "No evidence packet files were provided. Set Worker:ResearchEvidencePacketPath or Worker:ResearchEvidencePacketDirectory."));
            WriteReport(outputDir, report);
            context.LogSummary("ResearchJob");
            return Task.FromResult(JobRunResult.FromContext(context));
        }

        var drafts = new JsonArray();
        var reviewQueue = new List<ResearchReviewQueueItem>();
        var reviewDecisions = LoadReviewDecisions(context, report);
        var researchRequests = LoadResearchRequests(context, report);

        var evidencePacketOutputDir = Path.Combine(outputDir, "evidence-packet");
        Directory.CreateDirectory(evidencePacketOutputDir);

        foreach (var file in evidenceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessEvidencePacket(context, report, file, sourceRegistry, drafts, reviewQueue, evidencePacketOutputDir);
        }

        var draftsPath = Path.Combine(outputDir, "draft-substances.json");
        var reviewQueuePath = Path.Combine(outputDir, "review-queue.json");
        var summaryPath = Path.Combine(outputDir, "research-summary.json");
        var promotionManifestPath = Path.Combine(outputDir, "promotion-manifest.json");
        var resolutionPlanPath = Path.Combine(outputDir, "review-resolution-plan.json");
        var importPreviewPath = Path.Combine(outputDir, "promotion-import-preview.json");
        var activeReviewQueue = reviewQueue
            .Where(item => !reviewDecisions.IsCompoundArchived(item.CompoundName))
            .Where(item => !reviewDecisions.IsReviewQueueItemResolved(item.CompoundName, item.ItemId))
            .ToList();
        var summary = _summaryBuilder.Build(drafts, activeReviewQueue, reviewDecisions, researchRequests);
        var promotionManifest = _promotionManifestBuilder.Build(summary, new PromotionManifestOutputs(
            DraftSubstances: draftsPath,
            ReviewQueue: reviewQueuePath,
            ResearchSummary: summaryPath,
            RunReport: Path.Combine(outputDir, "research-run-report.json")));
        var resolutionPlan = _reviewResolutionPlanBuilder.Build(promotionManifest, activeReviewQueue);
        var promotionExport = _promotionExporter.Export(drafts, promotionManifest, outputDir);
        var importPreview = _promotionImportPreviewBuilder.Build(
            LoadJsonArray(promotionExport.AggregatePath),
            LoadPromotionExportManifest(promotionExport.ManifestPath),
            LoadOptionalSeedArray(_options.SeedFilePath),
            _substanceValidator);
        File.WriteAllText(draftsPath, drafts.ToJsonString(JsonOptions));
        File.WriteAllText(reviewQueuePath, JsonSerializer.Serialize(activeReviewQueue, JsonOptions));
        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, JsonOptions));
        File.WriteAllText(promotionManifestPath, JsonSerializer.Serialize(promotionManifest, JsonOptions));
        File.WriteAllText(resolutionPlanPath, JsonSerializer.Serialize(resolutionPlan, JsonOptions));
        File.WriteAllText(importPreviewPath, JsonSerializer.Serialize(importPreview, JsonOptions));
        report.Outputs["draftSubstances"] = draftsPath;
        report.Outputs["reviewQueue"] = reviewQueuePath;
        report.Outputs["researchSummary"] = summaryPath;
        report.Outputs["promotionManifest"] = promotionManifestPath;
        report.Outputs["reviewResolutionPlan"] = resolutionPlanPath;
        report.Outputs["evidencePacketDirectory"] = evidencePacketOutputDir;
        report.Outputs["promotionExportDirectory"] = promotionExport.ExportDirectory;
        report.Outputs["promotionExportManifest"] = promotionExport.ManifestPath;
        report.Outputs["promotionExportAggregate"] = promotionExport.AggregatePath;
        report.Outputs["promotionImportPreview"] = importPreviewPath;
        WriteReport(outputDir, report);

        context.LogSummary("ResearchJob");
        return Task.FromResult(JobRunResult.FromContext(context));
    }

    private JsonNode? ValidateOptionalArtifact(
        IngestionContext context,
        ResearchRunReport report,
        ResearchArtifactKind kind,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            var loaded = _loader.Load(kind, ResolveInputPath(path));
            report.Inputs.Add(loaded.Path);
            var result = _researchValidator.Validate(kind, loaded.Node);
            if (result.IsValid)
            {
                report.Records.Add(ResearchRunRecord.Validated(loaded.Path, kind.ToString()));
                return loaded.Node;
            }
            else
            {
                context.IncrementFailed();
                report.Records.Add(ResearchRunRecord.Failed(loaded.Path, result.Summary()));
            }
        }
        catch (Exception ex)
        {
            context.IncrementFailed();
            report.Records.Add(ResearchRunRecord.Failed(path, ex.Message));
        }
        return null;
    }

    private void ProcessEvidencePacket(
        IngestionContext context,
        ResearchRunReport report,
        string file,
        JsonNode? sourceRegistry,
        JsonArray drafts,
        List<ResearchReviewQueueItem> reviewQueue,
        string evidencePacketOutputDir)
    {
        context.IncrementScanned();
        try
        {
            var loaded = _loader.Load(ResearchArtifactKind.EvidencePacket, file);
            report.Inputs.Add(loaded.Path);
            var validation = _researchValidator.Validate(ResearchArtifactKind.EvidencePacket, loaded.Node);
            if (!validation.IsValid)
            {
                context.IncrementFailed();
                report.Records.Add(ResearchRunRecord.Failed(loaded.Path, validation.Summary()));
                return;
            }

            var preprocessed = _preprocessor.Preprocess(loaded.Node);
            var packet = preprocessed.Packet;
            if (sourceRegistry is not null)
            {
                packet = _sourceRegistryAuthorizer.Authorize(packet, sourceRegistry).Packet;
            }

            var postValidation = _researchValidator.Validate(ResearchArtifactKind.EvidencePacket, packet);
            if (!postValidation.IsValid)
            {
                context.IncrementFailed();
                report.Records.Add(ResearchRunRecord.Failed(loaded.Path, postValidation.Summary()));
                return;
            }

            WriteEvidencePacketArtifact(evidencePacketOutputDir, packet);

            var draft = _compiler.CompileDraft(packet);
            var draftValidation = _substanceValidator.Validate(draft);
            if (!draftValidation.IsValid)
            {
                context.IncrementFailed();
                report.Records.Add(ResearchRunRecord.Failed(loaded.Path, draftValidation.Summary()));
                return;
            }

            var items = _reviewQueueBuilder.BuildFromEvidencePacket(packet);
            reviewQueue.AddRange(items);
            drafts.Add(draft);
            context.IncrementCreated();
            if (items.Count > 0 || draft["ops"]?["needsReview"]?.GetValue<bool>() == true)
            {
                context.IncrementFlaggedForReview();
            }

            report.Records.Add(ResearchRunRecord.Compiled(
                loaded.Path,
                ReadString(packet["compound"]?["canonicalName"]),
                items.Count,
                ReadQualityFlags(packet)));
        }
        catch (Exception ex)
        {
            context.IncrementFailed();
            report.Records.Add(ResearchRunRecord.Failed(file, ex.Message));
        }
    }

    private static void WriteEvidencePacketArtifact(string outputDir, JsonNode packet)
    {
        var canonicalName = ReadString(packet["compound"]?["canonicalName"]);
        var slug = SubstanceRecordNormalizer.Slugify(canonicalName);
        if (slug.Length == 0) return;

        var artifactPath = Path.Combine(outputDir, $"{slug}.json");
        File.WriteAllText(artifactPath, packet.ToJsonString(JsonOptions));
    }

    private void WriteReport(string outputDir, ResearchRunReport report)
    {
        var reportPath = Path.Combine(outputDir, "research-run-report.json");
        report.CompletedAtUtc = DateTimeOffset.UtcNow;
        report.Outputs["runReport"] = reportPath;
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, JsonOptions));
    }

    private static IEnumerable<string> ResolveEvidenceFiles(WorkerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ResearchEvidencePacketPath))
        {
            yield return ResolveInputPath(options.ResearchEvidencePacketPath);
        }

        if (string.IsNullOrWhiteSpace(options.ResearchEvidencePacketDirectory)) yield break;

        var dir = ResolveInputPath(options.ResearchEvidencePacketDirectory);
        if (!Directory.Exists(dir)) yield break;

        foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f))
        {
            yield return file;
        }
    }

    private ReviewDecisionIndex LoadReviewDecisions(IngestionContext context, ResearchRunReport report)
    {
        var batches = new List<JsonNode>();
        foreach (var file in ResolveReviewDecisionFiles(_options))
        {
            try
            {
                var loaded = _loader.Load(ResearchArtifactKind.ReviewDecisionBatch, file);
                report.Inputs.Add(loaded.Path);
                var validation = _researchValidator.Validate(ResearchArtifactKind.ReviewDecisionBatch, loaded.Node);
                if (!validation.IsValid)
                {
                    context.IncrementFailed();
                    report.Records.Add(ResearchRunRecord.Failed(loaded.Path, validation.Summary()));
                    continue;
                }

                batches.Add(loaded.Node);
                report.Records.Add(ResearchRunRecord.Validated(loaded.Path, ResearchArtifactKind.ReviewDecisionBatch.ToString()));
            }
            catch (Exception ex)
            {
                context.IncrementFailed();
                report.Records.Add(ResearchRunRecord.Failed(file, ex.Message));
            }
        }

        return batches.Count == 0 ? ReviewDecisionIndex.Empty : ReviewDecisionIndex.FromBatches(batches);
    }

    private ResearchRequestIndex LoadResearchRequests(IngestionContext context, ResearchRunReport report)
    {
        var batches = new List<JsonNode>();
        foreach (var file in ResolveResearchRequestFiles(_options))
        {
            try
            {
                var loaded = _loader.Load(ResearchArtifactKind.ResearchRequestBatch, file);
                report.Inputs.Add(loaded.Path);
                var validation = _researchValidator.Validate(ResearchArtifactKind.ResearchRequestBatch, loaded.Node);
                if (!validation.IsValid)
                {
                    context.IncrementFailed();
                    report.Records.Add(ResearchRunRecord.Failed(loaded.Path, validation.Summary()));
                    continue;
                }

                batches.Add(loaded.Node);
                report.Records.Add(ResearchRunRecord.Validated(loaded.Path, ResearchArtifactKind.ResearchRequestBatch.ToString()));
            }
            catch (Exception ex)
            {
                context.IncrementFailed();
                report.Records.Add(ResearchRunRecord.Failed(file, ex.Message));
            }
        }

        return batches.Count == 0 ? ResearchRequestIndex.Empty : ResearchRequestIndex.FromBatches(batches);
    }

    private static IEnumerable<string> ResolveReviewDecisionFiles(WorkerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ResearchReviewDecisionPath))
        {
            yield return ResolveInputPath(options.ResearchReviewDecisionPath);
        }

        if (string.IsNullOrWhiteSpace(options.ResearchReviewDecisionDirectory)) yield break;
        var dir = ResolveInputPath(options.ResearchReviewDecisionDirectory);
        if (!Directory.Exists(dir)) yield break;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f))
        {
            yield return file;
        }
    }

    private static IEnumerable<string> ResolveResearchRequestFiles(WorkerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ResearchRequestPath))
        {
            yield return ResolveInputPath(options.ResearchRequestPath);
        }

        if (string.IsNullOrWhiteSpace(options.ResearchRequestDirectory)) yield break;
        var dir = ResolveInputPath(options.ResearchRequestDirectory);
        if (!Directory.Exists(dir)) yield break;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly).OrderBy(f => f))
        {
            yield return file;
        }
    }

    private static string ResolveInputPath(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static string ResolveOutputDirectory(string path)
        => Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    private static JsonArray LoadJsonArray(string path)
        => JsonNode.Parse(File.ReadAllText(path)) as JsonArray
           ?? throw new InvalidOperationException($"Expected JSON array at '{path}'.");

    private static PromotionExportManifest LoadPromotionExportManifest(string path)
        => JsonSerializer.Deserialize<PromotionExportManifest>(File.ReadAllText(path))
           ?? throw new InvalidOperationException($"Could not deserialize promotion export manifest at '{path}'.");

    private static JsonArray LoadOptionalSeedArray(string path)
    {
        var resolved = ResolveInputPath(path);
        if (!File.Exists(resolved)) return new JsonArray();
        return JsonNode.Parse(File.ReadAllText(resolved)) as JsonArray
               ?? throw new InvalidOperationException($"Expected seed JSON array at '{resolved}'.");
    }

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static IReadOnlyList<string> ReadQualityFlags(JsonNode packet)
        => packet["ops"]?["qualityFlags"] is JsonArray arr
            ? arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : Array.Empty<string>();

    private sealed record ResearchRunReport(
        DateTimeOffset StartedAtUtc,
        string OutputDirectory,
        List<string> Inputs,
        List<ResearchRunRecord> Records,
        Dictionary<string, string> Outputs)
    {
        public DateTimeOffset? CompletedAtUtc { get; set; }
    }

    private sealed record ResearchRunRecord(
        string Path,
        string Status,
        string? ArtifactKind,
        string? CompoundName,
        int ReviewQueueItems,
        IReadOnlyList<string> QualityFlags,
        string? Error)
    {
        public static ResearchRunRecord Validated(string path, string artifactKind) => new(
            path, "validated", artifactKind, null, 0, Array.Empty<string>(), null);

        public static ResearchRunRecord Compiled(
            string path,
            string compoundName,
            int reviewQueueItems,
            IReadOnlyList<string> qualityFlags) => new(
            path, "compiled", ResearchArtifactKind.EvidencePacket.ToString(), compoundName, reviewQueueItems, qualityFlags, null);

        public static ResearchRunRecord Failed(string path, string error) => new(
            path, "failed", null, null, 0, Array.Empty<string>(), error);
    }
}