namespace BioStack.Application.Services;

using System.Diagnostics;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Logging;

public sealed class ProtocolAnalyzerService : IProtocolAnalyzerService
{
    private static readonly TimeSpan ParseCacheTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan AnalysisCacheTtl = TimeSpan.FromDays(14);
    private static readonly TimeSpan CounterfactualCacheTtl = TimeSpan.FromDays(21);

    private readonly IProtocolParser _parser;
    private readonly IProtocolIngestionService _ingestionService;
    private readonly IProtocolNormalizationService _normalizationService;
    private readonly IProtocolFingerprintService _fingerprintService;
    private readonly IProtocolAnalysisCache _cache;
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IInteractionIntelligenceService _interactionIntelligenceService;
    private readonly IProtocolSuggestionService _suggestionService;
    private readonly ICounterfactualEngine _counterfactualEngine;
    private readonly IProtocolAnalysisPersistenceHook _persistenceHook;
    private readonly ILogger<ProtocolAnalyzerService> _logger;

    public ProtocolAnalyzerService(
        IProtocolParser parser,
        IProtocolIngestionService ingestionService,
        IProtocolNormalizationService normalizationService,
        IProtocolFingerprintService fingerprintService,
        IProtocolAnalysisCache cache,
        IKnowledgeSource knowledgeSource,
        IInteractionIntelligenceService interactionIntelligenceService,
        IProtocolSuggestionService suggestionService,
        ICounterfactualEngine counterfactualEngine,
        IProtocolAnalysisPersistenceHook persistenceHook,
        ILogger<ProtocolAnalyzerService> logger)
    {
        _parser = parser;
        _ingestionService = ingestionService;
        _normalizationService = normalizationService;
        _fingerprintService = fingerprintService;
        _cache = cache;
        _knowledgeSource = knowledgeSource;
        _interactionIntelligenceService = interactionIntelligenceService;
        _suggestionService = suggestionService;
        _counterfactualEngine = counterfactualEngine;
        _persistenceHook = persistenceHook;
        _logger = logger;
    }

    public Task<AnalyzeProtocolResponse> AnalyzeAsync(AnalyzeProtocolRequest request, CancellationToken cancellationToken = default)
    {
        return AnalyzeAsync(
            request,
            new ProtocolIngestionRequest(
                request.InputType,
                request.InputText,
                request.LinkUrl,
                request.SourceName,
                null,
                null),
            cancellationToken);
    }

