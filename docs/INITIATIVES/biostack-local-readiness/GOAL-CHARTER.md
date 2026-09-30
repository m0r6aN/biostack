# Goal Charter — BioStack Local Readiness (foreman-line)

**Goal slug:** `biostack-local-readiness`
**Created:** 2026-09-18
**Owner:** Clint Morgan
**Coordinator:** current session (Stage Zero + assessment author)
**Stage Zero baseline:** `BioStack @ e5b75e0` (`main`, in sync with `origin/main`); plugin `D:\Repos\agent-skills\plugins\foreman-line` (`docs/COORDINATOR-PATTERN.md`, `docs/SPEC-CONVENTION.md`, `skills/parcel-driven-development/SKILL.md`)
**Mode:** assessment → bounded remediation. No deployments. Local only.

## Objective

Carry BioStack from "reportedly works locally" to "proven locally at one pinned commit": a green, walkable chain from `docker-compose.dev.yml` boot through public evidence/tools, authenticated isolation, governance Spine/receipts, guidance enforcement, and contract-mirror integrity — each claim backed by a local artifact recorded in `VERIFICATION.md`. Anything not proven stays `unverified` and blocks LOCAL-GO unless waived by name.

## Exit criterion

This goal is complete only when ALL of the following are true at declared commit `e5b75e0` (or a named successor if a parcel forces a new SHA, with re-verification):

1. BIO-LOCAL-001..006 have each completed the parcel loop (shaped spec → coordinator lint → Gate 2 → isolated builder → deterministic pass on this machine → adversarial review → Gate 3 human merge decision) with sealed receipt references in `VERIFICATION.md`.
2. `RELEASE-GATES.md` shows every local gate `passing` with a pinned artifact, or `waived` with owner + rationale + expiry — zero `unverified`/`failing` blockers.
3. `FINAL-HANDOFF.md` records LOCAL-GO or HOLD with the full gate/finding/risk tables; no public, revenue, deployment, or privacy claim exceeds the evidence.
4. No frozen contract was modified; `docs/specs/active/BIO-LOCAL-*.md` moved to `docs/specs/done/` on merge per SPEC-CONVENTION §3; `docs/specs/INDEX.md` (if adopted) regenerated.
5. The corpus target (D10) holds: `Seeds/substances-seed.json` contains exactly 150 schema-valid records, all `reviewStatus: draft` + `needsReview: true` + `isActive: false`; a local SeedJob run ingests all 150; the local knowledge API serves 150 compounds; the frozen inventory/report tests assert the new counts (updated explicitly, never silently).

Completion does NOT imply staging/production readiness, live billing, live email, Azure deployability, SEO/browser sign-off, or provider operations — those remain under the HOLD production initiative and are explicitly excluded.

## Locked decisions

