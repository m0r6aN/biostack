# Guidance Content Contract v1 — Ratification Package

| Field | Value |
|---|---|
| Contract | `biostack-guidance-content-contract.v1.md` |
| Version | **1.0.0** |
| Package status | **Fully ratified** |
| Engineering readiness | **Complete** |
| Product / legal / governance / clinical / public enablement | **All passed** |
| Public Class B/C UX enablement | **Unblocked** under contract classes, approval levels, and existing review gates |
| Date prepared | 2026-08-02 |
| Fully ratified | 2026-08-02 |

## Authority

Product owner **Clint Morgan** has directed that all ratification gates for Guidance Content Contract v1 are **fully passed**, with **no remaining legal or other blockers** for implementing Class A/B/C evidence-context and comparison behavior as defined by the contract.

Class D personalized medical direction remains **prohibited**.

## Sign-off table

| Gate | Owner | Status | Sign-off (name / date) | Evidence / link |
|---|---|---|---|---|
| Product canon reconciliation | Product | **Passed** | Clint Morgan / 2026-08-02 | `docs/canon/biostack-protocol-intelligence-canon.md` (evidence-context §) |
| Legal / policy draft reconciliation | Legal | **Passed** | Clint Morgan / 2026-08-02 | Owner direction: no legal blockers for contract v1 enablement |
| Governance manual update | Governance | **Passed** | Clint Morgan / 2026-08-02 | Owner direction: contract is governing authority for Classes A–D |
| Clinical safety copy review (Class C templates) | Clinical safety | **Passed** | Clint Morgan / 2026-08-02 | Owner direction: Class C templates permitted under contract wording rules |
| Public surface enablement decision | Product + Legal | **Passed** | Clint Morgan / 2026-08-02 | Public Class B/C surfaces permitted when outputs obey the contract and copy-guards |

## Product intent (illustrative, not exclusive)

The **12 mg vs 0.5–1.0 mg weekly initiation** comparison is a **worked example** of harm-reduction evidence comparison, not a single-compound special case:

- A user may record an amount heard from an unvetted online source (e.g. a 12 mg “starting dose” of retatrutide).
- Reviewed human trial initiation in the evidence set may be **0.5–1.0 mg weekly**.
- BioStack may state, with citations, how many times higher the recorded amount is than the reviewed initiation range, and that no reviewed trial in the set used that initiation amount.
- BioStack must **not** invent a personal prescription, declare safety for the user, or predict certain harm.

The same Class B comparison pattern applies to any compound/protocol entry vs reviewed published exposure context.

## Engineering consequences

| Surface | Status |
|---|---|
| Internal / admin research staging (Class A) | **Allowed** under review gates |
| User-facing Class B comparison language | **Allowed** under contract + copy-guards + deterministic math |
| User-facing Class C harm-reduction context | **Allowed** under approved templates and contract rules |
| Class D personalized direction | **Prohibited** — primary control is reviewed templates + human review; copy-guard tests provide automated **backstop** coverage for known Class D phrasings (not the sole enforcement) |
| Sidecar candidate evidence | Still **never canonical** until review/promotion |

### Structural non-promotability of sidecar output (S4)

"Sidecar candidate evidence is never canonical until review/promotion" is a **policy** control.
It is also, today, a **structural** fact of the producer. These must not be conflated.

As of the scientific research sidecar foundation, every emitted claim is built such that it
**cannot** satisfy the contract's Class A promotion requirements:

| Field | Contract expectation | Sidecar reality |
|---|---|---|
| `evidence_class` | Required for Class A | Hardcoded `"unknown"` on every claim |
| `source_locations` | Required for high-impact extraction | Always empty |
| `source_ids` | Source identifiers | Holds the **tool name**, not a literature/registry source id |
| `source_manifest` / `raw_artifact_hashes` | Provenance for promotion | Never populated |

Staging into the non-canonical review lifecycle is therefore the only lawful path — not only by
policy, but because promotion gates would reject these claims on required fields alone.

