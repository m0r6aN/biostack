namespace BioStack.Api.Endpoints;

using BioStack.Application.Governance;
using BioStack.Application.Services;
using BioStack.Cognition;
using BioStack.Cognition.Models;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
using Keon.Collective;

/// <summary>
/// Stack Review Board deliberation surface. Every user-facing narrative the board produces
/// (deterministic findings, perspective findings/summaries, the contradiction counter-plan, the
/// witness narrative, and reasoning-graph node labels) is routed through the central Lane H
/// <see cref="IUserFacingIntelligenceGate"/> before serialization — replacing the prior direct
/// <see cref="DoctrineSanitizer"/> calls so doctrine constraint, high-risk warning-first framing,
/// unsafe-goal refusal, and safety receipts are applied consistently with the rest of the product.
/// The <see cref="DoctrineSanitizer"/> is no longer the final user-facing decision layer here; it
/// survives only as the lower-level phrase detector the gate composes internally.
/// </summary>
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
        IUserFacingIntelligenceGate gate,
        IRuntimeReceiptFactory receipts,
        ICurrentUserAccessor currentUser,
        CancellationToken ct)
    {
        if (request.Payload is null)
        {
            return Results.BadRequest("Provide Payload.");
        }

        var envelope = BuildEnvelopeFromPayload(request.Payload);

        var cognitiveEnvelope = await srbService.ReviewStackAsync(envelope, ct);

        var subject = $"stack-review:{cognitiveEnvelope.ReasoningGraphRef.GraphId}";

        // Evidence chain bound to every reviewed compound and the reasoning-graph artifact. Shared by
        // both the deliberation receipt and the safety gate so any safety receipt the gate issues
        // preserves the same provenance. Slugs are the most stable identifier on a client payload.
        var evidenceRefs = envelope.Compounds
            .Select(c => ReceiptRefs.Compound(c.Slug))
            .Append(ReceiptRefs.CompoundGraph(cognitiveEnvelope.ReasoningGraphRef.GraphId))
            .ToList();

        // Issue a Decision Receipt — the Stack Review Board deliberated over the stack. The receipt
        // proves governed reasoning before the commentary is surfaced.
        await receipts.IssueAndAppendAsync(new ReceiptContext(
            ReceiptClass: ReceiptClass.DeliberationStackReviewCompleted,
            SubjectUri: subject,
            Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
            EvidenceRefs: evidenceRefs,
            Decision: "commentary-only",
            EffectStatus: "commentary-only",
            InputHashSeed: cognitiveEnvelope.ReasoningGraphRef.GraphId), ct);

        // Route every user-facing narrative through the central Lane H gate in a single pass. The gate
        // constrains doctrine-violating text, forces warning-first framing for high-risk compounds,
        // refuses unsafe goals, and (only on warning/constrained/refused) issues a safety receipt
        // bound to the same evidence chain.
        var gated = await GateDeliberationAsync(
            gate, currentUser, envelope, cognitiveEnvelope, evidenceRefs, subject, ct);

        var response = MapToResponse(envelope, cognitiveEnvelope, gated);
        return Results.Ok(response);
    }

    /// <summary>
    /// Holds the gate decision plus the safe (constrained/refused) replacement narrative for every
    /// text the deliberation surfaces, keyed so each response section can read its own safe text.
    /// </summary>
    private sealed record GatedDeliberation(
        IntelligenceOutputDecision Decision,
        IReadOnlyDictionary<string, string> DeterministicNarratives,
        IReadOnlyDictionary<string, string> PerspectiveFindingNarratives,
        IReadOnlyDictionary<string, string> PerspectiveSummaries,
        string ContradictionNarrative);

    /// <summary>
    /// Collect every user-facing narrative the board produced, gate them in one evaluation (so a
    /// single safety decision/receipt covers the whole deliberation), then map the gate's ordered
    /// safe text back to each section. The user goal is screened as the request text so a sourcing/
    /// administration-seeking goal is refused rather than answered.
    /// </summary>
    private static async Task<GatedDeliberation> GateDeliberationAsync(
        IUserFacingIntelligenceGate gate,
        ICurrentUserAccessor currentUser,
        StackDeliberationEnvelope envelope,
        CognitiveDensityEnvelope cognitive,
        IReadOnlyList<string> evidenceRefs,
        string subject,
        CancellationToken ct)
    {
        // Build the ordered (bucket, key) → text list. The same order is used to read SafeText back.
        var slots = new List<(string Bucket, string Key)>();
        var texts = new List<string>();

        foreach (var f in envelope.DeterministicFindings)
        {
            slots.Add(("det", f.FindingId));
            texts.Add(f.Narrative);
        }

        var orderedRoles = cognitive.BranchPerspectiveReview.PerspectiveReviews
            .OrderBy(kvp => kvp.Key.ToString(), StringComparer.Ordinal)
            .ToList();

        foreach (var (role, review) in orderedRoles)
        {
            foreach (var pf in review.Findings)
            {
                slots.Add(("perspective", pf.FindingId));
                texts.Add(pf.Narrative);
            }
            slots.Add(("summary", role.ToString()));
            texts.Add(review.Summary);
        }

        slots.Add(("contradiction", string.Empty));
        texts.Add(cognitive.ContradictionReview.CounterPlanNarrative);

        // High-risk classification draws on both the substance names (display + slug) and the
        // payload-supplied category labels, so SARM/peptide/GLP-1/etc. force warning-first framing.
        var substances = envelope.Compounds
            .SelectMany(c => new[] { c.DisplayName, c.Slug })
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var categoryHints = envelope.Compounds
            .Select(c => c.Category)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var decision = await gate.EvaluateAsync(new IntelligenceOutputRequest(
            OutputType: "stack-review.deliberation-envelope",
            ActorUserId: currentUser.GetCurrentUserId(),
            SubjectUri: subject,
            TextFields: texts,
            EvidenceRefs: evidenceRefs,
            // The deliberation reasons over the governed reasoning-graph artifact (a Decision Receipt
            // is bound to it), so it is treated as graph-sourced rather than a fallback inference;
            // high-risk framing still fires from the substances/categories regardless of source.
            SourceType: IntelligenceSource.Graph,
            Substances: substances,
            CategoryHints: categoryHints,
            GraphArtifactHash: cognitive.ReasoningGraphRef.GraphId,
            RequestText: envelope.Goal), ct);

        var det = new Dictionary<string, string>(StringComparer.Ordinal);
        var perspective = new Dictionary<string, string>(StringComparer.Ordinal);
        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);
        var contradiction = cognitive.ContradictionReview.CounterPlanNarrative;

        for (var i = 0; i < slots.Count; i++)
        {
            var safe = decision.SafeText[i];
            switch (slots[i].Bucket)
            {
                case "det": det[slots[i].Key] = safe; break;
                case "perspective": perspective[slots[i].Key] = safe; break;
                case "summary": summaries[slots[i].Key] = safe; break;
                case "contradiction": contradiction = safe; break;
            }
        }

        return new GatedDeliberation(decision, det, perspective, summaries, contradiction);
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
        GatedDeliberation gated)
    {
        const string commentaryOnly = "commentary-only";

        var deterministicResponse = envelope.DeterministicFindings
            .Select(f => new DeterministicFindingResponse(
                FindingId: f.FindingId,
                Code: f.Code,
                Category: f.Category,
                Narrative: gated.DeterministicNarratives[f.FindingId],
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
                        Narrative: gated.PerspectiveFindingNarratives[f.FindingId],
                        Severity: f.Severity.ToString(),
                        EffectStatus: commentaryOnly)).ToList(),
                    Summary: gated.PerspectiveSummaries[kvp.Key.ToString()],
                    EffectStatus: commentaryOnly));

        var contradiction = new ContradictionReviewResponse(
            CounterPlanNarrative: gated.ContradictionNarrative,
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

        var witnessNarrative = BuildWitnessNarrative(cognitive, gated);
        var reasoningGraphFull = BuildReasoningGraph(envelope, cognitive, gated);

        return new StackDeliberationEnvelopeResponse(
            DeterministicFindings: deterministicResponse,
            PerspectiveReviews: perspectiveResponses,
            ContradictionReview: contradiction,
            ConfidenceProfile: confidence,
            ReasoningGraph: graphRef,
            EffectStatus: commentaryOnly,
            WitnessNarrative: witnessNarrative,
            ReasoningGraphFull: reasoningGraphFull,
            SafetyStatus: gated.Decision.SafetyStatus,
            Warnings: gated.Decision.Warnings,
            PolicyRefs: gated.Decision.PolicyRefs,
            SafetyReceiptId: gated.Decision.SafetyReceiptUri);
    }

    private static WitnessNarrativeResponse BuildWitnessNarrative(
        CognitiveDensityEnvelope cognitive,
        GatedDeliberation gated)
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
                    ? gated.PerspectiveSummaries[role.ToString()]
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
            Summary: gated.ContradictionNarrative,
            FindingIds: []));

        // Sort chronologically oldest first
        entries.Sort((a, b) => string.Compare(a.Timestamp, b.Timestamp, StringComparison.Ordinal));

        return new WitnessNarrativeResponse(entries);
    }

    private static ReasoningGraphResponse BuildReasoningGraph(
        StackDeliberationEnvelope envelope,
        CognitiveDensityEnvelope cognitive,
        GatedDeliberation gated)
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

        // Deterministic claim nodes — labels read the gated narrative so graph labels carry the same
        // doctrine constraints as the rest of the response (they previously used raw narrative).
        foreach (var f in envelope.DeterministicFindings)
        {
            var nodeId = $"claim-{f.FindingId}";
            var label = gated.DeterministicNarratives[f.FindingId];
            nodes.Add(new GraphNodeResponse(
                Id: nodeId,
                Kind: "claim",
                Label: label[..Math.Min(80, label.Length)],
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
                var label = gated.PerspectiveFindingNarratives[f.FindingId];
                nodes.Add(new GraphNodeResponse(
                    Id: nodeId,
                    Kind: MapSeverityToKind(f.Severity.ToString()),
                    Label: label[..Math.Min(80, label.Length)],
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