    public async Task<AnalyzeProtocolResponse> AnalyzeAsync(
        AnalyzeProtocolRequest request,
        ProtocolIngestionRequest ingestionRequest,
        CancellationToken cancellationToken = default)
    {
        var ingestion = await _ingestionService.IngestAsync(ingestionRequest, cancellationToken);

        var parseKey = $"analyzer:parse:parser-{ProtocolFingerprintService.ParserVersion}:{ingestion.ParseFingerprint}";
        var parseStopwatch = Stopwatch.StartNew();
        var parseResult = await GetOrParseAsync(ingestion.NormalizedText, parseKey, cancellationToken);
        parseStopwatch.Stop();

        var normalizedProtocol = _normalizationService.Normalize(parseResult);
        var analysisContext = _normalizationService.BuildAnalysisContext(request.Goal, request.Sex, request.Age, request.Weight, request.ExistingStackContext);
        var optimizationContext = _normalizationService.BuildOptimizationContext(
            request.Goal,
            request.MaxCompounds,
            request.RequiredCompoundIds,
            request.ExcludedCompoundIds,
            request.ExistingStackContext);

        var knownEntries = parseResult.Entries
            .Where(entry => parseResult.KnowledgeByCompound.ContainsKey(entry.CompoundName))
            .Select(entry => parseResult.KnowledgeByCompound[entry.CompoundName])
            .DistinctBy(entry => entry.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var analysisKey = _fingerprintService.GetAnalysisKey(normalizedProtocol, analysisContext);
        var analysisStopwatch = Stopwatch.StartNew();
        var analysis = await GetOrAnalyzeAsync(parseResult, knownEntries, analysisKey, cancellationToken);
        analysisStopwatch.Stop();

        var counterfactualKey = _fingerprintService.GetCounterfactualKey(normalizedProtocol, optimizationContext);
        var counterfactualStopwatch = Stopwatch.StartNew();
        var counterfactuals = await GetOrOptimizeAsync(parseResult.Entries, knownEntries, optimizationContext, counterfactualKey, cancellationToken);
        counterfactualStopwatch.Stop();

        var suggestions = _suggestionService.Suggest(parseResult, analysis.Issues, counterfactuals);

        var response = new AnalyzeProtocolResponse(
            parseResult.Entries,
            analysis.Score,
            analysis.ScoreExplanation,
            analysis.Issues.Take(5).ToList(),
            suggestions,
            parseResult.BlendExpansions,
            analysis.UnknownCompounds,
            counterfactuals,
            request.InputType.ToString(),
            ingestion.SourceName,
            ingestion.Warnings.ToList(),
            BuildParserWarnings(parseResult, analysis.UnknownCompounds),
            ingestion.LowConfidence,
            CreateExtractedTextPreview(ingestion.NormalizedText),
            ingestion.Artifacts.Select(artifact => new BioStack.Contracts.Responses.ProtocolIngestionArtifactResponse(artifact.Kind, artifact.Label, artifact.Preview)).ToList());

        await _persistenceHook.RecordAsync(_fingerprintService.GetNormalizedProtocolHash(normalizedProtocol), response, cancellationToken);

        _logger.LogInformation(
            "Analyzer pipeline complete. ParseKey={ParseKey} AnalysisKey={AnalysisKey} CounterfactualKey={CounterfactualKey} ParseMs={ParseMs} AnalysisMs={AnalysisMs} CounterfactualMs={CounterfactualMs}",
            parseKey,
            analysisKey,
            counterfactualKey,
            parseStopwatch.ElapsedMilliseconds,
            analysisStopwatch.ElapsedMilliseconds,
            counterfactualStopwatch.ElapsedMilliseconds);

        return response;
    }

    private async Task<ProtocolParseResult> GetOrParseAsync(string inputText, string parseKey, CancellationToken cancellationToken)
    {
        var cached = await _cache.GetParsedAsync(parseKey, cancellationToken);
        if (cached is not null)
        {
            var knowledge = new Dictionary<string, KnowledgeEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in cached.Protocol)
            {
                var match = await _knowledgeSource.GetCompoundAsync(entry.CompoundName, cancellationToken);
                if (match is not null)
                {
                    knowledge[entry.CompoundName] = match;
                }
            }

            return new ProtocolParseResult(cached.Protocol, knowledge, cached.DecomposedBlends);
        }

        var parsed = await _parser.ParseAsync(inputText, cancellationToken);
        await _cache.SetParsedAsync(parseKey, new ParsedProtocolCacheDto(parsed.Entries, parsed.BlendExpansions), ParseCacheTtl, cancellationToken);

        // The parser builds KnowledgeByCompound from its in-memory alias cache, which may be stale
        // (e.g. after new compounds are seeded via the admin endpoint). Re-verify each parsed entry
        // against the live DB so freshly seeded compounds are immediately reflected in the analysis.
        var augmentedKnowledge = new Dictionary<string, KnowledgeEntry>(parsed.KnowledgeByCompound, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in parsed.Entries)
        {
            if (!augmentedKnowledge.ContainsKey(entry.CompoundName))
            {
                var match = await _knowledgeSource.GetCompoundAsync(entry.CompoundName, cancellationToken);
                if (match is not null)
                {
                    augmentedKnowledge[entry.CompoundName] = match;
                }
            }
        }

        return new ProtocolParseResult(parsed.Entries, augmentedKnowledge, parsed.BlendExpansions);
    }

