namespace BioStack.Application.Services;

using System.Diagnostics;
using BioStack.Cognition;
using BioStack.Cognition.Models;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Keon.Collective;
using Microsoft.Extensions.Logging;

public sealed class ProtocolAnalyzerService : IProtocolAnalyzerService
{
    private static readonly TimeSpan ParseCacheTtl = TimeSpan.FromDays(7);
    private static readonly TimeSpan AnalysisCacheTtl = TimeSpan.FromDays(14);
    private static readonly TimeSpan CounterfactualCacheTtl = TimeSpan.FromDays(21);
    private static readonly TimeSpan StackReviewBoardTimeout = TimeSpan.FromMilliseconds(750);
    private static readonly EmergentFamilyRule[] EmergentFamilyRules =
    [
        new(
            Id: "repair-cluster",
            Title: "Repair-signaling cluster",
            PatternType: "support-cluster",
            Basis: "goal-alignment",
            MinCompoundCount: 2,
            SignalKeywords: ["repair", "healing", "wound", "angiogenesis", "regeneration", "collagen"],
            GoalKeywords: ["heal", "repair", "recover", "recovery", "injury", "tissue"]),
        new(
            Id: "metabolic-support",
            Title: "Mitochondrial / metabolic support cluster",
            PatternType: "support-cluster",
            Basis: "mechanism-complementarity",
            MinCompoundCount: 2,
            SignalKeywords: ["mitochond", "metabolic", "glucose", "insulin", "energy", "nad", "atp", "oxidation", "ampk"],
            GoalKeywords: []),
        new(
            Id: "gh-axis",
            Title: "GH-axis / growth-signal cluster",
            PatternType: "motif",
            Basis: "pathway-overlap",
            MinCompoundCount: 2,
            SignalKeywords: ["growth hormone", "growth-signal", "igf", "anabolic", "growth factor"],
            GoalKeywords: []),
        new(
            Id: "incretin-overlap",
            Title: "Incretin overlap cluster",
            PatternType: "redundancy-cluster",
            Basis: "mechanism-redundancy",
            MinCompoundCount: 2,
            SignalKeywords: ["glp-1", "gip", "incretin", "appetite", "gastric emptying"],
            GoalKeywords: [])
    ];

    private readonly IProtocolParser _parser;
    private readonly IProtocolIngestionService _ingestionService;
    private readonly IProtocolNormalizationService _normalizationService;
    private readonly IProtocolFingerprintService _fingerprintService;
    private readonly IProtocolAnalysisCache _cache;
    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IInteractionIntelligenceService _interactionIntelligenceService;
    private readonly IProtocolSuggestionService _suggestionService;
    private readonly ICounterfactualEngine _counterfactualEngine;
    private readonly IStackReviewBoardService _stackReviewBoardService;
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
        IStackReviewBoardService stackReviewBoardService,
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
        _stackReviewBoardService = stackReviewBoardService;
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
        var analysis = await GetOrAnalyzeAsync(parseResult, knownEntries, analysisContext.Goal, analysisKey, cancellationToken);
        analysisStopwatch.Stop();

        var counterfactualKey = _fingerprintService.GetCounterfactualKey(normalizedProtocol, optimizationContext);
        var counterfactualStopwatch = Stopwatch.StartNew();
        var counterfactuals = await GetOrOptimizeAsync(parseResult.Entries, knownEntries, optimizationContext, counterfactualKey, cancellationToken);
        counterfactualStopwatch.Stop();

