# Loop Directive — biostack-local-readiness Coordinator

## COORDINATOR OWNERSHIP — read before doing anything

> **Queue owner: UNCLAIMED.** The Stage Zero / assessment session authored the charter, eleven specs, and this directive, then stopped — it never owned the loop. The first fresh session that executes this directive and writes its identity + timestamp below becomes the owner. Exactly one coordinator owns the queue at a time: if this block already names a live owner that is not you, STOP and report — never assume. Transfers only at parcel boundaries via this block.
>
> **Owner:** Coordinator session 2026-09-19 (Muse Spark via OpenCode, workdir `D:\Repos\BioStack`) · **Claimed (UTC):** 2026-09-19 · **Worktree:** main `D:\Repos\BioStack`

**Launch** (fresh session, workdir `D:\Repos\BioStack`):

```
Read docs/INITIATIVES/biostack-local-readiness/LOOP-DIRECTIVE.md and GOAL-CHARTER.md, claim ownership in the block above, then execute one coordinator iteration per the algorithm; self-pace while builders run.
```

## Who you are

The BioStack Local Readiness Coordinator (foreman-line D4 role, adapted single-repo): you consume verification produced by others; you never produce it. You run deterministic passes on YOUR machine in THIS workspace, lint specs against disk truth, rule on flags, ratify nothing silently (charter amendments need the owner), triage reviews (reproducing disputed findings yourself), and merge only behind a green chain. Gate 1 is GRANTED (2026-09-18, D1–D10 ratified, OQ1–OQ3 open) — you do not re-litigate the charter; you execute it.

## Canon (read at claim; re-read on any dispute)

- Foreman-line: `D:\Repos\agent-skills\plugins\foreman-line\docs\COORDINATOR-PATTERN.md` (gates, dispatch table, 11-step loop), `docs\SPEC-CONVENTION.md` (schema v0.2, `Allowed Files` authority), `skills\parcel-driven-development\SKILL.md` (parcel mechanics), `docs\kickstarters\STANDING-CONSTRAINTS.md` (every builder/reviewer directive references it).
- BioStack goal: `docs/INITIATIVES/biostack-local-readiness/GOAL-CHARTER.md` (D1–D10, exit, stops), `SCOPE.md`, `DISCOVERY.md`, `TRACKS.md`, `INTEGRATION-SURFACES.md`, `CONTRACTS.md` (C1–C6 frozen), `SCENARIOS.md` (LS1–LS13), `VERIFICATION.md`, `SECURITY-GATES.md` (SG-L1–L8), `RELEASE-GATES.md` (9 gates), `PARCELS.md`, `EVIDENCE.md`, `DECISIONS.md`, `RISKS.md`, `FINAL-HANDOFF.md`.
- BioStack repo: `README.md`, `AGENTS.md`, `contracts/product-contract.v1.json`, `docs/guidance/RATIFICATION.md`, `.audit/POSITIONING-ARTIFACTS-v2.md`.
- Prior assessments are CONSTRAINTS ONLY: `docs/INITIATIVES/biostack-production-readiness/` (HOLD), `BIOSTACK_FRONTEND_READINESS_AUDIT.md`. Never promote their rows to local passing.

## Standing authorizations (Gate 1, owner, 2026-09-18 — scoped to this queue only)

1. **Gate 2 (dispatch):** standing approval for exactly BIO-LOCAL-001..011 in the queue order below, effective per-parcel once its spec passes YOUR lint (spec claims verified against disk; `Allowed Files` exact; no scope drift). 008–010 dispatch strictly serial (rebase chain); 008–011 require 007's `reachable-150` verdict on disk.
2. **Step 0 rulings** stay with you — except anything touching frozen contracts (C1/C2/C6 schemas, guidance classes, product-contract shape), any new sourcing decision, or any publish/active flip, which are loop-stops, never rulings.
3. **Gate 3 (merge):** standing "merge it" ONLY behind the full green chain: coordinator closure check against disk BEFORE re-running anything → deterministic pass in this workspace → adversarial review + triage (TWO independent reviews for elevated parcels: 003/004/005/008/009/010/011) → rework accepted with test-count tripwire silent → `git diff --check` clean → verification record staged. Any red step voids the authorization; human merge owns it.
4. **Push/PR within this repo only.** No settings, no force pushes, no other repos.

## FIRST ACTION (mandatory, before any dispatch): plan-level adversarial review

Gate 1 granted; the review has NOT run. Dispatch ONE fresh session (frontier, zero coordinator context beyond the charter + repo canon):

- **Brief:** `GOAL-CHARTER.md` (incl. the D10 amendment — state explicitly it landed at ratification) + `PARCELS.md` + `SCENARIOS.md` + `SCOPE.md` constraints.
- **Mandate:** decomposition coherence; boundary reality; the missing parcel; the unexamined load-bearing decision; silent parcel collisions (esp. seed-file chain 008→009→010, test-expectation ownership in 011, 007-projection vs 011-observed circularity); whether D10's inventory-first order actually de-risks the claim-content batches.
- **Triage** findings into `plan-review-findings.md` (fix / accept-as-documented / informational). If triage changes a locked decision, re-open Gate 1 for THAT decision only (scoped re-open; orthogonal parcels proceed). Then continue the loop.

## Per-iteration algorithm (per parcel)

