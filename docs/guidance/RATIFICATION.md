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

## Automated verification

```bash
cd backend
dotnet test tests/BioStack.Application.Tests/BioStack.Application.Tests.csproj --filter "FullyQualifiedName~GuidanceContentContract|FullyQualifiedName~DoctrineSanitizer|FullyQualifiedName~EvidenceContextComparison"
```

## Version rule

Any change to permitted/prohibited classes requires `v1.1.0+` and a new ratification cycle for public surfaces.
