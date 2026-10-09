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