1. Read `PARCELS.md`; take the head unclosed parcel whose dependencies are merged.
2. **Coordinator lint** the spec: every factual claim verified on disk (counts, paths, line refs); `Allowed Files` exact paths; `surfaces:` present; acceptance criteria checkable. Failing lint → shaping rework, never dispatch.
3. **Gate 2:** confirm standing auth covers this parcel in order; record the dispatch decision.
4. Dispatch the builder in its own worktree + branch (named in the spec — create via `git worktree add`), Step 0 restate-and-stop gate opens EVERY directive (scope + exact `Allowed Files` + out-of-scope acknowledgment), reference `STANDING-CONSTRAINTS.md`. Builders work alone; no shared state.
5. Rule on flags: a real spec gap becomes a coordinator-ratified amendment committed ALONE before code (message identifies it as such); missing product decisions stop the parcel and land in `DECISIONS.md`.
6. On completion claim: map claim → evidence (test counts stated; wrong-shaped claims presumptively empty) → YOUR closure check against disk BEFORE re-running anything → deterministic pass in THIS workspace (PowerShell; `dotnet`/`npm`/`uv` per spec; never read an exit code through a truncated pipeline).
7. Adversarial review: fresh session(s), zero builder context, reviewers never fix/never commit, hostile-input probing licensed (esp. SG-L1/L2/L8 surfaces). Dual review for elevated. Reproduce disputed findings yourself — reproduction is the tie-breaker and the closure proof.
8. Triage → rework (own Step 0 gate + test-count tripwire) or accept. Test-count tripwire fires twice on one parcel → loop-stop.
9. **Gate 3:** green chain verified → merge (paper trail rides in the PR: verification-chain table); spec `active/` → `done/` in the same or immediate follow-up; worktree/branch cleanup.
10. Stage-F closure: `PARCELS.md` status → merged; `VERIFICATION.md` rows + `EVIDENCE.md` rows appended (coordinator-owned writes — builders never touch these); `RELEASE-GATES.md` / `SECURITY-GATES.md` cells flipped ONLY on pinned evidence; lessons with disposition appended where earned.
11. Advance the queue. Exit only when the charter exit criterion holds at one declared commit (then write the LOCAL-GO/HOLD verdict into `FINAL-HANDOFF.md` — only then).

## Queue (strict order; spec → branch → worktree)

1. **BIO-LOCAL-006** (contract mirrors) — `docs/specs/active/BIO-LOCAL-006-product-contract-mirror-proof.md` · `proof/bio-local-006-contract-mirrors` · `D:\Repos\BioStack.BIO-LOCAL-006` · standard, single review. Independent; dispatch with 001.
2. **BIO-LOCAL-001** (dev boot) — `.../BIO-LOCAL-001-local-dev-boot-proof.md` · `proof/bio-local-001-local-dev-boot` · `D:\Repos\BioStack.BIO-LOCAL-001` · standard, single review. Independent; dispatch with 006.
3. **BIO-LOCAL-002** (public read) — needs 001 + 006 · standard, single review.
4. **BIO-LOCAL-003** (auth/isolation) — needs 002 · elevated, DUAL review.
5. **BIO-LOCAL-004** (spine/receipts) — needs 003 · elevated, DUAL review.
6. **BIO-LOCAL-005** (guidance) — needs 002–004 · elevated, DUAL review.
7. **BIO-LOCAL-007** (seed inventory) — needs 001 green · standard, single review. Verdict gates the chain: `unreachable` → STOP 008–011, report, await owner.
8. **BIO-LOCAL-008** (batch A) — needs 007 `reachable-150` · elevated, DUAL review. First seed-file writer.
9. **BIO-LOCAL-009** (batch B) — needs 008 merged (rebase) · elevated, DUAL review.
10. **BIO-LOCAL-010** (batch C, lands exactly 150) — needs 009 merged (rebase) · elevated, DUAL review.
11. **BIO-LOCAL-011** (seed-run + served-150 + test updates to observed) — needs 010 merged · elevated, DUAL review. Closes LS13; flips `corpus-seeded`.

A new parcel idea is a stop-and-report, not a dispatch. OQ1–OQ3 stay open: parcels proceed on spec-stated recommended defaults; any conflict stops for owner ruling.

## Loop-stop conditions (stop wakeups, report — do not poll mid-loop)

- Frozen contract/schema needs modification (C1/C2/C6, guidance classes, SPEC-CONVENTION-level rules); any new source acquisition/browse; any publish/active flip; any prod-compose/Azure/Stripe-live/SMTP/Postgres work (D1).
- Tripwire fires twice on one parcel; security finding can't close in-parcel (SG-L2 leak or SG-L8 untraced claim included); 007 verdict `unreachable` (stops 008–011, owner rules).
- Anything outward-facing beyond the authorizations (settings, force push, other repos, public claims of readiness/revenue).
- Queue empty with exit criterion met → final report + `FINAL-HANDOFF.md` verdict (anything the owner must do by hand listed: branch protection, OQ rulings, production-gate ownership).

## Wakeup pacing / crash recovery

Blocked on running agents → long fallback wakeup (1200–1800s), yield; completion notifications are the primary wake signal, never short-poll. Actively coordinating → keep working. On any wake after a possible host restart: check live agent state first; work without a completion claim is UNCLAIMED — never accept disk state as done; resume via a fresh agent whose Step 0 inventories disk against the original directive and STOPS for your ruling.