| # | Decision | Reasoning |
|---|---|---|
| D1 | Local-only truth. Only `docker-compose.dev.yml` + SQLite + stubbed Keon + in-memory inbox count as proof. Prod-shaped compose, Azure, Stripe live, SMTP, Postgres drills are out of scope and any parcel attempting them stops. | The user scoped this goal local-only/no-deployments; mixing in hosted proof reintroduces the exact staging-surprise the production HOLD already documents. |
| D2 | Single-repo initiative mode (PDD multi-project discipline inside one repo). Tracks are frontend, API/data, knowledge pipeline, governance/cognition, contracts/evidence — not separate repos. | BioStack is a monorepo with real cross-surface boundaries (auth, tenant, receipt, guidance, contract); repo-local mode would under-track the boundaries. |
| D3 | Specs are the unit of dispatch; the six BIO-LOCAL specs under `docs/specs/active/` are the ONLY dispatchable remediation/proof work. Jira is not used (no Jira MCP in this repo); `docs/INITIATIVES/biostack-local-readiness/PARCELS.md` is the tracking projection. | SPEC-CONVENTION D1 adapted: Git stays implementation truth; without Jira, the parcel index + verification log carry delivery state and must not drift. |
| D4 | Coordinator ≠ verifier. Deterministic passes run on the coordinator's machine in this workspace; adversarial review is a fresh session with zero builder context; reviewers never fix, never commit. | COORDINATOR-PATTERN + PDD hard rules; prevents self-graded local passes. |
| D5 | `contracts/product-contract.v1.json` v1.0.0 and Guidance Content Contract v1 (+ RATIFICATION.md incl. B3/B4/B5 + severity-null correction) are FROZEN for this goal. Any needed change is a loop-stop + version bump + new ratification, never a silent parcel edit. | Both contracts carry billing/entitlement and safety-class authority; silent drift voids every downstream gate. |
| D6 | Reduced-shape honesty is load-bearing: anonymous/Observer interaction/overlap responses carry pair names/ids + explicit `severity: null` only; no Description/Reason/Confidence/mechanism/direction/consequence; null means unavailable, never low/no-risk. | Owner-ratified B3/B4/B5 + correction; a parcel that re-exposes reasoning to the wrong tier fails its review by definition. |
| D7 | Unverified ≠ passing. Every scenario/gate without a pinned local artifact is `unverified` and blocks LOCAL-GO until proven or formally waived (finding ID, rationale, owner, date, re-assessment timeline). | Go-live hard rule 1; the optimism sweep is the failure mode being engineered out. |
| D8 | Standing authorizations REQUESTED (effective only after Gate 1): Gate 2 — standing dispatch for exactly BIO-LOCAL-001..011 in dependency order (008→009→010 strictly serial; see parcel table), once each spec passes coordinator lint; Gate 3 — standing "merge it" ONLY behind a full green chain (deterministic pass + review triage + `git diff --check` + updated verification record), otherwise human merge owns it. Gate 1 is never delegable. | Mirrors the proven foreman-ops-console D8 shape; keeps throughput without surrendering merge authority. |
| D9 | One parcel, one branch, one worktree (`D:\Repos\BioStack.<parcel-id>`), named in the dispatch; rebase onto `origin/main` immediately before PR; serialization points (`contracts/*`, `middleware.ts`, `InteractionIntelligenceProjection`, Spine chain, `scripts/sync-product-contract.mjs` mirrors, compose files, `Seeds/substances-seed.json`, inventory/report test expectations) are sequenced High-risk, never parallel-touched. | PDD hard rules 2/5/6; prevents the collision pileup this repo's ~100-worktree history warns about. |
| D10 | Targeted 150 compounds seeded (owner addition, ratified at Gate 1 2026-09-18). `Seeds/substances-seed.json` grows 57 → 150 with schema-valid substance records; every new record is `reviewStatus: draft` + `needsReview: true` + `isActive: false` + `completeness: partial` (unless fully evidenced) + `ops.lastChangeType: seed`; unknown fields stay unknown-honest (the "not reviewed" pattern, never inference); every claim sentence traces to an existing repo evidence-packet source line — no new source acquisition, no model-invented claims, no browsing; identity collisions resolve by human rule, never auto-merge; the frozen count-asserting tests (`CorpusIdentityInventoryBuilderTests`, `StructuralEvaluationReportBuilderTests`) update to the new counts explicitly in-parcel. Reachability is proven by inventory first (007): if 150 is not reachable from existing repo inputs without new acquisition, expansion batches stop and the owner rules (KEO-73/KEO-74 gates apply to any new sourcing). |Owner-directed launch-corpus target; draft-only + needsReview preserves the source-first, human-review-before-publication pipeline; the inventory-first order prevents authoring claims the repo cannot support. |

## Open questions for Gate 1 (recommendation attached to each)