        var suggestions = _suggestionService.Suggest(parseResult, analysis.Issues, counterfactuals);
        var stackReviewBoard = await TryBuildStackReviewBoardAsync(request, parseResult, analysis, cancellationToken);

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
            ingestion.Artifacts.Select(artifact => new BioStack.Contracts.Responses.ProtocolIngestionArtifactResponse(artifact.Kind, artifact.Label, artifact.Preview)).ToList(),
            analysis.KnownPatterns,
            analysis.EmergentPatterns,
            stackReviewBoard);

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
        string goal,
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
            unknownCompounds,
            DeriveKnownPatterns(interactionIntelligence),
            DeriveEmergentPatterns(goal, knownEntries, interactionIntelligence));

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

    private static List<KnownPatternResponse> DeriveKnownPatterns(InteractionIntelligenceResponse interactionIntelligence)
    {
        return interactionIntelligence.Interactions
            .Where(result => result.HintBacked && result.Type != BioStack.Domain.Enums.InteractionType.Neutral)
            .Select(ToKnownPattern)
            .GroupBy(pattern => pattern.PatternId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static KnownPatternResponse ToKnownPattern(InteractionResultResponse result)
    {
        var matchedCompounds = new[] { result.CompoundA, result.CompoundB }
            .Where(compound => !string.IsNullOrWhiteSpace(compound))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(compound => compound, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchedCompoundSlugs = matchedCompounds
            .Select(ToSlug)
            .ToList();

        var compoundLabel = string.Join(" + ", matchedCompounds);
        var patternKind = result.Type switch
        {
            BioStack.Domain.Enums.InteractionType.Complementary => "Complementary Pairing",
            BioStack.Domain.Enums.InteractionType.Synergistic => "Synergistic Pairing",
            BioStack.Domain.Enums.InteractionType.Redundant => "Redundancy Pattern",
            BioStack.Domain.Enums.InteractionType.Interfering => "Interference Pattern",
            _ => "Interaction Pattern",
        };

        return new KnownPatternResponse(
            PatternId: $"{string.Join("-", matchedCompoundSlugs)}-{result.Type.ToString().ToLowerInvariant()}",
            Name: $"{compoundLabel} {patternKind}",
            MatchedCompoundSlugs: matchedCompoundSlugs,
            Description: result.Reason);
    }

    private static List<EmergentPatternResponse> DeriveEmergentPatterns(
        string goal,
        IReadOnlyList<KnowledgeEntry> knownEntries,
        InteractionIntelligenceResponse interactionIntelligence)
    {
        var descriptors = knownEntries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.CanonicalName))
            .GroupBy(entry => entry.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .Select(group => ToCompoundDescriptor(group.First()))
            .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (descriptors.Count < 2)
        {
            return [];
        }

        var descriptorsByName = descriptors.ToDictionary(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase);
        var descriptorsBySlug = descriptors.ToDictionary(descriptor => descriptor.Slug, StringComparer.OrdinalIgnoreCase);
        var patterns = new List<EmergentPatternResponse>();

        patterns.AddRange(DeriveInferredPairPatterns(interactionIntelligence, descriptorsByName));
        patterns.AddRange(DeriveSharedPathwayMotifs(descriptors));
        patterns.AddRange(DeriveFamilyClusters(goal, descriptors));
        patterns.AddRange(DeriveRedundancyClusters(interactionIntelligence, descriptorsBySlug));

        var complexityLoad = TryDeriveComplexityLoad(descriptors, interactionIntelligence);
        if (complexityLoad is not null)
        {
            patterns.Add(complexityLoad);
        }

        var evidenceMismatch = TryDeriveEvidenceMismatch(descriptors);
        if (evidenceMismatch is not null)
        {
            patterns.Add(evidenceMismatch);
        }

        return patterns
            .GroupBy(pattern => pattern.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(pattern => ConfidenceRank(pattern.Confidence))
            .ThenBy(pattern => PatternTypeRank(pattern.PatternType))
            .ThenBy(pattern => pattern.Title, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static List<EmergentPatternResponse> DeriveInferredPairPatterns(
        InteractionIntelligenceResponse interactionIntelligence,
        IReadOnlyDictionary<string, CompoundPatternDescriptor> descriptorsByName)
    {
        return interactionIntelligence.Interactions
            .Where(result => !result.HintBacked)
            .Where(result => result.Type != BioStack.Domain.Enums.InteractionType.Neutral)
            .Where(result => result.Confidence >= 0.70d)
            .Select(result => ToEmergentPairPattern(result, descriptorsByName))
            .Where(pattern => pattern is not null)
            .Cast<EmergentPatternResponse>()
            .ToList();
    }

    private static EmergentPatternResponse? ToEmergentPairPattern(
        InteractionResultResponse result,
        IReadOnlyDictionary<string, CompoundPatternDescriptor> descriptorsByName)
    {
        var compoundNames = new[] { result.CompoundA, result.CompoundB }
            .Where(compound => !string.IsNullOrWhiteSpace(compound))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(compound => compound, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (compoundNames.Count < 2)
        {
            return null;
        }

        var matchedDescriptors = compoundNames
            .Where(descriptorsByName.ContainsKey)
            .Select(name => descriptorsByName[name])
            .ToList();

        var compoundSlugs = matchedDescriptors.Count > 0
            ? matchedDescriptors.Select(descriptor => descriptor.Slug).ToList()
            : compoundNames.Select(ToSlug).ToList();

        var basis = result.Type switch
        {
            BioStack.Domain.Enums.InteractionType.Redundant => "mechanism-redundancy",
            BioStack.Domain.Enums.InteractionType.Interfering => "risk-amplification",
            _ => "mechanism-complementarity"
        };

        var title = result.Type switch
        {
            BioStack.Domain.Enums.InteractionType.Redundant => $"{string.Join(" + ", compoundNames)} inferred overlap pattern",
            BioStack.Domain.Enums.InteractionType.Interfering => $"{string.Join(" + ", compoundNames)} inferred interaction signal",
            _ => $"{string.Join(" + ", compoundNames)} inferred complementary pair"
        };

        var explanation = result.Type switch
        {
            BioStack.Domain.Enums.InteractionType.Redundant =>
                $"{string.Join(" and ", compoundNames)} appear to touch substantially similar mechanisms in this stack. That may indicate an overlap pattern worth tracking for attribution clarity.",
            BioStack.Domain.Enums.InteractionType.Interfering =>
                $"{string.Join(" and ", compoundNames)} appear to push on adjacent systems in a way that may indicate a review-worthy pair pattern, inferred from this stack rather than curated memory.",
            _ =>
                $"{string.Join(" and ", compoundNames)} appear to converge on adjacent signaling in this stack. That may indicate a complementary pair pattern worth tracking, inferred from this stack rather than BioStack memory."
        };

        return new EmergentPatternResponse(
            Id: $"emergent-pair-{string.Join("-", compoundSlugs)}-{ToSlug(result.Type.ToString())}",
            Title: title,
            PatternType: "pair",
            Compounds: compoundSlugs,
            Pathways: result.SharedPathways
                .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList(),
            Mechanisms: SelectMechanisms(matchedDescriptors, 3),
            Confidence: result.Confidence >= 0.82d ? "high" : "moderate",
            Basis: basis,
            Explanation: explanation,
            EvidenceNotes: BuildEvidenceNotes(
                matchedDescriptors,
                result.SharedPathways.Count > 0
                    ? [$"Observed shared pathway tags: {string.Join(", ", result.SharedPathways.Take(3))}."]
                    : [$"Interaction confidence exceeded the inference threshold for this stack."]),
            UserFacingLabel: "Inferred from this stack · not canonical");
    }

    private static List<EmergentPatternResponse> DeriveSharedPathwayMotifs(IReadOnlyList<CompoundPatternDescriptor> descriptors)
    {
        return descriptors
            .SelectMany(descriptor => descriptor.Pathways.Select(pathway => new PathwayMembership(pathway.Trim().ToLowerInvariant(), pathway.Trim(), descriptor)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var compounds = group
                    .Select(item => item.Descriptor)
                    .DistinctBy(item => item.Slug, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (compounds.Count < 3)
                {
                    return null;
                }

                var displayPathway = group.Select(item => item.Display).First();
                return new EmergentPatternResponse(
                    Id: $"emergent-shared-pathway-{ToSlug(displayPathway)}",
                    Title: $"Shared pathway motif · {displayPathway}",
                    PatternType: "motif",
                    Compounds: compounds.Select(compound => compound.Slug).ToList(),
                    Pathways: [displayPathway],
                    Mechanisms: SelectMechanisms(compounds, 4),
                    Confidence: compounds.Count >= 4 ? "high" : "moderate",
                    Basis: "pathway-overlap",
                    Explanation: $"Three or more compounds appear to converge on {displayPathway}, inferred from overlapping pathway tags in this stack. That may indicate a motif worth tracking when the stack changes.",
                    EvidenceNotes: BuildEvidenceNotes(compounds, [$"Shared pathway tag observed across {compounds.Count} compounds: {displayPathway}."]),
                    UserFacingLabel: "Inferred from this stack · not canonical");
            })
            .Where(pattern => pattern is not null)
            .Cast<EmergentPatternResponse>()
            .ToList();
    }

    private static List<EmergentPatternResponse> DeriveFamilyClusters(string goal, IReadOnlyList<CompoundPatternDescriptor> descriptors)
    {
        var patterns = new List<EmergentPatternResponse>();

        foreach (var rule in EmergentFamilyRules)
        {
            if (rule.GoalKeywords.Count > 0 && !ContainsAnyPhrase(goal, rule.GoalKeywords))
            {
                continue;
            }

            var matched = descriptors
                .Where(descriptor => ContainsAnyPhrase(descriptor.SearchCorpus, rule.SignalKeywords))
                .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matched.Count < rule.MinCompoundCount || !HasSharedSignal(matched))
            {
                continue;
            }

            var pathways = CollectRelevantPathways(matched, rule.SignalKeywords, 4);
            var explanation = rule.Id switch
            {
                "repair-cluster" =>
                    "These compounds appear to converge on tissue-repair signaling, inferred from this stack's pathway and mechanism tags. That may make the cluster easier to reason about, but changes inside it should be tracked carefully.",
                "metabolic-support" =>
                    "These compounds appear to cluster around mitochondrial or metabolic support. That may indicate a coordinated support motif inferred from this stack.",
                "gh-axis" =>
                    "These compounds appear to touch adjacent GH-axis or growth-signal pathways. That may indicate a shared signaling motif worth tracking.",
                "incretin-overlap" =>
                    "These compounds appear to converge on incretin-related signaling. That may indicate overlapping appetite or glucose-control mechanisms in this stack.",
                _ => "These compounds appear to form a recurring motif inferred from this stack."
            };

            patterns.Add(new EmergentPatternResponse(
                Id: $"emergent-{rule.Id}-{string.Join("-", matched.Select(compound => compound.Slug))}",
                Title: rule.Title,
                PatternType: rule.PatternType,
                Compounds: matched.Select(compound => compound.Slug).ToList(),
                Pathways: pathways,
                Mechanisms: SelectMechanisms(matched, 4),
                Confidence: matched.Count >= 3 ? "high" : "moderate",
                Basis: rule.Basis,
                Explanation: explanation,
                EvidenceNotes: BuildEvidenceNotes(matched, [$"Cluster signal was inferred from pathway/mechanism tags associated with this stack."]),
                UserFacingLabel: "Inferred from this stack · not canonical"));
        }

        return patterns;
    }

    private static List<EmergentPatternResponse> DeriveRedundancyClusters(
        InteractionIntelligenceResponse interactionIntelligence,
        IReadOnlyDictionary<string, CompoundPatternDescriptor> descriptorsBySlug)
    {
        var redundantInteractions = interactionIntelligence.Interactions
            .Where(result => result.Type == BioStack.Domain.Enums.InteractionType.Redundant)
            .Where(result => result.Confidence >= 0.55d)
            .ToList();

        if (redundantInteractions.Count < 2)
        {
            return [];
        }

        var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var interaction in redundantInteractions)
        {
            var left = ToSlug(interaction.CompoundA);
            var right = ToSlug(interaction.CompoundB);
            if (!adjacency.TryGetValue(left, out var leftNeighbors))
            {
                leftNeighbors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                adjacency[left] = leftNeighbors;
            }

            if (!adjacency.TryGetValue(right, out var rightNeighbors))
            {
                rightNeighbors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                adjacency[right] = rightNeighbors;
            }

            leftNeighbors.Add(right);
            rightNeighbors.Add(left);
        }

        return FindConnectedComponents(adjacency)
            .Where(component => component.Count >= 3)
            .Select(component =>
            {
                var matched = component
                    .Where(descriptorsBySlug.ContainsKey)
                    .Select(slug => descriptorsBySlug[slug])
                    .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (matched.Count < 3)
                {
                    return null;
                }

                var pathways = redundantInteractions
                    .Where(result => component.Contains(ToSlug(result.CompoundA)) && component.Contains(ToSlug(result.CompoundB)))
                    .SelectMany(result => result.SharedPathways)
                    .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
                    .Take(4)
                    .ToList();

                return new EmergentPatternResponse(
                    Id: $"emergent-redundancy-{string.Join("-", matched.Select(compound => compound.Slug))}",
                    Title: "Redundancy cluster",
                    PatternType: "redundancy-cluster",
                    Compounds: matched.Select(compound => compound.Slug).ToList(),
                    Pathways: pathways,
                    Mechanisms: SelectMechanisms(matched, 4),
                    Confidence: matched.Count >= 4 ? "high" : "moderate",
                    Basis: "mechanism-redundancy",
                    Explanation: "Several compounds appear to touch substantially similar mechanisms in this stack. That may make attribution harder to interpret, which suggests a redundancy cluster worth tracking.",
                    EvidenceNotes: BuildEvidenceNotes(matched, [$"Multiple redundant pair signals were observed across this cluster."]),
                    UserFacingLabel: "Inferred from this stack · not canonical");
            })
            .Where(pattern => pattern is not null)
            .Cast<EmergentPatternResponse>()
            .ToList();
    }

    private static EmergentPatternResponse? TryDeriveComplexityLoad(
        IReadOnlyList<CompoundPatternDescriptor> descriptors,
        InteractionIntelligenceResponse interactionIntelligence)
    {
        if (descriptors.Count < 5)
        {
            return null;
        }

        var involved = interactionIntelligence.Interactions
            .Where(result => result.Type != BioStack.Domain.Enums.InteractionType.Neutral || result.SharedPathways.Count > 0)
            .Where(result => result.Confidence >= 0.55d || result.SharedPathways.Count > 0)
            .SelectMany(result => new[] { ToSlug(result.CompoundA), ToSlug(result.CompoundB) })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (involved.Count < 5)
        {
            return null;
        }

        var matched = descriptors
            .Where(descriptor => involved.Contains(descriptor.Slug))
            .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var distinctPathways = matched
            .SelectMany(descriptor => descriptor.Pathways)
            .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctPathways.Count < 4)
        {
            return null;
        }

        return new EmergentPatternResponse(
            Id: $"emergent-complexity-load-{matched.Count}",
            Title: "Complexity load cluster",
            PatternType: "complexity-load",
            Compounds: matched.Select(compound => compound.Slug).ToList(),
            Pathways: distinctPathways.Take(5).ToList(),
            Mechanisms: SelectMechanisms(matched, 5),
            Confidence: matched.Count >= 7 ? "high" : "moderate",
            Basis: "complexity-load",
            Explanation: "Several compounds appear to touch adjacent systems at once. That may make the stack harder to attribute cleanly, which suggests a complexity-load pattern worth tracking.",
            EvidenceNotes: BuildEvidenceNotes(matched, [$"This signal was triggered by the number of adjacent interaction/pathway touches across the stack."]),
            UserFacingLabel: "Inferred from this stack · not canonical");
    }

    private static EmergentPatternResponse? TryDeriveEvidenceMismatch(IReadOnlyList<CompoundPatternDescriptor> descriptors)
    {
        if (descriptors.Count < 4)
        {
            return null;
        }

        var lowEvidence = descriptors
            .Where(descriptor => IsLowEvidence(descriptor.EvidenceTier))
            .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (lowEvidence.Count < 3 || lowEvidence.Count * 10 < descriptors.Count * 6)
        {
            return null;
        }

        return new EmergentPatternResponse(
            Id: $"emergent-evidence-mismatch-{string.Join("-", lowEvidence.Select(compound => compound.Slug))}",
            Title: "Evidence-mismatch cluster",
            PatternType: "risk-cluster",
            Compounds: lowEvidence.Select(compound => compound.Slug).ToList(),
            Pathways: lowEvidence
                .SelectMany(descriptor => descriptor.Pathways)
                .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
                .Take(4)
                .ToList(),
            Mechanisms: SelectMechanisms(lowEvidence, 4),
            Confidence: lowEvidence.Count * 10 >= descriptors.Count * 8 ? "high" : "moderate",
            Basis: "evidence-mismatch",
            Explanation: "A large share of this stack appears to lean on limited-evidence entries. That may make outcome attribution harder to interpret, which suggests a pattern worth tracking carefully.",
            EvidenceNotes: BuildEvidenceNotes(lowEvidence, [$"Limited, mechanistic, or unknown evidence tiers dominate this cluster."]),
            UserFacingLabel: "Inferred from this stack · not canonical");
    }

    private static CompoundPatternDescriptor ToCompoundDescriptor(KnowledgeEntry entry)
    {
        var pathways = entry.Pathways
            .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
            .Select(pathway => pathway.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var benefits = entry.Benefits
            .Where(benefit => !string.IsNullOrWhiteSpace(benefit))
            .Select(benefit => benefit.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(benefit => benefit, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var searchCorpus = string.Join(
            " | ",
            new[]
            {
                entry.CanonicalName,
                entry.MechanismSummary,
                entry.Notes,
                string.Join(" ", pathways),
                string.Join(" ", benefits)
            }
            .Where(value => !string.IsNullOrWhiteSpace(value)))
            .ToLowerInvariant();

        return new CompoundPatternDescriptor(
            Slug: ToSlug(entry.CanonicalName),
            DisplayName: entry.CanonicalName,
            EvidenceTier: entry.EvidenceTier,
            Pathways: pathways,
            MechanismFamilies: ExtractMechanismFamilies(searchCorpus, entry.Classification),
            Benefits: benefits,
            SearchCorpus: searchCorpus);
    }

    private static List<string> ExtractMechanismFamilies(string searchCorpus, BioStack.Domain.Enums.CompoundCategory classification)
    {
        var families = new List<string>();

        if (ContainsAnyPhrase(searchCorpus, ["repair", "healing", "wound", "regeneration", "collagen"]))
        {
            families.Add("tissue-repair signaling");
        }

        if (ContainsAnyPhrase(searchCorpus, ["angiogenesis", "vascular", "blood flow"]))
        {
            families.Add("angiogenic support");
        }

        if (ContainsAnyPhrase(searchCorpus, ["mitochond", "energy", "atp", "nad"]))
        {
            families.Add("mitochondrial support");
        }

        if (ContainsAnyPhrase(searchCorpus, ["metabolic", "glucose", "insulin", "ampk", "oxidation"]))
        {
            families.Add("metabolic regulation");
        }

        if (ContainsAnyPhrase(searchCorpus, ["growth hormone", "growth-signal", "igf", "growth factor", "anabolic"]))
        {
            families.Add("growth signaling");
        }

        if (ContainsAnyPhrase(searchCorpus, ["glp-1", "gip", "incretin", "appetite", "gastric emptying"]))
        {
            families.Add("incretin signaling");
        }

        if (families.Count == 0 && classification != BioStack.Domain.Enums.CompoundCategory.Unknown)
        {
            families.Add(classification.ToString());
        }

        return families
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> SelectMechanisms(IReadOnlyList<CompoundPatternDescriptor> descriptors, int maxCount)
    {
        var mechanisms = descriptors
            .SelectMany(descriptor => descriptor.MechanismFamilies)
            .Where(mechanism => !string.IsNullOrWhiteSpace(mechanism))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(mechanism => mechanism, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToList();

        if (mechanisms.Count > 0)
        {
            return mechanisms;
        }

        return descriptors
            .SelectMany(descriptor => descriptor.Pathways)
            .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToList();
    }

    private static List<string> CollectRelevantPathways(
        IReadOnlyList<CompoundPatternDescriptor> descriptors,
        IReadOnlyList<string> keywords,
        int maxCount)
    {
        var preferred = descriptors
            .SelectMany(descriptor => descriptor.Pathways)
            .Where(pathway => ContainsAnyPhrase(pathway, keywords))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .ToList();

        return preferred.Count > 0
            ? preferred
            : descriptors
                .SelectMany(descriptor => descriptor.Pathways)
                .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();
    }

    private static List<string> BuildEvidenceNotes(
        IReadOnlyList<CompoundPatternDescriptor> descriptors,
        IReadOnlyList<string> additionalNotes)
    {
        var moderateOrStrong = descriptors.Count(descriptor => descriptor.EvidenceTier is BioStack.Domain.Enums.EvidenceTier.Moderate or BioStack.Domain.Enums.EvidenceTier.Strong);
        var lowEvidence = descriptors.Count(descriptor => IsLowEvidence(descriptor.EvidenceTier));
        var lowEvidenceNames = descriptors
            .Where(descriptor => IsLowEvidence(descriptor.EvidenceTier))
            .Select(descriptor => descriptor.DisplayName)
            .Take(3)
            .ToList();

        var notes = new List<string>
        {
            $"Evidence mix in this pattern: {moderateOrStrong} moderate/strong and {lowEvidence} limited/mechanistic entries."
        };

        if (lowEvidenceNames.Count > 0)
        {
            notes.Add($"Lower-evidence entries in this cluster: {string.Join(", ", lowEvidenceNames)}.");
        }

        notes.AddRange(additionalNotes.Where(note => !string.IsNullOrWhiteSpace(note)));

        return notes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private static bool HasSharedSignal(IReadOnlyList<CompoundPatternDescriptor> descriptors)
    {
        for (var i = 0; i < descriptors.Count; i++)
        {
            for (var j = i + 1; j < descriptors.Count; j++)
            {
                if (descriptors[i].Pathways.Intersect(descriptors[j].Pathways, StringComparer.OrdinalIgnoreCase).Any())
                {
                    return true;
                }

                if (descriptors[i].MechanismFamilies.Intersect(descriptors[j].MechanismFamilies, StringComparer.OrdinalIgnoreCase).Any())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<HashSet<string>> FindConnectedComponents(IReadOnlyDictionary<string, HashSet<string>> adjacency)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var components = new List<HashSet<string>>();

        foreach (var node in adjacency.Keys)
        {
            if (!visited.Add(node))
            {
                continue;
            }

            var component = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            queue.Enqueue(node);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                component.Add(current);

                if (!adjacency.TryGetValue(current, out var neighbors))
                {
                    continue;
                }

                foreach (var neighbor in neighbors)
                {
                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            components.Add(component);
        }

        return components;
    }

    private static bool IsLowEvidence(BioStack.Domain.Enums.EvidenceTier evidenceTier)
    {
        return evidenceTier is BioStack.Domain.Enums.EvidenceTier.Unknown
            or BioStack.Domain.Enums.EvidenceTier.Limited
            or BioStack.Domain.Enums.EvidenceTier.Mechanistic;
    }

    private static bool ContainsAnyPhrase(string source, IReadOnlyList<string> phrases)
    {
        if (string.IsNullOrWhiteSpace(source) || phrases.Count == 0)
        {
            return false;
        }

        return phrases.Any(phrase => source.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static int ConfidenceRank(string confidence)
    {
        return confidence switch
        {
            "high" => 3,
            "moderate" => 2,
            "low" => 1,
            _ => 0
        };
    }

    private static int PatternTypeRank(string patternType)
    {
        return patternType switch
        {
            "support-cluster" => 0,
            "motif" => 1,
            "pair" => 2,
            "redundancy-cluster" => 3,
            "risk-cluster" => 4,
            "complexity-load" => 5,
            _ => 6
        };
    }

    private async Task<StackReviewBoardResponse?> TryBuildStackReviewBoardAsync(
        AnalyzeProtocolRequest request,
        ProtocolParseResult parseResult,
        ProtocolAnalysisCacheDto analysis,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(StackReviewBoardTimeout);

            var envelope = BuildStackDeliberationEnvelope(request, parseResult, analysis);
            var review = await _stackReviewBoardService.ReviewStackAsync(envelope, timeoutCts.Token);
            return MapStackReviewBoard(review);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Stack Review Board timed out after {TimeoutMs}ms; returning deterministic analyzer response without SRB payload.", StackReviewBoardTimeout.TotalMilliseconds);
            return null;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Stack Review Board review failed; returning deterministic analyzer response without SRB payload.");
            return null;
        }
    }

    private static StackDeliberationEnvelope BuildStackDeliberationEnvelope(
        AnalyzeProtocolRequest request,
        ProtocolParseResult parseResult,
        ProtocolAnalysisCacheDto analysis)
    {
        var compounds = parseResult.Entries
            .Select(entry =>
            {
                parseResult.KnowledgeByCompound.TryGetValue(entry.CompoundName, out var knowledge);
                return new CompoundRef(
                    Slug: ToSlug(entry.CompoundName),
                    DisplayName: entry.CompoundName,
                    Form: InferForm(entry, knowledge),
                    Category: knowledge?.Classification.ToString() ?? "Unknown");
            })
            .ToList();

        var pathways = parseResult.KnowledgeByCompound.Values
            .SelectMany(entry => entry.Pathways)
            .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(pathway => pathway, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var evidenceTiers = parseResult.KnowledgeByCompound
            .ToDictionary(
                pair => ToSlug(pair.Key),
                pair => MapEvidenceTier(pair.Value.EvidenceTier),
                StringComparer.OrdinalIgnoreCase);

        var deterministicFindings = analysis.Issues
            .Take(5)
            .Select((issue, index) =>
            {
                var compoundSlugs = issue.Compounds
                    .Where(compound => !string.IsNullOrWhiteSpace(compound))
                    .Select(ToSlug)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var pathwayTags = compoundSlugs
                    .SelectMany(slug =>
                    {
                        var match = parseResult.KnowledgeByCompound.FirstOrDefault(pair => string.Equals(ToSlug(pair.Key), slug, StringComparison.OrdinalIgnoreCase));
                        return match.Value?.Pathways ?? [];
                    })
                    .Where(pathway => !string.IsNullOrWhiteSpace(pathway))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new DeterministicFinding(
                    FindingId: $"issue-{index + 1}",
                    Code: ToFindingCode(issue.Type, index),
                    Category: issue.Type,
                    Narrative: issue.Message,
                    CompoundSlugs: compoundSlugs,
                    PathwayTags: pathwayTags,
                    RiskScoreContribution: ToRiskContribution(issue.Type),
                    UtilityScoreContribution: 0m,
                    EvidenceTier: ResolveEvidenceTier(issue.Compounds, parseResult.KnowledgeByCompound),
                    QualifiesFindingId: null,
                    ConflictsWithFindingId: null);
            })
            .ToList();

        var missingInputs = new List<string>();
        if (parseResult.Entries.Any(entry => entry.Dose <= 0))
        {
            missingInputs.Add("dose");
        }

        if (parseResult.Entries.Any(entry => string.IsNullOrWhiteSpace(entry.Frequency)))
        {
            missingInputs.Add("frequency");
        }

        if (string.IsNullOrWhiteSpace(request.Goal))
        {
            missingInputs.Add("goal");
        }

        return new StackDeliberationEnvelope(
            Goal: string.IsNullOrWhiteSpace(request.Goal) ? "general wellness" : request.Goal,
            Compounds: compounds,
            Pathways: pathways,
            EvidenceTiers: evidenceTiers,
            DeterministicFindings: deterministicFindings,
            KnownPatterns: analysis.KnownPatterns
                .Select(pattern => new KnownPattern(
                    pattern.PatternId,
                    pattern.Name,
                    pattern.MatchedCompoundSlugs,
                    pattern.Description))
                .ToList(),
            EmergentPatterns: analysis.EmergentPatterns
                .Select(pattern => new EmergentPattern(
                    pattern.Id,
                    pattern.Title,
                    pattern.PatternType,
                    pattern.Compounds,
                    pattern.Pathways,
                    pattern.Mechanisms,
                    pattern.Confidence,
                    pattern.Basis,
                    pattern.Explanation,
                    pattern.EvidenceNotes,
                    pattern.UserFacingLabel))
                .ToList(),
            MissingInputs: missingInputs,
            ProviderReviewPressure: compounds.Any(compound => string.Equals(compound.Category, "Peptide", StringComparison.OrdinalIgnoreCase)) ? 0.5m : 0m,
            SafetyBoundaryText: "educational and observational only");
    }

    private static StackReviewBoardResponse MapStackReviewBoard(CognitiveDensityEnvelope review)
    {
        return new StackReviewBoardResponse(
            review.BranchPerspectiveReview.PerspectiveReviews.ToDictionary(
                pair => pair.Key.ToString(),
                pair => new PerspectiveReviewResponse(
                    pair.Value.Kind.ToString(),
                    pair.Value.Findings
                        .Select(finding => new PerspectiveFindingResponse(
                            finding.FindingId,
                            finding.Category,
                            finding.Narrative,
                            finding.Severity.ToString()))
                        .ToList(),
                    pair.Value.Summary),
                StringComparer.OrdinalIgnoreCase),
            new ContradictionReviewResponse(
                review.ContradictionReview.CounterPlanNarrative,
                review.ContradictionReview.CounterPlanIsExecutable,
                review.ContradictionReview.IsExecutable),
            new ConfidenceProfileResponse(
                review.ConfidenceProfile.Model,
                review.ConfidenceProfile.Epistemic,
                review.ConfidenceProfile.EvidenceSupport,
                review.ConfidenceProfile.ContradictionDensity,
                review.ConfidenceProfile.CalibrationVersion),
            new ReasoningGraphRefResponse(
                review.ReasoningGraphRef.GraphId,
                review.ReasoningGraphRef.NodeCount,
                review.ReasoningGraphRef.EdgeCount));
    }

    private static EvidenceTier ResolveEvidenceTier(
        IEnumerable<string> compounds,
        IReadOnlyDictionary<string, KnowledgeEntry> knowledgeByCompound)
    {
        var mapped = compounds
            .Where(compound => knowledgeByCompound.TryGetValue(compound, out _))
            .Select(compound => MapEvidenceTier(knowledgeByCompound[compound].EvidenceTier))
            .ToList();

        return mapped.Count == 0 ? EvidenceTier.None : mapped.Max();
    }

    private static EvidenceTier MapEvidenceTier(BioStack.Domain.Enums.EvidenceTier evidenceTier)
    {
        return evidenceTier switch
        {
            BioStack.Domain.Enums.EvidenceTier.Strong => EvidenceTier.Strong,
            BioStack.Domain.Enums.EvidenceTier.Moderate => EvidenceTier.Moderate,
            BioStack.Domain.Enums.EvidenceTier.Limited => EvidenceTier.Limited,
            BioStack.Domain.Enums.EvidenceTier.Mechanistic => EvidenceTier.Limited,
            _ => EvidenceTier.None,
        };
    }

    private static string ToSlug(string value)
    {
        return string.Concat(
            value.Trim().ToLowerInvariant().Select(character =>
                char.IsLetterOrDigit(character) ? character : '-'))
            .Trim('-');
    }

    private static string InferForm(ProtocolEntryResponse entry, KnowledgeEntry? knowledge)
    {
        if (entry.Unit.Contains("mg", StringComparison.OrdinalIgnoreCase) || entry.Unit.Contains("mcg", StringComparison.OrdinalIgnoreCase))
        {
            return "Injectable";
        }

        return string.IsNullOrWhiteSpace(knowledge?.RecommendedDosage) ? "Unknown" : "Oral";
    }

    private static string ToFindingCode(string issueType, int index)
    {
        var prefix = issueType switch
        {
            "redundancy" => "RED",
            "overlap" => "OVR",
            "inefficiency" => "INE",
            "excessive_compounds" => "CMP",
            _ => "ANL",
        };

        return $"{prefix}-{index + 1:000}";
    }

    private static decimal ToRiskContribution(string issueType)
    {
        return issueType switch
        {
            "overlap" => 0.15m,
            "redundancy" => 0.10m,
            "excessive_compounds" => 0.08m,
            "inefficiency" => 0.05m,
            _ => 0.03m,
        };
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

    private sealed record CompoundPatternDescriptor(
        string Slug,
        string DisplayName,
        BioStack.Domain.Enums.EvidenceTier EvidenceTier,
        IReadOnlyList<string> Pathways,
        IReadOnlyList<string> MechanismFamilies,
        IReadOnlyList<string> Benefits,
        string SearchCorpus);

    private sealed record EmergentFamilyRule(
        string Id,
        string Title,
        string PatternType,
        string Basis,
        int MinCompoundCount,
        IReadOnlyList<string> SignalKeywords,
        IReadOnlyList<string> GoalKeywords);

    private sealed record PathwayMembership(
        string Key,
        string Display,
        CompoundPatternDescriptor Descriptor);
}

public interface IProtocolAnalyzerService
{
    Task<AnalyzeProtocolResponse> AnalyzeAsync(AnalyzeProtocolRequest request, CancellationToken cancellationToken = default);
    Task<AnalyzeProtocolResponse> AnalyzeAsync(AnalyzeProtocolRequest request, ProtocolIngestionRequest ingestionRequest, CancellationToken cancellationToken = default);
}
