# Coordinator Decisions — 2026-10-07

Coordinator-owned record. Owner delegated decision authority for the pairwise lane in session
("I authorize you to make the decision") and ratified amendment R2 ("Ratify R2").

## R2 ratification (owner, 2026-10-07)

Amendment R2 in `biostack-governed-delivery/dispatch/P1-REWORK-2026-10-07.md` is **ratified**:
the worktree-literal equivalence mapping (`d:/repos/biostack-governance-p1` ≡
`/home/cmorgan76/Repos/biostack-wt/governance-p1`) stands as the accepted environment
reconciliation. The deviation remains disclosed in `CLOSURE-P1.md` as accepted, not hidden.
Fallback (full P1 re-dispatch) is not needed.

## D-C — D10 corpus ruling (owner delegated option selection, 2026-10-07: "a")

BIO-LOCAL-007 returned `VERDICT: REACHABLE-100` (57 existing + 43 harvestable; gap 50 to the
150 target requires new source acquisition). Per D10 the owner rules; the owner selected option
**(a)**: seed the reachable 100 now — batches 008/009/010 proceed against the reachable 43-record
set (target 100 total), and the 50-record gap is HELD pending future sourcing under the KEO-73/74
(gates) path. Consequences:

- Amendment A2 to the batch specs (text unchanged): their "blocked on 007 `reachable-150`"
  precondition is satisfied by `REACHABLE-100` + this ruling; batch ID lists are exactly the
  three partitions in `evidence/BIO-LOCAL-007-seed-gap-inventory.md`.
- D10's unknown-honest rules apply in full (draft/needsReview/inactive; claims copied verbatim
  from cited evidence-packet source lines; no invented claims; collisions to human rule).
- The 50-record gap gets an explicit gap record in BIO-LOCAL-011's evidence; count-asserting
  frozen tests are updated to the ACTUAL counts explicitly in 011 (per D10's in-parcel rule).
- New sourcing for the gap remains unauthorized until KEO-73/74 gates clear.

## D-D — Seed identity-collision human rules (owner, 2026-10-08)

The unknown-honest doctrine reserves identity collisions for human rule; the owner ruled:

1. **`creatine` vs `creatine-monohydrate`: DISTINCT and cross-referenced.** Both records remain.
   "Creatine comes in many forms" — the records are related, not identical. Cross-references added
   in whatever fields the frozen schema supports; where it has none, the mapping is documented in
   the consolidation evidence record (unknown-honest: no invented schema fields).
2. **`chorionic-gonadotropin` vs `human-chorionic-gonadotropin`: SAME identity.** "Exactly the
   same hormone; the word 'human' is just left out in some medical labels and shorthand."
   Consolidate to ONE record; canonical = `human-chorionic-gonadotropin` (fuller standard term),
   `chorionic-gonadotropin` recorded as a known shorthand/alias per schema capability (else
   documented in the evidence record). Corpus total becomes 99; count-asserting tests updated
   EXPLICITLY in the consolidation parcel.

Both rules execute in BIO-LOCAL-014 (bounded seed-consolidation parcel). No other collisions are
open (batches B/C introduced none).

## Pairwise lane decisions (coordinator, under delegated authority)

### D-A — Schema sufficiency pre-gate (BIO-PAIRWISE-002 constraint)

**Decision: proceed with P0 dispatch; pre-authorize a bounded schema-change parcel if and only if
P0's census proves `relationship-packet.schema.json` insufficient. The lane is NOT de-scoped.**

- Rationale: the pairwise negative-relationship lane has direct product value (interaction
  intelligence and harm-reduction signals); de-scoping on a hypothetical schema gap would forfeit
  it. A schema change is legitimate governance work when evidence demands it — it just needs its
  own ratified parcel.
- If P0 finds the schema sufficient: BIO-PAIRWISE-002 proceeds on the existing frozen schema; no
  schema parcel is created.
- If P0 finds it insufficient: the coordinator shapes `BIO-PAIRWISE-SCHEMA-001` (bounded to the
  proven gaps: relationship types, assertion classes, source references, evidence tiers), and
  BIO-PAIRWISE-002 waits on it per dependency order. Schema change remains a separate ratified
  parcel exactly as the spec demands — this decision pre-authorizes its SHAPING, not its merge.

### D-B — Migration vs. redesign (BIO-PAIRWISE-003 constraint)

**Decision: P2 proceeds code-only in ALL outcomes. No migration is authored in P2 under any
circumstance.**

- If P0 recommends abandoning the non-authoritative input path: P2 makes that path explicitly
  inert (fail-closed, documented), keeping the authoritative path untouched.
- If P0 recommends the hard-coded arrays must stay: same shape — P2 marks the packet path
  non-authoritative and inert rather than migrating anything.
- If P0's finding genuinely cannot be honored code-only: P2 stops and reports; a migration would
  be a separate future parcel requiring owner approval. That door stays closed here.

Both decisions preserve the specs' hard constraints (no in-lane schema authoring, no migration)
while removing the pre-dispatch deadlock. Recorded for Gate 2 reference by BIO-PAIRWISE-001..006.

## D-E — BIO-PAIRWISE-005 rulings (coordinator, delegated pairwise authority, 2026-10-08)

Spec review REJECTed the first draft with three blockers. Rulings:

1. **Sourcing discipline (F1):** derivation from already-authorized, already-cited lane sources
   IS permitted — it is verbatim-citation derivation from existing evidence, the same doctrine as
   D-C's corpus rule. Genuinely NEW external source acquisition invokes the KEO-73/74 gates and
   STOPS until they clear. The spec's Constraints must state this split explicitly.
2. **Delivery class (F2):** reclassify BIO-PAIRWISE-005 as `knowledge-promotion` — a sourced
   negative pair reaching the public projection is a promotion surface. D14 controls apply in
   full: dual review, source/license/provenance + evidence grade + review lifecycle + promotion
   authority + rollback sections, and the class's stop conditions (missing source/license/review
   state; bypassed promotion; unreviewed public claim).