| # | Question | Recommendation |
|---|---|---|
| OQ1 | Should parcels run `dotnet test` / frontend suite in full, or focused + build only, given local resource cost (prior worker OOM noted)? | Full backend `dotnet test` for 001/004/005/006 lanes + exact frontend suite + production build for 002/003 lanes once; focused re-runs on rework with a test-count tripwire. Full-suite cost is the proof. |
| OQ2 | Docker boot (001) vs host-only test evidence: must the API/frontend run inside `docker-compose.dev.yml`, or is host `dotnet`/`npm` sufficient? | Docker compose boot is REQUIRED for 001 (ports, healthchecks, SQLite volume); host runs are supplementary, never substitutes — environment specificity is mandatory. |
| OQ3 | Do we adopt `docs/specs/INDEX.md` for BioStack in this goal? | Yes, minimal INDEX regeneration as part of parcel closure (ticket→spec mapping + status); it is discovery projection, never authority. |

## Wave/parcel decomposition (dependency order)

| Parcel | One-liner | Risk | Routing class |
|---|---|---|---|
| BIO-LOCAL-001 | Local dev-stack boot proof: compose up, `/health`, `/health/keon` (stubbed posture stated), frontend 3043 reachability, SQLite volume behavior, reset procedure. | standard | standard-feature — mid-tier builder, single review |
| BIO-LOCAL-002 | Public knowledge/tools read proof: `/knowledge` + dossier + methodology anon read, contract route aliases, calculators/analyzer anon availability, TierGate public honesty (no reasoning leak). | standard | standard-feature — mid-tier builder, single review |
| BIO-LOCAL-003 | Auth + tenancy isolation proof: magic-link (in-memory inbox) + passkey posture locally, protected-route denial, cross-user profile/protocol denial, provider-intake non-enumeration + limits. | elevated | architecture/risk — frontier builder, dual review |
| BIO-LOCAL-004 | Governance Spine + receipt proof: hash chain round-trip (SQLite), signed checkpoints, receipt anchoring posture (stubbed), fail-closed boot check unit proof, receipt-view auth gating. | elevated | architecture/risk — frontier builder, dual review |
| BIO-LOCAL-005 | Guidance contract enforcement proof: Class A–D gates incl. B3/B4/B5 reduced shapes + severity-null correction, backstop test suite green, no Class D leakage locally. | elevated | architecture/risk — frontier builder, dual review |
| BIO-LOCAL-006 | Product-contract mirror proof: v1.0.0 shape, `--check` green, entitlement mapping (`FeatureGate`), monthly-only + grace-zero + alias/health-path assertions, drift guard. | standard | standard-feature — mid-tier builder, single review |
| BIO-LOCAL-007 | Seed-gap inventory: measure seed 57 / candidates / evidence 78 / registry posture, compute reachable total from existing repo inputs, allocate the ~93 new canonical IDs into batches A/B/C (or prove unreachable → stop + owner ruling). Zero record authoring; metadata only. | standard | standard-feature — mid-tier builder, single review |
| BIO-LOCAL-008 | Seed expansion batch A (~31 records): schema-valid, draft/needsReview/inactive, unknown-honest, claims trace to existing packets only. | elevated (claim-content) | architecture/risk — frontier builder, dual review |
| BIO-LOCAL-009 | Seed expansion batch B (~31 records): same bar as 008. | elevated (claim-content) | architecture/risk — frontier builder, dual review |
| BIO-LOCAL-010 | Seed expansion batch C (remainder to exactly 150): same bar as 008. | elevated (claim-content) | architecture/risk — frontier builder, dual review |
| BIO-LOCAL-011 | Seed-run + serving proof: local SeedJob ingests 150, knowledge API serves 150, frozen count tests updated explicitly (57→150), fixture independence confirmed. | elevated | architecture/risk — frontier builder, dual review |

