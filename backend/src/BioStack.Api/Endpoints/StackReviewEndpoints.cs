namespace BioStack.Api.Endpoints;

using BioStack.Application.Governance;
using BioStack.Application.Services;
using BioStack.Cognition;
using BioStack.Cognition.Models;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using Keon.Collective;

public static class StackReviewEndpoints
{
    public static void MapStackReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/stack-review")
            .WithTags("StackReview")
            .RequireAuthorization();

        group.MapPost("/envelope", GenerateEnvelope)
            .WithName("GenerateStackDeliberationEnvelope");
    }

    private static async Task<IResult> GenerateEnvelope(
        StackReviewRequest request,
        IStackReviewBoardService srbService,
        IKnowledgeService knowledgeService,
        DoctrineSanitizer sanitizer,
        CancellationToken ct)
    {
        StackDeliberationEnvelope envelope;

        if (request.Payload is not null)
        {
            envelope = BuildEnvelopeFromPayload(request.Payload);
        }
        else if (request.ProtocolId.HasValue)
        {
            return Results.BadRequest(
                "ProtocolId resolution not yet implemented. Supply a Payload instead.");
        }
        else
        {
            return Results.BadRequest("Provide either ProtocolId or Payload.");
        }

        var cognitiveEnvelope = await srbService.ReviewStackAsync(envelope, ct);

        var response = MapToResponse(envelope, cognitiveEnvelope, sanitizer);
        return Results.Ok(response);
    }

    private static StackDeliberationEnvelope BuildEnvelopeFromPayload(
        StackReviewEnvelopePayload payload)
    {
        var compounds = payload.Compounds.Select(c => new CompoundRef(
            c.Slug, c.DisplayName, c.Form, c.Category)).ToList();

        var deterministic = payload.DeterministicFindings.Select(f => new DeterministicFinding(
            FindingId: Guid.NewGuid().ToString("N"),
            Code: f.Code,
            Category: f.Category,
            Narrative: f.Narrative,
            CompoundSlugs: f.CompoundSlugs,
            PathwayTags: [],
            RiskScoreContribution: f.RiskScoreContribution,
            UtilityScoreContribution: 0m,
            EvidenceTier: EvidenceTier.None,
            QualifiesFindingId: null,
            ConflictsWithFindingId: null)).ToList();

        var evidenceTiers = payload.Compounds.ToDictionary(
            c => c.Slug,
            c => Enum.TryParse<EvidenceTier>(c.EvidenceTier, true, out var t) ? t : EvidenceTier.None);

        var knownPatterns = payload.KnownPatternNames.Select(name =>
            new KnownPattern(Guid.NewGuid().ToString("N"), name, [], name)).ToList();

        return new StackDeliberationEnvelope(
            Goal: payload.Goal,
            Compounds: compounds,
            Pathways: payload.Pathways,
            EvidenceTiers: evidenceTiers,
            DeterministicFindings: deterministic,
            KnownPatterns: knownPatterns,
            MissingInputs: [],
            ProviderReviewPressure: payload.ProviderReviewPressure,
            SafetyBoundaryText: "This review is commentary only. Consult a healthcare provider.");
    }

    private static StackDeliberationEnvelopeResponse MapToResponse(
        StackDeliberationEnvelope envelope,
        CognitiveDensityEnvelope cognitive,
        DoctrineSanitizer sanitizer)
    {
        const string commentaryOnly = "commentary-only";

        var deterministicResponse = envelope.DeterministicFindings
            .Select(f => new DeterministicFindingResponse(
                FindingId: f.FindingId,
                Code: f.Code,
                Category: f.Category,
                Narrative: sanitizer.SanitizeFinding(f.Narrative),
                CompoundSlugs: f.CompoundSlugs,
                RiskScoreContribution: f.RiskScoreContribution,
                EvidenceTier: f.EvidenceTier.ToString(),
                EffectStatus: commentaryOnly))
            .ToList();

        var perspectiveResponses = cognitive.BranchPerspectiveReview.PerspectiveReviews
            .ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => new PerspectiveReviewResponse(
                    Role: kvp.Key.ToString(),
                    Findings: kvp.Value.Findings.Select(f => new PerspectiveFindingResponse(
                        FindingId: f.FindingId,
                        Category: f.Category,
                        Narrative: sanitizer.SanitizeFinding(f.Narrative),
                        Severity: f.Severity.ToString(),
                        EffectStatus: commentaryOnly)).ToList(),
                    Summary: sanitizer.SanitizeFinding(kvp.Value.Summary),
                    EffectStatus: commentaryOnly));

        var contradiction = new ContradictionReviewResponse(
            CounterPlanNarrative: sanitizer.SanitizeFinding(
                cognitive.ContradictionReview.CounterPlanNarrative),
            IsExecutable: false,
            EffectStatus: commentaryOnly);

        var profile = cognitive.ConfidenceProfile;
        var confidence = new ConfidenceProfileResponse(
            profile.Model, profile.Epistemic, profile.EvidenceSupport,
            profile.ContradictionDensity, profile.CalibrationVersion);

        var graph = new ReasoningGraphRefResponse(
            cognitive.ReasoningGraphRef.GraphId,
            cognitive.ReasoningGraphRef.NodeCount,
            cognitive.ReasoningGraphRef.EdgeCount);

        return new StackDeliberationEnvelopeResponse(
            DeterministicFindings: deterministicResponse,
            PerspectiveReviews: perspectiveResponses,
            ContradictionReview: contradiction,
            ConfidenceProfile: confidence,
            ReasoningGraph: graph,
            EffectStatus: commentaryOnly);
    }
}
