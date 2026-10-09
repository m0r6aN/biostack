# Canon Precedence Manifest

Schema: `biostack.canon-precedence.v1`

Parcel: `P0-A` — Product Doctrine Recovery: Canon Precedence and Contradiction Inventory.

This document is the **total order** over every document this parcel recognizes as BioStack
product-doctrine or governance/procedural canon. It is self-contained: a reader does not need to
re-read `docs/INITIATIVES/biostack-governed-delivery/parcels/P0-A.md` to apply it. P0-A ranks
*documents*, not product behavior; it decides no product allowed-output (see "Scope boundary
statement," below).

`BaseCommit` for every claim in this document: `b78e7de8463a3db410c45cf223cd722713fec816`
(the Gate 2 dispatch anchor named in
`docs/INITIATIVES/biostack-governed-delivery/dispatch/GATE2-P0A-IMPLEMENTATION.md`).

---

## 1. The three-property ranking rule (stated normatively)

Given any two documents `A` and `B` both present in the registry (section 2), their relative
precedence is determined by applying the following three properties **in this exact order**,
stopping at the first property that distinguishes them:

1. **Ratification formality.** A document carrying an explicit, dated, named-owner ratification
   event in its own text — a "Status: Fully ratified," "100% Ratified," or equivalent recorded
   sign-off that names **both** who ratified it and when — outranks a document that carries no
   such event. A document with a date but no named owner, or a named owner but no date, does not
   qualify under this property; it is treated as carrying "no ratification event" and falls
   through to property 3 (property 2 never applies to it, because property 2 presupposes a
   qualifying event).
2. **Recency of ratification.** Among documents that both qualify under property 1, the document
   whose ratification event carries the **later** date outranks the one with the earlier date.
   Every rank assigned under this property must name, in the registry's "Ratification event"
   column, the exact dated, named-owner record the rank rests on; a rank claimed under this
   property without naming that record fails the `precedence-manifest-totality` deterministic
   check.
3. **Scope breadth.** Among documents tied on properties 1 and 2 (including the common case of
   two or more documents that both carry "no ratification event" under property 1), the document
   whose own text states or self-evidently carries the broader scope — product-wide doctrine over
   a single feature, pipeline, process, or parcel; prescriptive/normative text over narrative,
   descriptive, or audit text; a document that explicitly self-identifies as "doctrine," "canon,"
   or "authorization" over one that does not — outranks the narrower one. The registry's
   "Precedence note" column names the specific textual basis for every property-3 ranking
   decision, so the ordering is reproducible, not merely asserted.

Any two documents that remain tied after property 3 is applied are **not** resolved by this
parcel. They are recorded as an **open tie** (`open-tie`, with a `ruling-ref` of
`none-recorded-pending-P0-D1-or-owner-ruling`) rather than by inventing a fourth, unprincipled
tie-break criterion, mirroring the D-D/D-C owner-ruling precedent for unresolvable corpus
questions. No such tie exists in the current registry (section 2); every pair resolves to
`A-outranks-B` or `B-outranks-A` by the comparison function (section 3).

### Classification guidance for borderline document types (restated normatively)

A document is entered into the registry (section 2) only if its **own text** states a dated,
named-owner ratification/sign-off event for itself (property 1 may then apply), **or** it is a
product-doctrine, procedural-canon, or claim-bearing artifact that does not merely record a
decision, authorization, or ruling *about* other documents. A document whose own text does only
the latter — records who authorized or ruled on something, and when, without itself carrying a
"Status: Fully ratified" or equivalent doctrine sign-off — is classified `no ratification event`
the same as any undated document, but is **excluded from the registry entirely** and instead read
and cross-referenced by the Contradiction Inventory. Two documents are excluded from this
registry under this test: `docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md` (a procedural
authorization/ruling ledger — its D-G/D-H/D-I entries authorize and rule on other documents'
positions, none ratifies itself) and
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` (a decision-presentation and
ruling record, explicitly self-described as "a decision package, not a spec"). Both are Required
source list items (12 and 13) that the Contradiction Inventory and Source Manifest fully cover;
neither is ranked here or against any registry document.

---

## 2. Ranked registry

Rank is a dense integer (1 = highest precedence) with no gaps. No rank is shared in this registry
(the current corpus produces zero genuine three-way or two-way ties after property 3 is applied;
see "Extension rule," section 5, for how a future tied document would be recorded).

| Rank | Document | Ratification event (date, named owner, or "none recorded") | Stated scope | Precedence note |
|---|---|---|---|---|
| 1 | `docs/guidance/biostack-guidance-content-contract.v1.md` | **2026-08-02, Clint Morgan** — "Fully ratified \| 2026-08-02 (Clint Morgan — all gates passed; no remaining blockers)" (own front-matter table) | Product-wide output-class doctrine (the contract's own Class A/B/C/D taxonomy of permitted/prohibited output) | Property 1: the only required-source document whose own text states both a date and a named owner for its own ratification. Property 2 is vacuous here (no later-dated qualifying document exists in the registry) but recorded for completeness. |
| 2 | `docs/guidance/RATIFICATION.md` | **2026-08-02, Clint Morgan** — "Package status: Fully ratified … Fully ratified 2026-08-02" plus "Authority: Product owner Clint Morgan has directed that all ratification gates for Guidance Content Contract v1 are fully passed" | The sign-off package and evidence record for rank 1's contract (not new doctrine content of its own) | Ties rank 1 on properties 1 and 2 (same date, same named owner). Property 3 breaks the tie: rank 1's document states the Class A-D rules themselves (the doctrine content); this document records the gate sign-offs *for* that doctrine (narrower, evidentiary scope) — "Package status," "Sign-off table," "Engineering consequences" are about the ratification event, not independent product rules. |
| 3 | `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md` | None recorded — the lineage phrase "the developer's product-purpose correction and explicit `100% Ratified` decision" names no date in the document's own text, and `git log -1 --format=%ad` reflects a file-touch date, not a ratification-event citation, per this parcel's own governing spec (`parcels/P0-A.md`, "Precedence manifest," rule 2) | Product-wide: the document's own section header is literally "## Ratified product doctrine," governing the full `may`/`must not` list for all BioStack product behavior | Property 3: self-identifies in its own text as *doctrine* (prescriptive, governs behavior) rather than *description* (of the product, for an audience) — the distinguishing textual basis used against rank 4 below. |
| 4 | `README.md` (repository root) | None recorded (no "ratified," dated, named-owner sign-off anywhere in the file) | Product-wide: "the primary product-facing and investor/partner-facing description of what BioStack is, its tiers, its 'Safety and compliance boundary' paragraph" (per this parcel's Required source list, item 3) | Property 3: self-identifies as a *description* ("What is free, and what is not," "Investor and partner diligence") rather than as doctrine; ranked immediately below rank 3 on that textual distinction, still above all narrower-scope documents below. |
| 5 | `docs/canon/biostack-protocol-intelligence-canon.md` | Dated (2026-06-18, "Status: Canonical product foundation") but **no named owner** stated anywhere in the document's own text — fails property 1's full test (date **and** named owner both required); classified `no ratification event` | Domain-bounded: "BioStack is an observational protocol intelligence product" — the protocol-intelligence domain specifically, not the whole product (tiers, pricing, architecture, billing are out of this document's stated scope) | Property 3: narrower self-declared domain than ranks 3-4 (whole product) but broader and more prescriptive ("may"/"must not" lists) than the procedural-canon documents ranked below it. |
| 6 | `docs/specs/README.md` | None recorded | Procedural: governs the governed-spec lifecycle repo-wide | Property 3: this parcel's own Required source list explicitly distinguishes "Sources 1-2 are governance/procedural canon; 3-9 are product-doctrine canon" — procedural canon is categorically narrower in *doctrine* authority than product-doctrine canon, even though its process scope is repo-wide. Ranked above `docs/specs/INDEX.md` because it states normative lifecycle rules ("A parcel becomes `done` only after merge and complete closure evidence") rather than a bare data table. |
| 7 | `docs/specs/INDEX.md` | None recorded | Procedural: the registry table itself, no independent normative text | Property 3: narrower than rank 6 — a data table recording state, not a document stating rules. |
| 8 | `docs/product/knowledge-engine-capability-map.md` | None recorded (only a "Date: 2026-06-18" header, not a ratification event) | Domain-bounded: ties BioStack product capabilities to source-first data/model roles; states its own authorization/prohibition list in its own voice ("It authorizes … It does not authorize …") | Property 3: prescriptive (states its own authorization boundary) though narrower than rank 5 (bounded to the knowledge-engine capability map specifically, not the whole protocol-intelligence product posture). Ranked above rank 9 because it is prescriptive rather than narrative. |
| 9 | `docs/product/knowledge-engine-model-data-roadmap.md` | None recorded (only a "Date: 2026-06-18" header) | Domain-bounded: "roadmap-level product-capability narrative" (per this parcel's Required source list, item 5) covering the same knowledge-engine domain as rank 8 | Property 3: same domain as rank 8 but explicitly narrative/planning ("implementation phases"), not prescriptive — it states no authorization boundary in its own voice. |
| 10 | `BIOSTACK_FRONTEND_READINESS_AUDIT.md` | None recorded | Narrow: frontend/UX readiness claims and verdicts for specific product surfaces | Property 3: this parcel's own Required source list explicitly classifies it as "a distinct claim-bearing artifact, not itself doctrine" (item 7) — the plainest textual basis in the corpus for ranking a document below all doctrine and procedural-canon documents above it. |
| 11 | `docs/guidance/GOVERNANCE-ENFORCEMENT-FINDINGS.md` | None recorded ("Date \| 2026-08-02" header is a document date, not a ratification sign-off; the document is adversarial-review findings about runtime enforcement, not a doctrine text) | Narrow: a single static code-enforcement review of the rank-1/2 contract's implementation, explicitly "not a live/runtime test" | Property 3: narrower than rank 10 — a one-time enforcement audit of a single contract's runtime behavior, not a product-wide or even product-facing claim set. Discovered during this parcel's corpus read (not a Required source list item); entered per the "additional canon document the builder's corpus read turns up" allowance, document contract 2. |
| 12 | `docs/specs/active/BIO-PAIRWISE-001-relationship-substrate-and-label-census.md` through `BIO-PAIRWISE-006-studied-combinations-surface.md` (treated as one registry entry; six files, one `review-candidate` lane, identical governance status) | None recorded (each carries `status: review-candidate` in its own frontmatter, not a ratification event) | Narrow: a single product lane (pairwise negative-relationship substrate), per this parcel's Required source list, item 10: "narrower-scope precedent" | Property 3: narrower than every document above it — single-lane, pre-dispatch specs, explicitly named by this parcel's own spec as narrower-scope precedent rather than canon in its own right. |
| 13 | `docs/INITIATIVES/biostack-local-readiness/FINAL-HANDOFF.md` | None recorded ("Date. 2026-10-08. Coordinator handoff" is a record date, not a ratification sign-off) | Narrow: a single initiative's (`biostack-local-readiness`) closure record and claim-discipline precedent | Property 3: narrower than rank 12 — one initiative's own closure record, explicitly named by this parcel's Required source list (item 11) as a *precedent for method*, not doctrine content. |
| 14 | `docs/product/product-ids.md` | None recorded (the file is effectively empty — a single blank line, 2 bytes) | None stated — the file carries no content from which any scope can be read | Property 3: narrowest possible — an empty file states no doctrine, no procedure, and no claim of any kind to rank against. |

Required source list items 12-13 (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`,
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md`) are **excluded** from this
registry per the Classification guidance (section 1) — they are read and cross-referenced by
`CONTRADICTION-INVENTORY.md` instead (see CI-001's disposition and its cross-references to
CI-002/CI-004/CI-005).

Three additional documents the corpus read discovered (`docs/guidance/BUILDER-HANDOFF.md`,
`docs/guidance/OPS-SPINE-AND-SIDECAR.md`, `docs/guidance/pairwise-relationship-publication-contract.v1.md`)
and the three `docs/legal/*` files are **not** entered in this registry. Rationale, recorded for
transparency rather than silently dropped: the two `docs/legal/*.docx` files are binary
word-processor documents outside this parcel's byte-for-byte quotation-verification method (Hard
constraints, "Real-corpus grounding") and their own filenames (`v0.9`, `v0.10`) identify them as
pre-approval drafts, consistent with `README.md`'s "stubs pending approved policy" moratorium
(CI-007) rather than ratified canon; `docs/legal/POLICY-REDLINE-v0.9-to-v0.10.md` is a changelog
between those two drafts, not itself a canon document. `docs/guidance/BUILDER-HANDOFF.md` and
`docs/guidance/OPS-SPINE-AND-SIDECAR.md` are engineering handoff/operations notes, not
product-doctrine or procedural canon in the sense this registry ranks.
`docs/guidance/pairwise-relationship-publication-contract.v1.md` is a narrow-scope ratification
entry fully covered by its parent record, `docs/guidance/RATIFICATION.md` (already registered at
rank 2), and is outside the Required source list; adding it as a fifteenth registry row was judged
unnecessary scope expansion beyond what the spec requires ("may add sources the builder discovers
are load-bearing canon") since its content is already read and cited where load-bearing
(`CORPUS-COVERAGE-MATRIX.md`, pair 9-10).

---

## 3. Comparison function definition

```
compare(A, B) -> { A-outranks-B | B-outranks-A | open-tie, ruling-ref }

compare(A, B):
  if A not in registry or B not in registry:
    undefined (the function is total only over the registry, section 2)
  rankA := Rank(A)   # from section 2
  rankB := Rank(B)
  if rankA < rankB:  return A-outranks-B
  if rankA > rankB:  return B-outranks-A
  if rankA == rankB: return open-tie, ruling-ref = <the recorded ruling-ref for that shared rank>
```

**Proof of totality by construction:** section 2 assigns every registered document exactly one
dense integer rank, 1 through 14, with no gaps and no shared rank in the current corpus. For any
two distinct registered documents `A != B`, `Rank(A)` and `Rank(B)` are two distinct integers in
`{1..14}` (or, in a future extension, `{1..N}`), so exactly one of `Rank(A) < Rank(B)` or
`Rank(A) > Rank(B)` holds — `compare` therefore returns exactly one of `A-outranks-B` or
`B-outranks-A` for every registered pair, with zero pairs falling through to `open-tie` today.
Should a future extension (section 5) introduce a genuine property-1/2/3 tie, that pair receives a
**shared** rank and `compare` returns `open-tie` with the pair's recorded `ruling-ref` — a named,
valid outcome, not a failure to answer. `open-tie` is therefore part of the function's total
range even though the current registry never reaches it.

---

## 4. Scope boundary statement

This manifest ranks **documents**, not individual sentences. Where a single document contains
both higher-authority and lower-authority content (for example, a ratified product-doctrine
section and an unratified aside in the same file), `CONTRADICTION-INVENTORY.md` — not this
manifest — is the mechanism that surfaces the internal inconsistency; this manifest's
document-level rank applies only once that internal inconsistency is itself resolved, which is a
P0-D concern, not this parcel's. No registered document in section 2 was found, during this
parcel's corpus read, to require that internal-inconsistency carve-out for its own
document-level rank to be usable as stated.

---

## 5. Extension rule

A future document not yet in this registry is placed by applying the same three-property test
(section 1) against the current registry:

1. Determine whether the new document's own text states a dated, named-owner ratification event
   (property 1); if it does, compare its date against every registered document that also
   qualifies under property 1 (property 2); if it does not, or after property 2 leaves a tie,
   compare its stated scope breadth against the full registry (property 3), using the same kind
   of textual basis recorded in the "Precedence note" column above.
2. Append the new document at the computed rank. If the computed rank falls between two existing
   ranks, every rank at or below the insertion point increments by one (ranks are never silently
   renumbered without this being an explicit, logged amendment to this document — the amendment
   names the inserted document, its computed rank, and the property that placed it there).
3. If the new document ties an existing document on all three properties, both receive the same
   shared rank and are recorded as an `open-tie` pair with a named `ruling-ref` (an owner or
   P0-D1 ruling reference, or, pending such a ruling,
   `none-recorded-pending-P0-D1-or-owner-ruling`) — never an invented fourth tie-break criterion.
4. Existing ranks are never renumbered for any reason other than an explicit, logged amendment of
   this kind; the registry's rank column is never silently gap-filled or reordered.