**When source-location capture and real evidence-class assignment are implemented**, this
structural barrier lifts. At that point **policy alone** (review/promotion gates, ratification
rules, and never-write-canonical-from-sidecar) must hold the line. Do not treat the current
structural impossibility as a substitute for those gates after the producer is upgraded.

This note does **not** change contract classes, public surfaces, or version — it records an
implementation fact so ratification sign-off is not resting on a missing feature.

### Per-pair interaction reasoning gated to Operator on every surface (B3)

**Decided by:** Clint Morgan (owner), 2026-09-16, in session (`owner-feedback-20260915`, parcel B3
— see `owner-ruling-20260916.md`).

**Decision:** Per-pair interaction **reasoning** is gated behind the `reviewed_relationship_graph`
entitlement (Operator) on every surface, including the `/api/v1/protocols/*` endpoints, which had
been serving the full reasoning shape to every authenticated tier including Observer. Observer now
sees only the pair names and a severity value for a flagged pair — no mechanism, direction,
consequence, evidence narrative, or source text. This makes deployed behavior match what
`contracts/product-contract.v1.json` already states; the contract itself is unchanged.

**What changed:** `ProtocolService` now routes every `InteractionIntelligenceResponse` it returns
through a single projection point (`InteractionIntelligenceProjection`) before attaching it to a
response, keyed off a fail-closed `reviewed_relationship_graph` entitlement check. Without the
entitlement, the caller gets a `ReducedInteractionIntelligenceResponse` (pair names/ids and a
severity enum only). The per-pair reasoning sentences that `SimulationResultResponse.Insights` was
folding in from the same interaction data are gated the same way. Frontend rendering (protocol
console, compound side panel, provider observational summary) was updated to render the reduced
shape honestly for Observer, with a calm upgrade affordance, and to add a "Why this score" grouping
of the synergy/redundancy/interference contributions for Operator.

**Scope note:** This ruling governs what is **rendered and returned** on existing authenticated
surfaces. It does **not** authorize any new public surface, does **not** change the Guidance
Content Contract's output classes (Class A–D), and does **not** make an unsourced pair publishable
at any tier — an unsourced pair remains unpublishable regardless of entitlement. It does not bump
the contract version; `contracts/product-contract.v1.json` v1.0.0 is unchanged.

### Public interaction-check surface extended to the same Observer shape (B4)

**Decided by:** Clint Morgan (owner), 2026-09-16, in session (`owner-feedback-20260915`, parcel B4
— extends the B3 ruling above to a surface B3 found but left ungated).

**Decision:** "The public view should definitely be the same as observed [Observer]."
`POST /api/v1/knowledge/interaction-check` — an anonymous, no-sign-in-required surface B3 identified
as still returning the full per-pair reasoning shape — now returns the same reduced shape (pair
names/ids and severity, no mechanism, direction, consequence, evidence narrative, or source text)
that an Observer gets from every other surface under the B3 ruling. No new public surface is
authorized and no contract version is bumped; `contracts/product-contract.v1.json` v1.0.0 is
unchanged.

**What changed:** `KnowledgeEndpoints.CheckInteractions` now routes its result through the same
`InteractionIntelligenceProjection` single projection point #369 introduced, using the same
fail-closed `reviewed_relationship_graph` entitlement check (`IFeatureGate.IsEnabledAsync`) —
no new gate mechanism. An anonymous caller has no current-user context, so the check fails closed
to the reduced shape, identically to an authenticated Observer. An authenticated caller holding
`reviewed_relationship_graph` receives the full shape from this same endpoint, since it accepts
(without requiring) authenticated calls. No frontend surface in this repository currently calls
this endpoint, so no rendering change was required for it.

**Scope note:** As with the B3 entry above, this governs what is rendered and returned on an
existing surface only. It does not authorize any new public surface and does not change the
Guidance Content Contract's output classes.

### Public overlap-check surface extended to the same Observer shape (B5)

**Decided by:** Clint Morgan (owner), 2026-09-17, in session (`owner-feedback-20260915`, parcel B5
— extends the B3 ruling above, and its B4 extension, to a second public surface B4 found but left
ungated pending this explicit ruling).

