# P0-A Corpus Coverage Matrix

Schema: `biostack.p0a-corpus-coverage-matrix.v1`

One row per combinatorial pair of the Required source list's 13 items (`13 choose 2 = 78` pairs;
items 12-13 included even though excluded from `canon-precedence.md`'s registry). No pair is
silently absent. `BaseCommit`: `b78e7de8463a3db410c45cf223cd722713fec816`.

## Item key

| # | Item |
|---|---|
| 1 | `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` |
| 2 | `docs/specs/INDEX.md` and `docs/specs/README.md` |
| 3 | `README.md` (repository root) |
| 4 | `docs/product/knowledge-engine-capability-map.md` |
| 5 | `docs/product/knowledge-engine-model-data-roadmap.md` |
| 6 | `docs/product/product-ids.md` |
| 7 | `BIOSTACK_FRONTEND_READINESS_AUDIT.md` |
| 8 | `docs/canon/biostack-protocol-intelligence-canon.md` |
| 9 | `docs/guidance/biostack-guidance-content-contract.v1.md` and `docs/guidance/RATIFICATION.md` |
| 10 | `docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md` through `BIO-PAIRWISE-006-studied-combinations-surface.md` |
| 11 | `docs/INITIATIVES/biostack-local-readiness/FINAL-HANDOFF.md` |
| 12 | `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` |
| 13 | `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` |

## Pairwise outcomes

