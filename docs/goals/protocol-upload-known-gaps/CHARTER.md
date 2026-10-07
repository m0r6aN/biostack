# Goal Charter — Protocol Upload Known-Gap Remediation (P01-R follow-on)

**Status:** RATIFIED 2026-10-04 (developer, in session)
**Coordinator:** this `/goal` session (Claude)
**Goal slug:** `protocol-upload-known-gaps`
**Parent goal:** `docs/goals/protocol-upload-graceful-failure/` (BIO-ANALYZER-001; HANDOFF 2026-10-03)
**Coordinator branch/worktree:** `goal/protocol-upload-known-gaps` @ `D:/Repos/BioStack/.worktrees/protocol-upload-known-gaps-20261004`, branched from `fix/protocol-upload-graceful-failure` @ `a12d580d`
**Date:** 2026-10-04

## Objective

Close the "known gaps, not fixed" recorded in the parent HANDOFF (Residuals A, B, C and table-row dose recovery) as three reviewed parcels, each locked by regression tests, on top of BIO-ANALYZER-001. Deliverable: committed fixes, deterministic evidence, and a durable HANDOFF on local branches. Nothing is pushed, merged, deployed, or claimed go-live.

## Parcels (dependency order; merge order 002 → 003 → 004)

| Parcel | Spec | Risk | Routing | Reviews |
|---|---|---|---|---|
| BIO-ANALYZER-002 | `docs/specs/active/BIO-ANALYZER-002-xlsx-package-robustness.md` (rev 3) | low | implementation/standard | 1 independent code review |
| BIO-ANALYZER-003 | `docs/specs/active/BIO-ANALYZER-003-single-word-frequency-prose-gate.md` (rev 3) | standard | implementation/standard | 1 independent code review |
| BIO-ANALYZER-004 | `docs/specs/active/BIO-ANALYZER-004-spreadsheet-row-reconstruction.md` (rev 6) | elevated | standard-feature | **2** independent code reviews (mandatory, spec Ratified Decision 5) |

002 and 003 touch disjoint files and are built in parallel worktrees; both are integrated into the coordinator branch before 004 is dispatched.

## Locked decisions (developer-ratified)

- **K1 — Scope.** Supersedes parent charter D1's exclusion of row-aware table reconstruction, for spreadsheet (CSV/XLSX) uploads only. DOCX/PDF table dose binding, multi-token prose (Residual A2), `Check-in daily` (A3), and all items under each spec's Out of Scope remain open known gaps.
- **K2 — Design decisions.** All six shaping decisions and the spec-level rulings recorded in each spec's "Ratified Decisions" and "Coordinator rulings" sections (ratified 2026-10-04: "Ratify all 6, as written").
- **K3 — Stacking.** The parent branch `fix/protocol-upload-graceful-failure` (BIO-ANALYZER-001) is not yet merged. The developer chose to stack on it rather than wait; trial merges of that branch into `origin/main` and `dev` are conflict-free. `main` has unrelated red CI (frontend `Sidebar.collapse` and `CompoundIntelligenceCard` tests, gitleaks), treated as a separate problem by the developer's choice (option A).
- **K4 — Boundaries.** Same as parent D2: local commits only on named branches; no push, PR, merge to `main`/`dev`, deploy, or cloud/settings changes. Local integration of parcel branches into the coordinator branch is permitted.
- **K5 — Verification.** Builders are fresh `task` sessions. Code reviewers are fresh read-only `reviewer` sessions. The coordinator reproduces disputed findings before ruling and never accepts a builder's self-review.

## Authorizations

- **Gate 1:** ratified by the developer 2026-10-04.
- **Gate 2:** authorized by the developer 2026-10-04 ("Yes, authorized") for BIO-ANALYZER-002, -003, -004. Contingency: any failed, stale, or wrong-environment verification voids it for the affected parcel.
- **Gate 3 (merge/push/PR):** withheld. Human action.

## Exit criterion

Each spec's Acceptance Criteria met and evidenced; full `BioStack.Application.Tests` and the named Api tests green on the coordinator branch; independent code reviews PASS with all findings triaged; a durable HANDOFF on the coordinator branch listing exact evidence, remaining known gaps, and human gates; nothing pushed or merged.

## Stop conditions

- A frozen contract (`AnalyzeProtocolResponse`, feature gate, `ProtocolIngestionException`) needs to change.
- A builder stop-and-report that cannot be closed by a coordinator-ratified spec amendment committed alone.
- Deterministic tripwire fires twice on one parcel; reviewer FAIL the coordinator cannot reproduce and resolve.
- Any need to push, PR, merge, deploy, or touch cloud/settings.
- The developer says stop.
