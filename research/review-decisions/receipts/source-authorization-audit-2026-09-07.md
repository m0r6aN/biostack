# Source Authorization Audit

Date: 2026-09-07. Base: `915e26f` (origin/main after PR #269).
Scope: **read-only diagnosis** of item 2 of the wave006 remediation directive — "resolve exact source/field
authorization and provenance."

Nothing was authorized, registered, retiered, or promoted. No packet, registry, decision batch, seed, or
route was modified. This receipt reports what is wrong and hands Clint an exact decision list; it does not
make any of those decisions.

Reproduce with:

```bash
python tools/research/audit-source-authorization.py --out research/review-decisions/receipts/authorization-audit-2026-09-07
```

## Headline

The wave006 reviewers reported authority defects one packet at a time — NIH ODS tiered A2 in the creatine
packet against a registry policy of C2, an FDA-hosted industry GRAS submission treated as FDA-authored. Those
are not isolated authoring mistakes. **The pipeline cannot enforce registry authority at all**, so a packet's
own assertion is the only thing the gate ever sees.

| Measure | Count |
|---|---|
| Claims requiring A1/A2 support (78 packets) | 224 |
| Pass today, on the packet's self-declared tier | 212 |
| Would pass under a registry-backed, rights-approved gate | **13** |
| **Pass only because the packet asserts its own authority** | **199** |
| Source `publishedAt` values that are a placeholder `-01-01` date | 67 |

## Root Cause: Two Code Defects

**1. The field-authority gate never consults the registry.**
`backend/src/BioStack.KnowledgeWorker/Pipeline/EvidencePacketPreprocessor.cs:61-73` builds its tier map from
`root["sources"]` — the packet's own array — and nothing else. `Preprocess` then emits
`missing-authoritative-support` based purely on that map. A packet that writes `"authorityTier": "A1"` next to
any URL clears the gate, whether or not the source is registered, rights-approved, or authorized for that
field. This is the mechanism by which "do not weaken authority requirements to resolve this" can be violated
without anyone editing a policy file.

**2. The one registry fallback that exists is dead code.**
`Pipeline/CompoundGraphBuilder.cs:825-835` falls back to the registry by reading `obj["sourceId"]` and
`obj["authorityTier"]` at the **top level** of a registry source entry. Registry schema v2.0.0 nests these as
`identity.sourceId` and `evidencePolicy.authorityTier`. Verified: **0 of 13** registry entries expose either
field at the top level, so that branch cannot match and the graph also falls back to packet self-assertion.

## The Decision List

199 gap claims trace to 146 distinct self-asserted sources, bucketed by what each actually needs. Full
per-source detail, including the compounds and claim IDs each affects, is in
[`authorization-audit-2026-09-07/source-authorization-audit.json`](authorization-audit-2026-09-07/source-authorization-audit.json).

### A. Over-asserted — 7 sources, 11 claim references (Clint decision, substantive)

The packet claims **stronger** authority than its own registry class allows. This is the defect class the
wave006 re-review found by hand.

| Source | Packet tier | Registry class | Class tier |
|---|---|---|---|
| `nih-ods-exercise-athletic-performance-hp` | A2 | nih-ods | C2 |
| `nih-ods-vitamind-health-professional` | A1 | nih-ods | C2 |
| `pubmed-mazdutide-first-approval` | A1 | pubmed | C1 |
| `pubmed-search-vk2735-oral-2026` | A2 | pubmed | C1 |
| `clinicaltrials-gov-nct03323749` | A2 | clinicaltrials | C1 |
| `clinicaltrials-nct03359473` | A2 | clinicaltrials | C1 |
| `clinicaltrials-nct07505745` | A2 | clinicaltrials | C1 |

Correcting these downward removes authoritative support from the claims that lean on them. That is the
correct direction and it will block, not unblock, those claims.

### B. Registration missing — 102 sources, 204 claim references (per-item review required)

The packet tier is consistent with an approved registry class (mostly `dailymed` A1 and `fda` A1), but the
specific item was never registered. **This bucket must not be cleared by bulk prefix aliasing.** The `fda-`
prefix is not evidence of FDA authorship, and the corpus already contains counterexamples:

- `fda-grn-931-creatine-monohydrate` — a manufacturer GRAS notice *hosted by* FDA, exactly the item the
  wave006 receipt said "is not automatically FDA-authored authority."
- `fda-drugs-com-hcg-diet-illegal` — a Drugs.com page carrying an `fda-` prefix.
- `fda-pcac-*` (10 sources) — advisory-committee briefing materials of mixed authorship.

Aliasing by prefix would convert a naming convention into an authority grant. Each item needs its publisher
confirmed individually.

### C. Class rights pending — 7 sources, 7 claim references (blocked on legal, Clint-only)

All seven are WADA. `wada` is registry tier A1 but `rights.reviewStatus: pending-human-legal`, so no WADA
citation can currently support a claim regardless of tier. `wada-s2-prohibited-list-drugscom` additionally
appears to be a Drugs.com page under a `wada-` prefix — same laundering pattern as bucket B.

Five other registry classes are also `pending-human-legal`: `issn-position-stands`, `drugbank`,
`peer-reviewed-paper`, `peer-reviewed-review`, `community-monitoring` — 6 of 13 classes in total.

### D. No registry class at all — 30 sources, 45 claim references (new class needed)

Regulators and publishers with no registry representation: EMA (`ema-epar-fablyn`), Health Canada, HSA
Singapore, the Federal Register, NCBI LiverTox, `accessdata.fda.gov` sponsor-submitted labels, Semantic
Scholar. Each needs a rights and tier decision before it can support anything.

## Separate Finding: Placeholder Publication Dates

67 sources across 44 packets carry a `publishedAt` of `YYYY-01-01T00:00:00Z`. These are almost certainly
year-only metadata widened into a false exact date rather than genuine January 1 publications. The wave006
receipt called for "verified dates or honest uncertainty"; the schema permits `null`, which is the honest
value where only a year is known. Enumerated in the audit artifact under `placeholderDates`.

## What I Did Not Change, And Why

- **The gate itself.** Making `EvidencePacketPreprocessor` registry-aware would flip roughly 199 claims to
  `missing-authoritative-support` in one step. That is the correct end state, but the sequencing — whether to
  land enforcement before or after registry population — is a release decision with a large blast radius, and
  it interacts with legal decisions that are Clint's. Quantified here so the call can be made on numbers.
- **Any tier, alias, right, or `reviewStatus`.** Every one of those is an authorization decision. The
  directive is explicit that they must not be adjusted to make a candidate pass, and the same reasoning
  forbids adjusting them to make an audit look better.

## Recommended Order

1. Fix the dead registry lookup in `CompoundGraphBuilder` (a defect on any policy).
2. Resolve bucket A downward — 7 sources, mechanical once decided.
3. Clint's legal pass on the 7 `pending-human-legal` classes (bucket C) and the bucket D publishers.
4. Per-item registration for bucket B, with publisher confirmed individually — not by prefix.
5. Replace the 67 placeholder dates with verified dates or `null`.
6. Only then make the gate registry-backed, and re-run this audit expecting the gap to reach zero.

Independent re-review of the wave006 packets (directive item 3) remains outstanding and is unaffected by this
audit. It must be done by someone who did not author the 2026-09-06 corrections.