3. **Testability (F3):** Required Tests section added (sourced-pair validation, page-image
   verification where applicable, schema compliance).

The fixer applies these to the spec; re-review at the new hash precedes Gate 2.

## D-F — Owner rulings (2026-10-08, all "as recommended")

1. **Onboarding routes:** `/start` becomes canonical; `/map` and `/onboarding` become 301
   redirects with a mode toggle inside the canonical experience. (Unblocks the frontend polish
   wave as BIO-FE-002.)
2. **Production lane kickoff:** PR-PROV-001 (provider operations) + deployment-config review
   START NOW in parallel; Stripe remains TEST-MODE until the KEO-68 runbook validates; owner
   Clint Morgan is the named release owner for PR-REL-001. (The production initiative's
   NO-GO/HOLD verdict is unchanged until its own gates clear with evidence.)
3. **KEO-73/74 sourcing:** derivation-only pairs first (per D-E's split); the KEO-73/74 gate
   review opens ONLY when a builder stops with proof of acquisition need.
4. **Anchor mutual-binding migration (H2-AC3 residual):** DEFERRED to the production migration
   wave (one migration window). Residual stays disclosed in the hardening register.

## D-G — P0-A authorization (owner, 2026-10-08: "P0-A authorized")

The owner explicitly authorizes **P0-A (Product Doctrine Recovery: canon precedence and
contradiction inventory)** ONLY — the charter grants no standing authorization for P0, so this is
the required explicit human gate for P0-A's shaping → review → dispatch chain. Scope bound to the
analytical inventory (read-only against product behavior). **P0-B remains UNAUTHORIZED** and
returns to the owner for a fresh gate with its design presented (it decides product
allowed-outputs). P0-C/P0-D likewise await their own gates. Class controls for P0-A in full:
health-boundary + privacy + legal-policy + knowledge-promotion sections, dual review, and the
human-approval conditions those classes trigger at merge.

## D-H — Owner directive: Gate 3 posture + P0-B design gate opened (owner, 2026-10-08)

Owner directive, verbatim (via `/foreman-line:goal resume`, 2026-10-08):

> resume go-live initiative to help 1,000,000 people. gate 3 is cleared and open. On to P0-B
> design gate — the one where we decide what the product may say. 🚀

Recorded as two distinct grants:

1. **Gate 3 posture — "cleared and open".** Coordinator scope reading (owner may correct):
   Gate 3 merges with fully green chains (every deterministic check, required review, rework
   check, acceptance-evidence item, and coordinator reproduction complete — D9's voiding
   conditions unchanged) may proceed without per-merge stops. **Still owner-only regardless of
   this grant:** every human-approval condition the D14 fold triggers at merge (health-boundary,
   privacy, legal-policy, knowledge-promotion — D9's written-delegation rule); any production
   deployment or production-readiness verdict change (D17); billing/Stripe, legal-policy
   effectiveness, provider-pilot expansion, data deletion. At recording time zero PRs were open
   (nothing was pending at Gate 3).
2. **P0-B design gate OPEN.** The owner invokes the fresh gate D-G reserves to the owner — the
   gate "where we decide what the product may say." The design is presented in
   `docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md` (decision set D-B1..D-B6,
   including the central canon conflict between guidance-content-contract v1.0.0 Class D
   (prohibited) and charter D13 (allowed) and the recommended staged split). **P0-B dispatch
   remains unauthorized** until (a) the owner's D-B1..D-B6 rulings are recorded and (b) P0-A
   freezes canon precedence per the charter dependency spine. Gate rulings, once given, are
   product doctrine of record and are encoded verbatim into the P0-B parcel spec.

## D-I — P0-B design gate RULED (owner, 2026-10-08: "D-B1: c · D-B2–D-B6: as recommended")

The owner rules on the design presented in
`docs/INITIATIVES/biostack-governed-delivery/P0-B-DESIGN-GATE.md`:

1. **D-B1 = (c) staged split.** Deterministic math on user-entered values + guidance-contract
   v1.0.0 Class A/B/C surfaces are the product's current posture. `biostack-recommended`
   origination (dose targets, schedules, profile-aware picks) is defined in the P0-B contract but
   publicly enabled only after guidance-content-contract **v2.0.0** re-ratification +
   `legal_product_ratification` — a separate owner event.
2. **D-B2–D-B6 = as recommended.** The per-label behavior matrix (D-B2), fail-closed numeric
   provenance rules (D-B3), missing-input ladder (D-B4), function-review/public-enablement rules
   (D-B5), and escalation semantics (D-B6) are adopted exactly as presented.

No locked §1 item is changed; no charter amendment is required. Effects: the D-B1..D-B6 matrix
is now **frozen as P0-B's normative design input** and will be encoded verbatim in the P0-B
parcel spec. **P0-B dispatch remains gated** on P0-A freezing canon precedence (charter
dependency spine); the §2 canon conflict is resolved in substance by D-B1(c), and P0-A's
contradiction inventory records its disposition by this ruling.

## D-J — P0-B definitional amendment: operational definition of "dose-context" (coordinator, 2026-10-09)

Spec-gap contingency (loop-directive rule: any spec gap becomes a ratified amendment committed
alone before code). P0-B re-review (`p0b_re_review_2` F1) found that the owner-ruled cell
`pregnancy-or-lactation` × C1 = `R (dose-context)` was made machine-checkable without the term
"dose-context" ever being operationally defined in canon. This amendment pins the definition
**mechanically and conservatively — it does not change any ruled cell, row, or rule** (the
reviewers byte-verified the matrix; that remains frozen). Owner may override; override reopens
only this definition.

> **Dose-context output.** An output is in dose context if and only if any value it presents or
> derives is a **compound amount or an amount-derived quantity**: a dose or target amount,
> concentration, reconstitution/dilution volume, split or load amount, per-administration or
> per-period amount, cumulative amount, or a syringe-unit rendering of any of these. The five
> D-B3 numeric-provenance origins attach exactly to these values. Outputs carrying no
> compound-amount value (e.g., calendar/interval arithmetic over non-amount quantities) are not
> dose-context. The guidance-contract v1.0.0 term "public dosing-context UX" denotes user-facing
> surfaces that render dose-context outputs, and therefore gates identically.

This definition is transcribed verbatim into the P0-B contract spec (amendment commit alone,
before any P0-B implementation code).

## D-K — Precedence-manifest directional application constraint (coordinator, 2026-10-09)

Triggered by P0-A implementation review (`p0a_impl_review_2` F1, BLOCKER-candidate; severity
reproduced as a real interpretive hazard): the manifest's total order, mechanically applied to
still-open rows CI-002/CI-005 (CHARTER.md's broad "may" list vs the narrower safety prohibitions
in `docs/canon/biostack-protocol-intelligence-canon.md` and
`docs/product/knowledge-engine-capability-map.md`), resolves authority in favor of the more
permissive text — and no artifact warned against treating that as license to weaken a safety
prohibition.

Constraint (fails safe; owner may override — override reopens only this rule):

> The precedence manifest answers **document authority order only**. Where the mechanically
> resolved order favors a more permissive text over a narrower safety prohibition, the
> prohibition **stands unchanged** until an explicit owner ruling supersedes it. Such rows route
> to the owner through P0-D's disposition process; a P0-D builder must never weaken a safety
> prohibition by rank alone. This is consistent with the charter's must-not list, D12's principle
> (controls calibrate useful guidance; they do not license weakened safety), and D-I's staged-split
> posture, all of which are already the owner's ruled doctrine.

This is procedurally conservative — it cannot make the product say more than the owner ruled; it
can only stop rank-mechanics from silently saying more. Transcribed into the P0-A artifacts by
the remediation builder; recorded here as the amendment of record.

## D-L — Coordinator ratification: P3-B 17th-path deviation (2026-10-09)

`p3b_builder` (PR #533) needed the P3-B spec file, which was reviewed (`7b3225c`, APPROVE) but not
yet merged to `main` at its BaseCommit. It carried the file in **byte-identical** (independently
byte-verified by both implementation reviewers: identical to `git show 7b3225c:...`) as a
disclosed 17th changed path — instead of stopping per the spec's Stop Conditions. Both reviewers
flagged the process deviation (R1-F1 / R2-F2, MAJOR-process). Ratified as follows: (1) the
deviation is **accepted-as-documented** — content is reviewed canon at the reviewed hash, zero
semantic drift, and the spec subsequently merged unchanged (#532); (2) the process failure is
**acknowledged**: builders must STOP on surface-count/deviation conflicts, not self-reconcile —
recorded as a standing dispatch note for all future parcels (Gate 2 records already carry
STOP-AND-REPORT language; enforcement emphasis added); (3) no product allowed-output consequence
exists. Owner may override.

## D-M — P4 amendment A-P4-2: three pins + one process ratification (coordinator, 2026-10-10)

Triggered by the P4 implementation review split (R1 `PASS` vs R2 `FAIL`; R2's F1 reproduced at
code level — stage 5 scans only headings+frontmatter, never `$parsed.Body`, contradicting the
spec's own stage-5 text). Owner may override any pin.

1. **A-P4-2a (process ratification, F2):** the builder's D1 technical rationale is accurate —
   the spec's `pilot-rollback-alias` example is self-contradictory (it contains the bare term it
   is meant to distinguish). Ratified: the corrected `expected.result` for
   `positive-real-spec-p3b-cross-check.json` becomes the pinned value, the spec's example is
   amended to a non-self-contradictory one, and AC-P4-05's cross-verifier-agreement scope is
   restored to full agreement (not the builder's narrowed form). The builder's process failure
   (unilateral override without STOP-AND-REPORT) is recorded under D-L's enforcement.
2. **A-P4-2b (F3):** `missing-required-field` is ratified as the **13th** literal of the closed
   reason vocabulary (it is a pre-existing P2 literal, not an invention) and must gain its own
   fixture.
3. **A-P4-2c (F4):** the `extension_section` frontmatter key is ratified as a named P4 convention
   and pinned in the spec (it was builder-invented and unpinned).
4. **F5:** fixture `expected` outcomes are hash-pinned in the verifier (independent of its own
   re-derivation).

## D-N — P4 amendment A-P4-3: stage-5 scan-composition semantics + amendment-commit exclusivity (coordinator, 2026-10-10)

Triggered by the P4 remediation-1 re-verify split (`p4_reverify_1` **FAIL** vs `p4_reverify_2`
**PASS-WITH-FIXES**). Per D8, every disputed or shared finding was reproduced at code level
before this triage. Owner may override any pin; override reopens only the pinned item.

### Reproductions (coordinator, live at remediated hash `45d5995f`)

1. **R1's disputed F1-REMAINS BLOCKER — REPRODUCED, HOLDS.** A `coordinator-parcel`-shape file
   with clean frontmatter and clean headings whose section bodies are isolated `TBD` paragraphs
   (the exact PoC class R2 originally demonstrated) returns `result: "valid"` at the remediated
   hash. Root cause confirmed at the normalization layer: `strip-unicode-category-Cf-and-Cc`
   (U+000A/U+000D are Unicode category Cc) deletes newlines without inserting a separator, gluing
   `## Objective\nTBD` into `## ObjectiveTBD` and `TBD\nTBD` into `TBDTBD`, which defeats both
   pinned `noPlaceholderPatterns` (`\b` boundaries gone; `^` line anchors gone). Normalized-text
   proof: `[## ObjectiveTBD## ContractsTBD]`, `(?i)\b(TBD|TODO|FIXME)\b` → `False`. Control case
   (space-separated `TBD` mid-paragraph) is correctly caught (`invalid`/`placeholder-violation`) —
   so the body scan runs; line-isolated placeholders, the dominant real-world shape, evade it.
   No fixture in the 17-fixture suite exercises body prose at all.
2. **Shared finding (R2 F-R2-1 BLOCKER / R1 F6-NEW MAJOR) — REPRODUCED by both reviewers (R2
   dynamically, via disposable clone) and confirmed by coordinator code reading.** The
   amendment-commit carve-out in `verify-p4.ps1` check 4 asserts only (a) the code commit does
   not touch `parcels/P4.md` and (b) `BaseCommit..HEAD~1` carries a `parcels/P4.md` change — it
   never asserts the amendment commit is EXCLUSIVE to `parcels/P4.md`, and `HEAD~1` is checked as
   a range, not as a commit. Any non-frozen path (e.g. `validate-spec.ps1`, `verify-p4.ps1`
   themselves) smuggled into the "spec-only" amendment commit passes a clean `P4 verification
   PASS`. This is the D-L failure class in verifier form.
3. **R1/R2 shared MINOR (anchor semantics) — accepted as a record defect + a verifier hygiene
   gap.** The remediation-1 dispatch record's `baseCommit` (the D-M ledger commit) is correct for
   topology but is not a valid `verify-p4.ps1 -BaseCommit`: check 3's expected surface set is
   anchored to the ORIGINAL P4-IMPL dispatch anchor, and a wrong-anchor run dies on a raw
   assertion (`Expected 25, got 4`) rather than a named check failure. Both reviewers
   independently derived the correct run anchor and got deterministic double-run `PASS` at it.
   Additionally, check 3's `KnownOutOfScopeCoordinatorPaths` tolerance (three coordinator paths)
   was builder-invented to absorb main-side coordinator commits in the original anchor span — it
   is exact-pinned but manually grown, i.e. fragile-by-construction and outside check 3's own
   "no open-ended tolerance" promise. All of this is ratified-and-reframed by A-P4-3c below; the
   remediation-1 dispatch record gets a corrective anchor note.

### A-P4-3 (ratified pins; transcribed verbatim into `parcels/P4.md` in the amendment commit, alone, before code)

**A-P4-3a — stage-5 scan-composition semantics (fixes reproduction 1).** The nine pinned
`placeholderNormalizationSteps` remain applied in their exact pinned order and are never modified
(`parcel-spec.schema.json` stays byte-frozen). Stage 5 composes TWO normalized views of the same
source text (frontmatter values, headings, and body text), and a placeholder match in EITHER view
is a violation:

1. **Merged view:** the current composition — scan parts joined, pipeline applied to the whole —
   preserving today's detection of placeholders split across line boundaries (e.g. `TB` + newline
   + `D` glues to `TBD` and is caught). This view's behavior is unchanged.
2. **Line-preserving view:** the source text is split into its original lines; the identical
   pipeline (same steps, same order) is applied to each line SEPARATELY; the normalized lines are
   then scanned line-by-line (each line scanned against both pinned patterns, with `^`/`$`
   anchoring intact). Newline deletion can no longer glue a line-isolated placeholder into a
   neighboring word.

Both views are computed from the same input text by one shared implementation used by BOTH
invocation modes (`-SpecPath` disk mode reads the real file's body; `-SyntheticSpecJson` mode
reads the synthetic input's new `body` field, below). Closed-world: there is no third view, no
exclusion beyond the two already-pinned fixture spans from P4's original check 8, and no
warning-only acceptance.

**A-P4-3b — amendment-commit exclusivity (fixes reproduction 2).** The carve-out is retained but
made mechanically exclusive. A conforming remediation-style branch is EXACTLY two commits above
its dispatch anchor: (1) the amendment commit, whose changed-path set must equal exactly
`docs/INITIATIVES/biostack-governed-delivery/parcels/P4.md` and nothing else (checked with
per-commit path enumeration, not range diffs); (2) the code commit, which must not touch
`parcels/P4.md`. Any other commit count (`branch-shape-not-amendment-then-code`), any extra path
in the amendment commit (`amendment-commit-not-exclusive`), or any `parcels/P4.md` change in the
code commit fails by the named identifier given here. These identifiers are
`verify-p4.ps1`-local check-failure names in the AC-P4-05 sense, not `validate-spec.ps1` output
`reason` literals.

**A-P4-3c — anchor and surface-set semantics (fixes reproduction 3).** The full deterministic run
takes `-BaseCommit` = the build's own Gate 2 dispatch anchor (the builder's branch base), and
check 3's expected changed set is computed anchor-relative: exactly the paths the build's amended
surface enumeration lists (the amendment commit's `parcels/P4.md` + the code-commit surfaces
enumerated in the amended document contracts, including every new fixture), sorted ordinally, no
more, no fewer. The `KnownOutOfScopeCoordinatorPaths` tolerance is DELETED; a fresh-anchor run
needs none. A run attempted at any anchor whose changed set disagrees fails by named identifier
`anchor-surface-set-mismatch` — a clean named failure, never an uncaught exception. Honest-scope
sentence added to the spec: the verification anchor for any P4-lineage build is that build's own
dispatch anchor, and the historical full-lineage run anchor (the original P4-IMPL dispatch
anchor) is named in the spec as historical context only.

**A-P4-3d — body-prose fixtures + pins (the regression proof).** Two new fixtures, hash-pinned
per D-M's F5 rule, exercising the shared body path via the synthetic input's new OPTIONAL `body`
string field (absent = today's behavior, unchanged; present = flows through the identical
stage-5 dual-view scan as disk-mode body text):

1. `negative-body-prose-isolated-paragraph-placeholder.json` — body is line-isolated `TBD`
   paragraphs under clean headings (reproduction 1's exact shape); `expected.result: "invalid"`,
   `expected.reason: "placeholder-violation"`.
2. `negative-body-prose-split-token-placeholder.json` — body contains a placeholder split across
   a line boundary (`TB` / `D` on consecutive lines); `expected.result: "invalid"`,
   `expected.reason: "placeholder-violation"` — pins the merged view's continued necessity.

A source-level structural assertion (check 5 class) additionally requires disk-mode and synthetic
body text to flow through one shared stage-5 implementation — no second, divergent scan path.

### Process notes

- Re-verify verdict handling: `p4_reverify_2`'s PASS-WITH-FIXES stands as evidence for every item
  it marked clean (A-P4-2 pins byte-match D-M; F5 hash pins independently tamper-tested; 12
  adversarial body variants held against the MERGED view). Its verdict is superseded only on the
  two reproduced findings. `p4_reverify_1`'s FAIL stands.
- Fix-then-reverify: `p4_fixer_2` on `fix/p4-remediation-2` (amendment commit first, alone, then
  code); bounded re-verify after (fresh reviewers, with reproduction 1's shape as a pinned
  regression), then P4 closure. No locked decision is reopened by any pin above.
- Verifier scar tissue applied to the new work itself: pin anchor pairs (BaseCommit = dispatch
  anchor, HEAD = builder tip, is-ancestor-check every reviewed SHA); scan body prose (the very
  defect); byte-pin all normative text; closed-world citation rules; guard empty-content paths
  (empty body changes nothing about non-placeholder stages); hash-pin fixture expectations;
  honest scope claims; structural-block quote matching; STOP-AND-REPORT on any deviation
  (D-L — no self-reconciliation).
