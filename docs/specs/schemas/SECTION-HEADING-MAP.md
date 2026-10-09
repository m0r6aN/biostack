# SECTION-HEADING-MAP — deterministic term-to-heading bridge (P3-A)

This document is the deterministic bridge between `delivery-class-controls.json`'s free-text
`requiredSpecAdditions` terms and the Markdown ATX heading text that satisfies each term
(document contract 2 of `docs/INITIATIVES/biostack-governed-delivery/parcels/P3-A.md`). The term
column below is derived **live**, at authoring time and re-verified at check time, from the
distinct `requiredSpecAdditions` strings found anywhere in `delivery-class-controls.json` — it is
not a hand-maintained or independently invented list. As of P2's shipped content this live union
is 46 distinct terms (informative context only; the verifier derives the count live, not from this
sentence).

**Matching rule.** A term is satisfied by a document's heading set if any heading, after
normalizing (lowercase; `/` and `-` replaced with a space; whitespace collapsed; each token's
single trailing `s` stripped), contains, as a contiguous token subsequence, the same normalization
applied to the term itself or to any listed alias. Every term below uses itself as its own
canonical alias: each `requiredSpecAdditions` term, title-cased, is a direct, on-topic ATX heading
(for example the term `rollback` is satisfied by the literal heading `## Rollback`), so no term in
this parcel's own templates requires a materially different alias phrase. A future amending parcel
(the `delivery-class-extension` extension point) may append an additional alias to an existing row
or a new row for a genuinely new term, additively only, per `EXTENSION-POINTS.md`.

**Anti-heading-soup constraints.** A contiguous-token-subsequence match alone does not mark a term
satisfied. Both of the following must also hold, deterministically, from the document's actual ATX
heading set (`^#{2,3}\s+.+$` lines):

1. **Distinct-heading-per-term**, resolved by a pinned, deterministic one-to-one bipartite match:
   process required terms in strict ordinal (byte-value) lexicographic ascending order of the
   live, deduplicated `requiredSpecAdditions` union's term strings for the spec's declared, folded
   class set; for each term in that order, assign it the lexicographically least (by normalized
   heading text, ties broken by earlier document-order position) still-unconsumed candidate
   heading occurrence that textually matches it; a term with no remaining unconsumed matching
   candidate at its turn resolves `satisfied: false`.
2. **Heading-length bound**: a candidate heading's own normalized token count must not exceed the
   matched term's (or matched alias's) normalized token count by more than 4 tokens, and must
   never exceed 10 normalized tokens in total, whichever bound is smaller. A heading exceeding
   this bound is disqualified as a candidate for any term before the one-to-one match is
   attempted.

A term that fails either constraint resolves `satisfied: false` and the containing document fails
with reason `missing-required-section` naming that term — there is no separate reason code for a
heading-soup failure.

## Term map

| Control term | Canonical alias(es) | Source |
|---|---|---|
| SLA/owner | SLA/owner | delivery-class-controls.json |
| acceptance criteria | acceptance criteria | delivery-class-controls.json |
| access control | access control | delivery-class-controls.json |
| approval status | approval status | delivery-class-controls.json |
| claims | claims | delivery-class-controls.json |
| compatibility window | compatibility window | delivery-class-controls.json |
| consent | consent | delivery-class-controls.json |
| contracts | contracts | delivery-class-controls.json |
| data inventory | data inventory | delivery-class-controls.json |
| data-loss analysis | data-loss analysis | delivery-class-controls.json |
| deletion | deletion | delivery-class-controls.json |
| enforcement surfaces | enforcement surfaces | delivery-class-controls.json |
| environment plan | environment plan | delivery-class-controls.json |
| escalation | escalation | delivery-class-controls.json |
| evidence grade | evidence grade | delivery-class-controls.json |
| evidence threshold | evidence threshold | delivery-class-controls.json |
| export | export | delivery-class-controls.json |
| external contract version | external contract version | delivery-class-controls.json |
| fail-open/closed behavior | fail-open/closed behavior | delivery-class-controls.json |
| forward/backward behavior | forward/backward behavior | delivery-class-controls.json |
| function-review status | function-review status | delivery-class-controls.json |
| guidance class | guidance class | delivery-class-controls.json |
| intended use | intended use | delivery-class-controls.json |
| issuer/verifier ownership | issuer/verifier ownership | delivery-class-controls.json |
| jurisdiction/scope | jurisdiction/scope | delivery-class-controls.json |
| minimization | minimization | delivery-class-controls.json |
| missingness | missingness | delivery-class-controls.json |
| numeric provenance | numeric provenance | delivery-class-controls.json |
| objective | objective | delivery-class-controls.json |
| permitted workflow | permitted workflow | delivery-class-controls.json |
| pilot population | pilot population | delivery-class-controls.json |
| policy owner | policy owner | delivery-class-controls.json |
| prohibited clinical behavior | prohibited clinical behavior | delivery-class-controls.json |
| promotion authority | promotion authority | delivery-class-controls.json |
| purpose | purpose | delivery-class-controls.json |
| red flags | red flags | delivery-class-controls.json |
| redaction | redaction | delivery-class-controls.json |
| retention | retention | delivery-class-controls.json |
| review lifecycle | review lifecycle | delivery-class-controls.json |
| role/consent | role/consent | delivery-class-controls.json |
| rollback | rollback | delivery-class-controls.json |
| source/license/provenance | source/license/provenance | delivery-class-controls.json |
| surfaces | surfaces | delivery-class-controls.json |
| tests | tests | delivery-class-controls.json |
| trust boundary | trust boundary | delivery-class-controls.json |
| version/effective date | version/effective date | delivery-class-controls.json |

This table has exactly 46 rows, one per distinct `requiredSpecAdditions` string in
`delivery-class-controls.json` as of this parcel's shipped hash. The verifier re-derives this
union live at check time and fails on any extra, missing, or misspelled term.