Order: 006 (contract shape) + 001 (boot) first (independent) → 002 (public read) → 003 (auth/isolation) → 004 (spine/receipts) → 005 (guidance, consumes 002–004 surfaces) → 007 (inventory; independent of 002–005, may run once 001 proves boot) → 008 → 009 → 010 (strictly serial, each rebases onto the prior's merge; the seed file is a serialization point) → 011 (after 010 merges). 003/004/005/008/009/010 touch serialization points — sequence, never parallel. If 007 proves unreachable, 008–011 stop and the owner rules before any new sourcing.

## Canon this goal builds against

- Foreman-line: `D:\Repos\agent-skills\plugins\foreman-line\docs\COORDINATOR-PATTERN.md`, `docs\SPEC-CONVENTION.md` (schema v0.2 + `Allowed Files`), `docs\FOREMAN-LINE-PLAN.md` (§5 routing, §6 audit triggers), `skills\parcel-driven-development\SKILL.md`.
- BioStack: `README.md` (dev/prod compose, test commands, hand-written migrations), `AGENTS.md`, `contracts/product-contract.v1.json` v1.0.0, `docs/guidance/biostack-guidance-content-contract.v1.md` + `docs/guidance/RATIFICATION.md`, `docs/canon/`, `.audit/POSITIONING-ARTIFACTS-v2.md`, `BioStack.Api/ProductionMigrationBaselineConfiguration.cs`.
- Seed corpus: `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json`, `Schemas/substance-record.schema.json`, `Pipeline/SubstanceRecordValidator.cs` (+ `SubstanceRecordLoader.cs`), `Jobs/SeedJob.cs`, `Config/WorkerOptions.cs` (`SeedFilePath`); frozen count tests `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs` + `StructuralEvaluationReportBuilderTests.cs` (test `Fixtures/substances-seed.json` is an independent synthetic sample — not the corpus).
- Prior assessments as CONSTRAINTS ONLY: `docs/INITIATIVES/biostack-production-readiness/` (HOLD), `BIOSTACK_FRONTEND_READINESS_AUDIT.md`.

## Stop conditions

Universal (COORDINATOR-PATTERN) + charter stops: frozen contract needs modification; tripwire fires twice on one parcel; security finding can't close in-parcel; anything outward-facing beyond standing auth (deploy, charge, email, publish, claim); queue empty. Goal-specific: parcel proposes prod-compose/Azure/Stripe-live/SMTP/Postgres-drill work (D1 violation — stop); parcel edits a frozen contract silently (D5 — stop + amendment path); reduced-shape leak found and not fixed in-parcel (D6 — stop, gate fails); `ParcelState`/verification disagreement between two consumers (stop, single-authority invariant broke); seed-expansion parcel authors a claim without an existing packet source line, browses/acquires a new source, flips a record active/published, or auto-merges an identity collision (D10 violation — stop); 007 proves 150 unreachable from existing inputs (stop 008–011, owner rules; KEO-73/KEO-74 gates apply to any new sourcing).

## Ratification record

- Gate 1: **GRANTED 2026-09-18 (owner: Clint Morgan)** — charter ratified with one addition (D10, 150 compounds seeded, ratified as part of Gate 1). Decisions D1–D10 ratified; OQ1–OQ3 carried OPEN (no ruling yet — parcels proceed on the recommended defaults only where their spec states the assumption explicitly, and stop on conflict). Dispatch authorized only under the D8 standing authorizations; implementation and external effects only per-parcel under their own Gate 2 + green chain.
- Post-ratification amendment (same day, owner-directed): D8 standing Gate 2 extended to BIO-LOCAL-007..011 in dependency order (007 → 008 → 009 → 010 → 011, strictly serial for 008–010); exit criterion gained item 5 (150 seeded + served); `RELEASE-GATES.md` gained blocking gate `corpus-seeded`; D9 serialization points gained the seed file + count-test expectations.
- Plan-level adversarial review: runs AFTER Gate 1, always (fresh session, charter + repo canon only). NOTE: the review brief must include this D10 amendment (it landed after the initial charter draft) — reviewer is asked explicitly whether the 007-first ordering and batch decomposition are coherent.
- Loop directive: written after Gate 1 + plan review; ownership block names the claiming coordinator; transfers only at parcel boundaries.