    private async Task<ProtocolAnalysisCacheDto> GetOrAnalyzeAsync(
        ProtocolParseResult parseResult,
        IReadOnlyList<KnowledgeEntry> knownEntries,
        string analysisKey,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAnalysisAsync(analysisKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var interactionIntelligence = await _interactionIntelligenceService.EvaluateAsync(knownEntries, cancellationToken);
        var issues = DeriveIssues(parseResult, interactionIntelligence);
        var unknownCompounds = parseResult.Entries
            .Where(entry => !parseResult.KnowledgeByCompound.ContainsKey(entry.CompoundName))
            .Select(entry => entry.CompoundName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var analysis = new ProtocolAnalysisCacheDto(
            (int)Math.Round(interactionIntelligence.CompositeScore),
            BuildScoreExplanation(interactionIntelligence),
            issues,
            unknownCompounds);

        await _cache.SetAnalysisAsync(analysisKey, analysis, AnalysisCacheTtl, cancellationToken);
        return analysis;
    }

    private async Task<CounterfactualResultDto> GetOrOptimizeAsync(
        List<ProtocolEntryResponse> protocol,
        IReadOnlyList<KnowledgeEntry> knownEntries,
        OptimizationContext optimizationContext,
        string counterfactualKey,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetCounterfactualAsync(counterfactualKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var computed = await _counterfactualEngine.OptimizeAsync(protocol, knownEntries, optimizationContext, cancellationToken);
        await _cache.SetCounterfactualAsync(counterfactualKey, computed, CounterfactualCacheTtl, cancellationToken);
        return computed;
    }

    private static List<ProtocolIssueResponse> DeriveIssues(
        ProtocolParseResult parseResult,
        InteractionIntelligenceResponse interactionIntelligence)
    {
        var issues = new List<ProtocolIssueResponse>();

        foreach (var result in interactionIntelligence.Interactions
            .Where(result => result.Type == BioStack.Domain.Enums.InteractionType.Redundant)
            .OrderByDescending(result => result.Confidence)
            .Take(3))
        {
            issues.Add(new ProtocolIssueResponse(
                "redundancy",
                $"Overlapping pathways detected between {result.CompoundA} and {result.CompoundB}.",
                new List<string> { result.CompoundA, result.CompoundB }));
        }

        foreach (var result in interactionIntelligence.Interactions
            .Where(result => result.Type == BioStack.Domain.Enums.InteractionType.Interfering)
            .OrderByDescending(result => result.Confidence)
            .Take(3))
        {
            issues.Add(new ProtocolIssueResponse(
                "overlap",
                $"Potential interference between {result.CompoundA} and {result.CompoundB}.",
                new List<string> { result.CompoundA, result.CompoundB }));
        }

        if (interactionIntelligence.Summary.Synergies == 0 && parseResult.Entries.Count > 1)
        {
            issues.Add(new ProtocolIssueResponse(
                "inefficiency",
                "Stack lacks strong complementary pathway signals under the current rule set.",
                parseResult.Entries.Select(entry => entry.CompoundName).ToList()));
        }

        if (parseResult.Entries.Count > 5)
        {
            issues.Add(new ProtocolIssueResponse(
                "excessive_compounds",
                "Large stacks are harder to attribute cleanly and usually benefit from simplification.",
                parseResult.Entries.Select(entry => entry.CompoundName).ToList()));
        }

        return issues
            .GroupBy(issue => $"{issue.Type}:{issue.Message}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static ProtocolScoreExplanationResponse BuildScoreExplanation(InteractionIntelligenceResponse interactionIntelligence)
    {
        return new ProtocolScoreExplanationResponse(
            50,
            (int)Math.Round(interactionIntelligence.Score.SynergyScore * 18d),
            (int)Math.Round(interactionIntelligence.Score.RedundancyPenalty * -14d),
            (int)Math.Round(interactionIntelligence.Score.InterferencePenalty * -12d));
    }

    private static List<string> BuildParserWarnings(ProtocolParseResult parseResult, IReadOnlyList<string> unknownCompounds)
    {
        var warnings = new List<string>();
        if (unknownCompounds.Count > 0)
        {
            warnings.Add($"{unknownCompounds.Count} compound{(unknownCompounds.Count == 1 ? string.Empty : "s")} could not be fully normalized.");
        }

        if (parseResult.BlendExpansions.Count > 0)
        {
            warnings.Add($"{parseResult.BlendExpansions.Count} blend{(parseResult.BlendExpansions.Count == 1 ? string.Empty : "s")} were expanded into individual compounds.");
        }

        if (parseResult.Entries.Any(entry => entry.Dose <= 0 || string.IsNullOrWhiteSpace(entry.Frequency)))
        {
            warnings.Add("One or more protocol entries were only partially parsed.");
        }

        return warnings;
    }

    private static string? CreateExtractedTextPreview(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return null;
        }

        return normalizedText.Length <= 400
            ? normalizedText
            : $"{normalizedText[..397]}...";
    }
}

public interface IProtocolAnalyzerService
{
    Task<AnalyzeProtocolResponse> AnalyzeAsync(AnalyzeProtocolRequest request, CancellationToken cancellationToken = default);
    Task<AnalyzeProtocolResponse> AnalyzeAsync(AnalyzeProtocolRequest request, ProtocolIngestionRequest ingestionRequest, CancellationToken cancellationToken = default);
}