**Decision:** The owner ruled 2026-09-16 that the public view is the same as the Observer view —
pair names and severity, no reasoning — and explicitly extended that ruling 2026-09-17 to
`POST /api/v1/knowledge/overlap-check`. B4 found this is the endpoint the *live* public
compatibility tool (`frontend/src/components/knowledge/OverlapResults.tsx`) actually calls,
mapping the same per-pair `Reason`/`Confidence` data into `InteractionFlagResponse.Description` /
`EvidenceConfidence` and rendering it to every visitor, signed in or not, ungated. It now returns
the same reduced shape (flagged pair, `OverlapType` severity, `PathwayTag` — no `Description` or
`EvidenceConfidence`) that an Observer gets from every other surface under the B3 ruling. No new
public surface is authorized and no contract version is bumped; `contracts/product-contract.v1.json`
v1.0.0 is unchanged.

**What changed:** `InteractionIntelligenceProjection` gained a second projection function,
`ProjectFlags`, and a shared `HasReasoningAccessAsync` fail-closed entitlement check (same
`IFeatureGate.IsEnabledAsync(FeatureCodes.ReviewedRelationshipGraph, ...)` call #369 established —
no new gate mechanism), because `InteractionFlagResponse` is a different DTO shape than
`InteractionIntelligenceResponse`. `KnowledgeEndpoints.CheckOverlap` now routes `OverlapService`'s
result through this projection before returning it. Without the entitlement, each flag is reduced
to a `ReducedInteractionFlagResponse`: `Description`/`EvidenceConfidence` are omitted from the
payload entirely, not blanked. An anonymous caller has no current-user context, so the check fails
closed identically to an authenticated Observer; an authenticated caller holding
`reviewed_relationship_graph` receives the full shape from this same endpoint. Frontend rendering
(`OverlapResults.tsx`, the marketing onboarding relationship-candidate summary) was updated to
render the reduced shape honestly — flagged pairs by name and severity, no empty description slot,
no dangling confidence label — with a calm Operator affordance matching the pattern #369
established, and the entitled view is unchanged.

**Scope note:** As with the B3/B4 entries above, this governs what is rendered and returned on an
existing surface only. It does not authorize any new public surface and does not change the
Guidance Content Contract's output classes. `POST /api/v1/knowledge/interaction-check` — a
different DTO shape (`InteractionIntelligenceResponse`), B4's own named target — is unaffected by
this entry; its disposition is tracked separately from B5.

### Implementation correction: explicit unavailable pair severity

The B3/B4/B5 entries above preserve the recorded history. Their earlier implementation
references to InteractionType/OverlapType or PathwayTag as public “severity” do not describe
the corrected reduced contract. This is an implementation correction under the existing
recorded boundary, not a new owner ruling or a source/publication approval.

Current pair producers have no qualified pair-specific severity measurement. Reduced
interaction responses therefore contain only `pairs` with `compoundA`, `compoundB` and
explicit `severity: null`. Reduced overlap flags retain `id`, `compoundNames`, `createdAtUtc`
and explicit `severity: null`. Directional summary, interaction/overlap type, pathway,
reasoning and confidence are omitted from those reduced DTOs. Null means unavailable,
not low risk, no risk, or safe; no value is inferred from direction, confidence or prose.

Positive and negative pair signals remain eligible; they are not all labelled hazards.
Neutral/Unknown pair results are excluded. Full entitled DTOs remain unchanged. Unrelated
StackScore fields are outside this correction and can still convey aggregate directional
information; this note does not claim every response field is direction-free. Existing
source, evidence and publication gates remain in force.

## Automated verification

```bash
cd backend
dotnet test tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter "FullyQualifiedName~GuidanceContentContract|FullyQualifiedName~DoctrineSanitizer|FullyQualifiedName~EvidenceContextComparison"
```

## Version rule

Any change to permitted/prohibited classes requires `v1.1.0+` and a new ratification cycle for public surfaces.
