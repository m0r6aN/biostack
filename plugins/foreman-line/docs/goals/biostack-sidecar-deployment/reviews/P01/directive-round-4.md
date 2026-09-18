# P01 Review Directive — Round 4

Candidate: `a0929aaf37b77b1ccd9a5ecb5ff9f959cec7f742`

## Disposition

- **FIX:** the checked-in POSIX child regression must stay alive until SIGTERM and exit exactly 1 on Linux. The exact `node:22` read-only-container reproduction must pass before commit.
- **FIX:** TERM/INT must tighten the effective deadline so all reconciliation plus cleanup fits strictly inside CI's 20-second TERM→KILL escalation.
- **FIX:** a failed lifecycle cleanup must retain its pending claim and trigger one bounded outer recovery attempt before direct CLI exit. Release the claim only after verified cleanup or verified absence. Preserve primary and both cleanup diagnostics if recovery also fails.
- **FIX:** when proof sources disagree, any exactly one full ID bearing the unpredictable expected label may be retained solely for exact-ID, label-revalidated cleanup. Preserve disagreement as failure and never start/operate it. Multiple label matches remain fail-closed.
- **CLOSED:** hash-locked/audited build closure, runtime installer absence, package canonical uniqueness, immutable/no-pull image use, and log-level handoff.

## Required round-five verification

1. Run a fresh Step 0 on the exact candidate and unchanged nine-file P01 allowlist.
2. Add Linux-native child tests for TERM timing before create, during create/reconcile/start/operate/cleanup and cleanup error; use referenced event-loop handles and assert exact nonzero semantics plus pending-claim state.
3. Run the exact read-only `node:22` container test command; it must report every test green, not merely host Windows green.
4. Repeat a bounded real-Docker TERM probe and prove zero label/name leftovers.
5. Rerun the full Python, Node, runtime/build audits, hash bootstrap, build, nine-check, installer/build-tool, missing-image/no-pull, gitleaks, YAML, allowlist, secret and cleanup chains.
6. Submit the new exact commit to fresh independent adversarial and security review. P02 remains blocked.

Retain only the previously stated hard-kill/host/daemon, maintained-host-label, same-host synthetic-token, checkout-only scan, and downstream production log/revision custody limitations.