| Pair | Outcome | Citation / reason |
|---|---|---|
| 1-2 | `scope-disjoint` | Item 1 is product doctrine (CHARTER.md's own `may`/`must not` lists); item 2 is procedural canon (how specs are ranked/registered/closed); no overlapping claim. |
| 1-3 | `compared-contradictory` | `CI-001` |
| 1-4 | `compared-contradictory` | `CI-005` |
| 1-5 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 1, 5 — item 5 is a non-prescriptive roadmap narrative that explicitly disclaims authorizing production behavior changes; no claim competes with item 1's `may`/`must not` list. |
| 1-6 | `scope-disjoint` | Item 6 is effectively empty; no claim exists to compare against item 1. |
| 1-7 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 1, 7 — item 7 makes readiness verdicts, not product-behavior `may`/`must not` claims; no competing claim found upon comparison. |
| 1-8 | `compared-contradictory` | `CI-002` (also touched by `CI-001`/`CI-003`) |
| 1-9 | `compared-contradictory` | `CI-001` |
| 1-10 | `compared-consistent` | `CI-009` |
| 1-11 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 1, 11 — item 11 explicitly preserves charter D17 ("LOCAL-GO does NOT imply staging/production readiness … those remain under the production HOLD initiative"); no competing claim found. |
| 1-12 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 1, 12 — the ledger's D-G/D-H/D-I entries operate inside the charter's dependency spine and authorization boundary and do not contradict item 1's text; D-H explicitly preserves every class-triggered human-approval condition. |
| 1-13 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 1, 13 — the design-gate package's §2 restates, rather than contradicts, item 1's D13 text; see `CI-001`'s disposition for the full cross-reference. |
| 2-3 | `scope-disjoint` | Spec-lifecycle procedure versus product-facing description; no overlapping claim. |
| 2-4 | `scope-disjoint` | Spec-lifecycle procedure versus knowledge-engine capability mapping; no overlapping claim. |
| 2-5 | `scope-disjoint` | Spec-lifecycle procedure versus knowledge-engine roadmap narrative; no overlapping claim. |
| 2-6 | `scope-disjoint` | Item 6 is effectively empty. |
| 2-7 | `scope-disjoint` | Spec-lifecycle procedure versus frontend readiness audit; no overlapping claim. |
| 2-8 | `scope-disjoint` | Spec-lifecycle procedure versus protocol-intelligence product canon; no overlapping claim. |
| 2-9 | `scope-disjoint` | Spec-lifecycle status vocabulary (`review-candidate`/`active`/`done`) and the guidance contract's approval-level vocabulary (`automated_candidate`.."legal_product_ratification") serve distinct, non-competing purposes (spec governance status versus product-guidance output approval); no overlapping claim. |
| 2-10 | `compared-contradictory` | `CI-011` |
| 2-11 | `scope-disjoint` | `FINAL-HANDOFF.md` references `docs/specs/INDEX.md` regeneration as a completed process step for its own parcel ledger; it does not dispute the lifecycle rules item 2 states. |
| 2-12 | `scope-disjoint` | Owner-decision ledger versus spec-lifecycle procedure; no overlapping claim. |
| 2-13 | `scope-disjoint` | Design-gate decision package versus spec-lifecycle procedure; no overlapping claim. |
| 3-4 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 3, 4 — README's safety boundary ("does not provide … individualized dosing … start/stop/taper/escalation advice") and the capability map's prohibition list name the same restricted behaviors in parallel, non-competing terms. |
| 3-5 | `scope-disjoint` | README's public-facing safety boundary versus the roadmap's internal implementation-phase narrative; no overlapping claim. |
| 3-6 | `scope-disjoint` | Item 6 is effectively empty. |
| 3-7 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 3, 7 — README's "Investor and partner diligence" section ("does not … establish … a launched B2B provider offering") and the audit's "Ready for provider acquisition" verdict describe different claim types (readiness-to-pursue versus already-launched); no direct textual contradiction found upon comparison; the audit's own review-status gap is separately catalogued at `CI-008`. |
| 3-8 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 3, 8 — both state the same restrictive "Not Medical Advice"/`must not` posture in parallel terms. |
| 3-9 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 3, 9a — README's "Governance" section accurately summarizes the contract's Class A-D taxonomy (including "Class D … Prohibited") without contradicting it. |
| 3-10 | `scope-disjoint` | README does not discuss the pairwise negative-relationship lane; no overlapping claim. |
| 3-11 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 3, 11 — both independently uphold the same claim-discipline posture (README's `/privacy` stub moratorium; `FINAL-HANDOFF.md`'s "No public, revenue, deployment, or privacy claim below exceeds the evidence"). |
| 3-12 | `scope-disjoint` | README does not reference the Coordinator Decisions ledger; no overlapping claim. |
| 3-13 | `scope-disjoint` | README does not reference the P0-B design-gate package; no overlapping claim beyond what `1-9`/`1-13` already cover for the underlying contract text. |
| 4-5 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 4, 5 — the roadmap implements the capability map's model/data roles; same source family, non-conflicting. |
| 4-6 | `scope-disjoint` | Item 6 is effectively empty. |
| 4-7 | `scope-disjoint` | Knowledge-engine capability mapping versus frontend UX readiness audit; no overlapping claim. |
| 4-8 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 4, 8 — both state the same prohibition list (individualized dosing, sourcing guidance, injection instructions, etc.) in near-identical terms. |
| 4-9 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 4, 9a — the capability map's prohibition on "individualized dosing" and the contract's Class A/B evidence-comparison permissions (e.g., "the recorded 12 mg amount is 12 to 24 times the initiation range") describe non-competing behaviors: evidence comparison is not individualized dosing. |
| 4-10 | `scope-disjoint` | Knowledge-engine capability mapping versus the pairwise relationship-census lane; no overlapping claim. |
| 4-11 | `scope-disjoint` | Knowledge-engine capability mapping versus local-readiness closure record; no overlapping claim. |
| 4-12 | `scope-disjoint` | Knowledge-engine capability mapping versus the Coordinator Decisions ledger; no overlapping claim. |
| 4-13 | `compared-consistent` | `CI-005` cross-reference note — the design-gate package's §2 does not dispute the capability map's text; D-I's ruling does not, by its own text, resolve `CI-005`'s narrower prohibition-list conflict (that conflict remains between items 1 and 4, not between items 4 and 13 themselves). |
| 5-6 | `scope-disjoint` | Item 6 is effectively empty. |
| 5-7 | `scope-disjoint` | Knowledge-engine roadmap narrative versus frontend readiness audit; no overlapping claim. |
| 5-8 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 5, 8 — both describe the same warning-first, high-risk-category handling posture. |
| 5-9 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 5, 9a — the roadmap's deterministic-refusal guardrail approach aligns with the contract's Class D prohibition and escalation rules. |
| 5-10 | `scope-disjoint` | Knowledge-engine roadmap versus the pairwise relationship-census lane; no overlapping claim. |
| 5-11 | `scope-disjoint` | Knowledge-engine roadmap versus local-readiness closure record; no overlapping claim. |
| 5-12 | `scope-disjoint` | Knowledge-engine roadmap versus the Coordinator Decisions ledger; no overlapping claim. |
| 5-13 | `scope-disjoint` | Knowledge-engine roadmap versus the P0-B design-gate package; no overlapping claim. |
| 6-7 | `scope-disjoint` | Item 6 is effectively empty. |
| 6-8 | `scope-disjoint` | Item 6 is effectively empty. |
| 6-9 | `scope-disjoint` | Item 6 is effectively empty. |
| 6-10 | `scope-disjoint` | Item 6 is effectively empty. |
| 6-11 | `scope-disjoint` | Item 6 is effectively empty. |
| 6-12 | `scope-disjoint` | Item 6 is effectively empty. |
| 6-13 | `scope-disjoint` | Item 6 is effectively empty. |
| 7-8 | `scope-disjoint` | The audit evaluates UX/product readiness, not canon's substance-specific `may`/`must not` list; no overlapping claim. |
| 7-9 | `scope-disjoint` | The audit never discusses the contract's output-class taxonomy (verified: zero occurrences of "Class A/B/C/D" or the contract's filename in the audit text); no overlapping claim. |
| 7-10 | `scope-disjoint` | The audit does not discuss the pairwise relationship-census lane; no overlapping claim. |
| 7-11 | `scope-disjoint` | `FINAL-HANDOFF.md` documents local-readiness closure claims; it does not make or dispute the audit's provider-acquisition verdict. |
| 7-12 | `scope-disjoint` | The audit does not reference the Coordinator Decisions ledger; no overlapping claim. |
| 7-13 | `scope-disjoint` | The audit does not reference the P0-B design-gate package; no overlapping claim. |
| 8-9 | `compared-consistent` | `SOURCE-MANIFEST.md` entries 8, 9a — the contract's own front-matter cross-references the canon ("Product canon … evidence-context §"); both independently prohibit Class D/dosing-instruction behavior, mutually reinforcing rather than conflicting with each other (they jointly conflict with item 1, catalogued at `CI-001`/`CI-002`/`CI-003`). |
| 8-10 | `compared-consistent` | The canon's "Runtime behavior must prefer `Unknown` over unsupported inference … BioStack should say it has not evaluated that relationship" restates, rather than contradicts, the pairwise lane's absence doctrine (`CI-009`'s source_a). |
| 8-11 | `scope-disjoint` | Protocol-intelligence canon versus local-readiness closure record; no overlapping claim. |
| 8-12 | `compared-consistent` | D-I's ruling cross-references the headline canon conflict (`CI-001`) without amending the canon document's own text; no new contradiction between items 8 and 12 themselves. |
| 8-13 | `scope-disjoint` | The design-gate package's §2 conflict table cites the contract and the charter directly; it does not quote `docs/canon/biostack-protocol-intelligence-canon.md`. |
| 9-10 | `compared-consistent` | `docs/guidance/RATIFICATION.md`'s "Pairwise relationship publication contract v1 ratified (BIO-PAIRWISE-002)" entry states its ratification "does not loosen, widen, or reverse" the existing per-pair reasoning entitlement gate — consistent with, not contradicting, the pairwise lane's own doctrine. |
| 9-11 | `scope-disjoint` | Guidance contract versus local-readiness closure record; no overlapping claim. |
| 9-12 | `compared-consistent` | D-I's own text states its ruling "amends neither document's text" — no direct textual conflict between items 9 and 12 themselves (the conflict is between items 1 and 9, catalogued at `CI-001`). |
| 9-13 | `compared-consistent` | The design-gate package accurately restates the contract's Class D position without disputing it; see `CI-001`'s cross-reference. |
| 10-11 | `scope-disjoint` | Pairwise relationship-census lane versus local-readiness closure record; no overlapping claim. |
| 10-12 | `compared-consistent` | The ledger's D-A/D-B/D-E entries directly govern and are consistent with/supportive of the pairwise lane specs (schema-sufficiency pre-gate, migration-vs-redesign, BIO-PAIRWISE-005 rulings). |
| 10-13 | `scope-disjoint` | Pairwise relationship-census lane versus the P0-B design-gate package (a distinct conflict domain — personalized dosing, not pairwise relationships); no overlapping claim. |
| 11-12 | `compared-consistent` | `FINAL-HANDOFF.md` explicitly cites and relies on the ledger's D-C/D-D rulings as its own evidence ("owner ruling D-C … + owner ruling D-D"); directly supportive, not conflicting. |
| 11-13 | `scope-disjoint` | Local-readiness closure record versus the P0-B design-gate package; no overlapping claim. |
| 12-13 | `compared-consistent` | The ledger's D-H/D-I entries directly describe, open, and rule on the design-gate package; fully aligned by construction (D-I rules on exactly what the package presents). |

## Completeness statement

78 of 78 combinatorial pairs over the 13 Required source list items are recorded above with a
non-empty outcome (`compared-contradictory`, `compared-consistent`, or `scope-disjoint`). No pair
is silently absent.
