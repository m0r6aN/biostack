# Wave006 Packet Correction Receipt

Date: 2026-09-06. Base: `a065c2eef842cd9a825be00519a74a7290fc075d` (origin/main after PR #268).
Scope: **packet-authoring correction of the three lead-validated errors only**. No promotion, no clearance,
no re-review, no source or authority changes.

This executes item 1 of the remediation directive in
[`wave006-rereview-2026-09-05.md`](wave006-rereview-2026-09-05.md), and only item 1. Items 2 (source/field
authorization), 3 (independent re-review of changed packets), and 4 (scoped promotion) remain open and are
explicitly **not** advanced by this pass.

## What Was Corrected

The wave006 receipt distinguishes reviewer-proposed dispositions from the three blockers the lead
independently reproduced. Only the lead-validated three are corrected here. Every other request-changes and
unresolved disposition in that receipt is untouched and still outstanding.

| Packet | Claim | Demonstrated error | Correction |
|---|---|---|---|
| `creatine.evidence.json` | `creatine-safety-common-adverse-effects-001` | 13.7% / 13.2% presented as participant side-effect rates; source described as independent corroboration | Relabeled as proportion **of studies** reporting side effects; the three distinct denominators (studies, participants, spontaneous reports) separated; independence claim withdrawn on shared author/venue lineage with the ISSN position stand cited in the same claim |
| `vitamin-d3.evidence.json` | `vitamind3-efficacy-fracture-falls-uspstf-negative-001` | Asserted the kidney-stone harm was "not mentioned in the 2018 statement" | Corrected: the 2018 final statement describes the harm explicitly and assesses it as small (pooled RR 1.18, 95% CI 1.04-1.35); the December 2024 draft **updates** that pooled estimate (RR 1.11) rather than identifying a new harm |
| `tamoxifen.evidence.json` | `tamoxifen-efficacy-atlas-extended-001` | 18.6% vs 21.1% (RR 0.87) labeled breast cancer mortality; crude proportions labeled cumulative | Corrected to **all-cause mortality**; figures identified as crude event proportions, distinguished from the primary report's years-5-to-14 cumulative risks; separate efficacy and safety populations stated |

Each claim's in-band `reviewFlags` narrative that had declared the underlying issue **resolved** was withdrawn
and replaced with a corrected, still-unresolved narrative. Historical decision receipts and decision batches
were not rewritten.

## What Was Deliberately Not Done

- **No source was added.** The tamoxifen packet still cites only the secondary trial-summary page that
  mislabels the endpoint. Anchoring the ATLAS primary text (PMID 23219286) requires source authorization,
  which is not this pass's authority. Recorded as conflict `tamoxifen-atlas-endpoint-mislabel-001`.
- **No authority tier, `fieldAuthorityRequired` flag, `evidenceTier`, `confidence`, `claimType`, `context`,
  or `sourceRefs` value was changed** in any claim, in any packet. Verified mechanically against `HEAD`.
- **No unanchored numbers were introduced.** The wave006 receipt records creatine participant-frequency
  figures of 4.21% (placebo) versus 4.60% (creatine). These are named in a `reviewFlag` as requiring verbatim
  extraction and are deliberately **not** asserted in the claim statement.
- **No packet was cleared.** All three remain `needsReview: true`, `completeness: partial`, and review-gated.
- No seed, public route, census fixture, registry, PCAC `live` flag, or decision batch was touched.

## New And Modified Conflicts

| Conflict | Packet | Severity | Status |
|---|---|---|---|
| `creatine-pharmacovigilance-source-independence-001` | creatine | high | needs-human-review (new) |
| `vitamind3-uspstf-2018-vs-2024-harm-provenance-001` | vitamin-d3 | high | needs-human-review (new) |
| `tamoxifen-atlas-endpoint-mislabel-001` | tamoxifen | high | needs-human-review (new) |
| `tamoxifen-extended-duration-benefit-risk-001` | tamoxifen | moderate → high | needs-human-review (updated) |

## Verification

- Edits were applied as text-level substitutions rather than a JSON round-trip, so the diff contains only the
  intended changes and no reformatting churn.
- All three packets parse and validate against
  `backend/src/BioStack.KnowledgeWorker/Schemas/evidence-packet.schema.json`.
- A mechanical object-level diff against `HEAD` confirms: `packet`, `compound`, and `sources` blocks
  unchanged; claim count unchanged; the six authority-sensitive fields unchanged on every claim in all three
  packets; changes confined to three `statement` values, three `reviewFlags` arrays, two `extractedEvidence`
  arrays, the `conflicts` arrays, and `ops.reviewReasons` / `ops.qualityFlags`.
- Database-free Research run against the explicit `pilot-source-registry.json` completed with
  `Scanned=78 Created=78 FlaggedForReview=78 Failed=0`, and logged that the Postgres connectivity check was
  skipped. The promotion manifest is unchanged from the 2026-09-05 baseline: **76 blocked, 1 review-required,
  1 research-requested, 1 candidate for promotion (Semaglutide only)**. Creatine, Vitamin D3, and Tamoxifen
  all remain in the **Blocked** bucket. These corrections moved no compound toward promotion.
  Artifacts written to the ignored path `research/output/gtm-20260906/`.

## Next Work, Unchanged

The order from the wave006 receipt still stands and this pass does not shorten it: resolve exact source and
field authorization; obtain **independent** re-review of these changed packets and of every still-unresolved
assertion against different source families; only then consider a scoped promotion decision. A corrected
packet is not a cleared packet.
