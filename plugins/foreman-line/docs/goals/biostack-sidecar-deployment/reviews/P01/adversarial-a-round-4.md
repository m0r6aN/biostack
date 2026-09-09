# P01 Adversarial Review A — Round 4 Candidate `a0929aa`

Worktree was clean at completion. The exact image passed all nine checks and real handler injections at create/start/operate/destroy left zero artifacts, but the checked-in Linux test failed.

## Findings

1. **HIGH / release-blocking — Linux CI signal regression exits 13.** In `node:22` Linux, the test child exits because its unresolved promise has no referenced event-loop handle; expected 1, actual 13 (`workflow:189-190`; test `:1066-1146`). Windows passes because stdin emulation keeps the child alive.
2. **MEDIUM — termination does not tighten the remaining deadline.** A TERM received early can leave reconciliation/cleanup budgets larger than CI's 20-second TERM→KILL escalation (`verifier:92-179,863-923`; workflow `:230`).
3. **MEDIUM — cleanup failure releases the pending claim.** A transient removal failure emits an error but clears the only structured recovery state (`verifier:1001-1028`).
4. **LOW — conflicting-proof cleanup is narrower than unique label proof.** Cleanup is exposed only when cidfile and exact-name lookup both agree; name+label or cidfile+label unique proof is still withheld (`verifier:905-918`).
5. **INFORMATIONAL — build closure and runtime absence are fixed.** Dockerfile/workflow manifests are byte-equal, hash-bound and audited; direct build is no-index/no-deps; runtime is clean.

The reviewer also ran a corrected Linux keepalive harness: real SIGTERM returned code 1 with zero leftovers and zero pending claims. The implementation path works; the checked-in regression does not.

Release-blocking P01 defect remains: **yes**, finding 1.
