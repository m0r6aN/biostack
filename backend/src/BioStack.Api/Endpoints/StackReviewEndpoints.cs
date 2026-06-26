namespace BioStack.Api.Endpoints;

using BioStack.Application.Governance;
using BioStack.Application.Services;
using BioStack.Cognition;
using BioStack.Cognition.Models;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
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
        IRuntimeReceiptFactory receipts,
        ICurrentUserAccessor currentUser,
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

        // Issue a Decision Receipt — the Stack Review Board deliberated over the stack. The
        // receipt binds to each reviewed compound (evidence) and the reasoning-graph artifact,
        // proving governed reasoning before the commentary is surfaced. Slugs are the most
        // stable identifier available on a client-supplied payload (no knowledge-entry id).
        var evidenceRefs = envelope.Compounds
            .Select(c => ReceiptRefs.Compound(c.Slug))
            .Append(ReceiptRefs.CompoundGraph(cognitiveEnvelope.ReasoningGraphRef.GraphId))
            .ToList();

        await receipts.IssueAndAppendAsync(new ReceiptContext(
            ReceiptClass: ReceiptClass.DeliberationStackReviewCompleted,
            SubjectUri: $"stack-review:{cognitiveEnvelope.ReasoningGraphRef.GraphId}",
            Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
            EvidenceRefs: evidenceRefs,
            Decision: "commentary-only",
            EffectStatus: "commentary-only",
            InputHashSeed: cognitiveEnvelope.ReasoningGraphRef.GraphId), ct);

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

        var graphRef = new ReasoningGraphRefResponse(
            cognitive.ReasoningGraphRef.GraphId,
            cognitive.ReasoningGraphRef.NodeCount,
            cognitive.ReasoningGraphRef.EdgeCount);

        var witnessNarrative = BuildWitnessNarrative(cognitive, sanitizer);
        var reasoningGraphFull = BuildReasoningGraph(envelope, cognitive);

        return new StackDeliberationEnvelopeResponse(
            DeterministicFindings: deterministicResponse,
            PerspectiveReviews: perspectiveResponses,
            ContradictionReview: contradiction,
            ConfidenceProfile: confidence,
            ReasoningGraph: graphRef,
            EffectStatus: commentaryOnly,
            WitnessNarrative: witnessNarrative,
            ReasoningGraphFull: reasoningGraphFull);
    }

    private static WitnessNarrativeResponse BuildWitnessNarrative(
        CognitiveDensityEnvelope cognitive,
        DoctrineSanitizer sanitizer)
    {
        var roleOrder = cognitive.BranchPerspectiveReview.PerspectiveReviews
            .OrderBy(kvp => kvp.Key.ToString())
            .ToList();

        var entries = new List<WitnessEntryResponse>();

        // Roles staggered: index 3 (oldest) to 0 (newest) so Optimizer is oldest
        int total = roleOrder.Count;
        for (int i = 0; i < total; i++)
        {
            var (role, review) = (roleOrder[i].Key, roleOrder[i].Value);
            int minutesBack = total - i; // index 0 => minutesBack=total (oldest), last => minutesBack=1
            bool hasFindings = review.Findings.Count > 0;

            entries.Add(new WitnessEntryResponse(
                Role: role.ToString(),
                EventType: hasFindings ? "proposed" : "blocked",
                Timestamp: DateTime.UtcNow.AddMinutes(-minutesBack).ToString("O"),
                Summary: hasFindings
                    ? sanitizer.SanitizeFinding(review.Summary)
                    : $"No findings were surfaced for the {role} perspective.",
                FindingIds: hasFindings
                    ? review.Findings.Select(f => f.FindingId).ToList()
                    : (IReadOnlyList<string>)[]));
        }

        // Contradiction entry at UtcNow (newest)
        entries.Add(new WitnessEntryResponse(
            Role: "Contradiction",
            EventType: "challenged",
            Timestamp: DateTime.UtcNow.ToString("O"),
            Summary: sanitizer.SanitizeFinding(cognitive.ContradictionReview.CounterPlanNarrative),
            FindingIds: []));

        // Sort chronologically oldest first
        entries.Sort((a, b) => string.Compare(a.Timestamp, b.Timestamp, StringComparison.Ordinal));

        return new WitnessNarrativeResponse(entries);
    }

    private static ReasoningGraphResponse BuildReasoningGraph(
        StackDeliberationEnvelope envelope,
        CognitiveDensityEnvelope cognitive)
    {
        var nodes = new List<GraphNodeResponse>();
        var edges = new List<GraphEdgeResponse>();

        var intentNode = new GraphNodeResponse(
            Id: "intent-0",
            Kind: "decision",
            Label: envelope.Goal,
            RoleOrigin: null,
            EvidenceRefs: []);
        nodes.Add(intentNode);

        // Deterministic claim nodes
        foreach (var f in envelope.DeterministicFindings)
        {
            var nodeId = $"claim-{f.FindingId}";
            nodes.Add(new GraphNodeResponse(
                Id: nodeId,
                Kind: "claim",
                Label: f.Narrative[..Math.Min(80, f.Narrative.Length)],
                RoleOrigin: "deterministic",
                EvidenceRefs: [f.EvidenceTier.ToString()]));
            edges.Add(new GraphEdgeResponse(Source: "intent-0", Target: nodeId, Relation: "derives_from"));
        }

        // Perspective nodes
        foreach (var kvp in cognitive.BranchPerspectiveReview.PerspectiveReviews)
        {
            foreach (var f in kvp.Value.Findings)
            {
                var nodeId = $"perspective-{f.FindingId}";
                nodes.Add(new GraphNodeResponse(
                    Id: nodeId,
                    Kind: MapSeverityToKind(f.Severity.ToString()),
                    Label: f.Narrative[..Math.Min(80, f.Narrative.Length)],
                    RoleOrigin: kvp.Key.ToString(),
                    EvidenceRefs: []));
                edges.Add(new GraphEdgeResponse(Source: "intent-0", Target: nodeId, Relation: "derives_from"));
            }
        }

        // depends_on edges between deterministic findings sharing a compound slug
        var deterministicList = envelope.DeterministicFindings.ToList();
        for (int i = 0; i < deterministicList.Count; i++)
        {
            for (int j = i + 1; j < deterministicList.Count; j++)
            {
                var f1 = deterministicList[i];
                var f2 = deterministicList[j];
                bool sharesCompound = f1.CompoundSlugs.Any(s => f2.CompoundSlugs.Contains(s));
                if (sharesCompound)
                {
                    edges.Add(new GraphEdgeResponse(
                        Source: $"claim-{f1.FindingId}",
                        Target: $"claim-{f2.FindingId}",
                        Relation: "depends_on"));
                }
            }
        }

        return new ReasoningGraphResponse(
            GraphId: cognitive.ReasoningGraphRef.GraphId,
            Nodes: nodes,
            Edges: edges);
    }

    private static string MapSeverityToKind(string severity) => severity switch
    {
        "Critical" => "risk",
        "Warning"  => "assumption",
        _          => "claim"
    };
}
