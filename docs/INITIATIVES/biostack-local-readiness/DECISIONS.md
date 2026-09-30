# Decisions — biostack-local-readiness

| ID | Date (UTC) | Decision / ruling needed | Status | Owner |
|---|---|---|---|---|
| D1–D9 | 2026-09-18 | Goal-charter locked decisions proposed (see `GOAL-CHARTER.md`) | RATIFIED at Gate 1 (owner, 2026-09-18) | Clint Morgan |
| D10 | 2026-09-18 | Targeted 150 compounds seeded (owner addition at ratification): seed 57 → 150, draft/needsReview/inactive, unknown-honest, claims trace to existing packets, inventory-first (007) with stop on unreachable | RATIFIED at Gate 1 (owner, 2026-09-18) | Clint Morgan |
| Gate 1 | 2026-09-18 | Charter ratification (D1–D10) | GRANTED | Clint Morgan |
| Plan-review | 2026-09-19 | Plan-level adversarial review triaged (`plan-review-findings.md`): PR-01/PR-02 accept-as-documented (007 stop path armed, no re-open); PR-04/PR-07/PR-08(part)/PR-09/PR-11 fixed by coordinator shaping; PR-03/PR-10/PR-13 deferred to parcel head; PR-05 referred to owner at handoff; PR-12 LS3 ruling (closes only on 002+005 green); PR-14 informational. No locked decision changed — no Gate 1 re-open. | TRIAGED (no re-open) | coordinator 2026-09-19 |
| Gate 2-006 | 2026-09-19 | Dispatch BIO-LOCAL-006 (standing auth, first batch): coordinator lint PASS post-shaping (contract shape v1.0.0 verified on disk; surfaces exist; AC5 fixed; OQ1/OQ3 stated). Branch `proof/bio-local-006-contract-mirrors`, worktree `D:\Repos\BioStack.BIO-LOCAL-006`, base `e5b75e0`. | DISPATCHED | coordinator 2026-09-19 |
| Gate 2-001 | 2026-09-19 | Dispatch BIO-LOCAL-001 (standing auth, first batch): coordinator lint PASS post-shaping (compose services/ports/healthchecks/volumes + health mapping verified on disk; config-note path authorized; OQ1/OQ2/OQ3 stated). Branch `proof/bio-local-001-local-dev-boot`, worktree `D:\Repos\BioStack.BIO-LOCAL-001`, base `e5b75e0`. | DISPATCHED | coordinator 2026-09-19 |
| OQ1 | 2026-09-18 | Full vs focused test suites (recommendation: full once + focused rework, test-count tripwire) | OPEN (no ruling; parcels use the recommendation only where their spec states it) | Clint Morgan |
| OQ2 | 2026-09-18 | Docker-compose boot required for 001 (recommended: yes, host runs supplementary only) | OPEN (no ruling; 001 spec states the assumption) | Clint Morgan |
| OQ3 | 2026-09-18 | Adopt minimal `docs/specs/INDEX.md` (recommended: yes, projection only) | OPEN (no ruling) | Clint Morgan |
| Q-COMP | — | Is `/compounds` staying gated while `/knowledge` is public intentional? (carried from frontend audit; parcel 002 records, does not decide) | open | product owner |
| Q-PANEL | — | Homepage proof panel stays hardcoded vs live-engine link (parcel 002 records, does not decide) | open | product owner |

Rule: no builder decides product scope. In-parcel decision needs stop the parcel and land here via coordinator before work resumes.
