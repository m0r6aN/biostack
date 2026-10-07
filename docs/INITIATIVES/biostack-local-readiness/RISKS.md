# Risks — biostack-local-readiness

| ID | Risk | Mitigation | Owner | Status |
|---|---|---|---|---|
| R1 | ~100 stale worktrees/branches cause collision/confusion at dispatch | D9: named worktrees, rebase-before-PR, serialization sequencing; coordinator lint of branch/worktree per dispatch | coordinator | open |
| R2 | Frontend full-suite OOM/timeout recurs locally (history) | OQ1: full suite once per lane + focused rework + tripwire; record machine constraints in 001 | 001/002 builders | open |
| R3 | Prod-compose `KeonRuntime__*` gap mistaken for deployment endorsement | Recorded as known limitation in 001 + FINAL-HANDOFF deferred list; D1 stop on any prod-compose work | 001 builder | accepted (local scope) |
| R4 | Pairwise-lane drafts (`docs/specs/`, untracked) bleed into parcel context | Per-spec Context & References whitelist; "load the specs folder is not a thing" (SPEC-CONVENTION §6) | coordinator | open |
| R5 | Seed-expansion claim risk: ~93 new records carry evidence claims; thin packet coverage tempts padding | D10 trace rule + unknown-honest fields + draft/inactive + dual review (008–010) + SG-L8; thin compounds ship explicit evidence gaps | 008–010 builders + reviewers | open |
| R6 | 150 unreachable from existing inputs without new sourcing | 007-first ordering with stop + owner ruling; KEO-73/KEO-74 gates apply to any new sourcing | coordinator + owner | open |
| R7 | Frozen count-test updates (57→150) mask a real corpus regression | 011 updates to OBSERVED values cross-checked against 007 projection; reviewer replays builder + test runs | 011 builder + reviewers | controlled |
| R5 | Optimistic sweep (marking unverified as passing to reach LOCAL-GO) | D7: `unverified` blocks by default; coordinator closure checks against disk before any gate flips | coordinator | controlled |
| R6 | Reduced-shape or Class D regression discovered mid-proof | SG-L2 fails closed; remediation parcel (bounded) + gate stays failing until re-proven | 002/005 builders + reviewers | open |
| R7 | Contract drift discovered (`--check` red or entitlement mismatch) | Loop-stop + amendment path; never silent edit (D5) | 006 builder | open |

Historical credential/proxy/prod-data risks are NOT carried here — they belong to the HOLD production initiative and are out of scope by design.
