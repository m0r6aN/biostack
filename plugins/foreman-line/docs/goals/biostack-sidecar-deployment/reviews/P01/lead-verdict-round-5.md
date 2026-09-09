# Lead Independent Verdict — P01 Round 5

Candidate: `fac342b2e5d3e55a524b34cca2e45af2e1b1d9c7`

Written after the coordinator's independent source review and deterministic reruns, before any round-five external seat output was read.

## Verdict

**PROVISIONAL ACCEPT — no open P01 implementation blocker found; one final fresh two-seat adversarial plus security review is required.**

1. The Linux child harness now keeps a referenced handle until signal delivery. Seven native POSIX phases exit exactly 1 with no signal passthrough, leftover, or pending claim.
2. TERM/INT clamps the global deadline to 15 seconds, strictly inside CI's 20-second TERM-to-KILL escalation. Work, reconciliation, lifecycle cleanup, outer reconciliation, and outer cleanup reserve distinct portions of that deadline.
3. Lifecycle cleanup failure retains the pending claim and known full ID. Direct CLI performs one bounded outer recovery; claims release only after exact-label cleanup or bounded verified absence. Failed outer recovery remains reported and retained.
4. Ownership-source disagreement remains fatal/no-start/no-operate, while any exactly one expected-label full ID may be used solely for revalidated exact-ID cleanup. Multiple label matches remain withheld.

## Independent coordinator evidence

- Worktree clean; full parcel diff exactly eight allowed files and zero outside; diff check clean.
- Host Node suite: 223/223 passed.
- Linux Node suite: 223/223 passed in local `node@sha256:c601a46abb4d2ab80a9dc3da208d50d1122642d53f17a101926ace71e5a9bf1c`, with `--pull=never`, network disabled, read-only container/repo, all capabilities dropped, and no-new-privileges.
- Python suite: 53 passed, 1 expected skip.
- Container contract: 9/9 passed and emitted `sha256:82a077111407934c325c6dfa89b4b0b629ada2b1cdd937d3700722be7cac590f`.
- Post-run ownership-label census: zero.
- Builder evidence additionally records a real Docker TERM during `Created`: exact exit 1 in 1.674 seconds, no start/operate, zero pending claims, zero label/name leftovers; both audits, hash bootstrap, gitleaks, missing-image no-pull, runtime tool/ownership census and YAML/scope checks green.

## Retained residuals

1. SIGKILL, host/runtime destruction, or Docker-daemon loss can make cleanup non-atomic; labels/cidfiles/full IDs provide attribution and bounded recovery, not an absolute guarantee.
2. Fixed hosted runner/language maintained labels can drift within patch lines.
3. The random local service token is visible to same-host process inspection while passed to Docker, has no external authority, and is excluded from output.
4. P01 secret scanning is checkout-only; P03 owns history-wide scan/ruleset custody.
5. P01 log evidence is local warning-level evidence; P02/P03 own exact production configuration and effective-revision verification.

## Fresh-review question

Reviewers should rerun the exact Linux suite and real TERM path; attack deadline arithmetic, signal during cleanup/outer recovery, verified-absence semantics after cidfile removal, retained-claim error paths, multi-label disagreement, diagnostic redaction, and regression count/custody.
