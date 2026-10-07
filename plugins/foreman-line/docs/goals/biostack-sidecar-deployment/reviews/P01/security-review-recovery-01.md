# P01 Security Review — Recovery 01

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

## Verdict

Hold. Two Medium, high-confidence CI TERM-fixture findings remain. No Critical or High security issue was demonstrated by this seat.

## Findings

1. **SEC-01 — Medium; CWE-252; cleanup absence verification fails open on Docker errors.** `.github/workflows/research-sidecar-ci.yml:343-352` ignores both `docker container ls` exit statuses inside conditional substitutions. Empty stdout from failed queries is accepted as absence, after which ownership state and the cleanup trap are cleared. A literal Bash mock whose `docker` function returned 1 exited 0 and printed `ABSENCE_GATE_PASSED`. Capture each bounded query with an explicit success check and reject query failure before testing emptiness or releasing the trap.
2. **SEC-02 — Medium; CWE-441; fixture cleanup can adopt a pre-existing container.** Workflow lines 250 and 256-300 use a predictable name without preflight absence and learn the expected owner marker from the discovered container. A literal in-memory mock representing a pre-existing exact-name container with valid ID/label caused the cleanup path to request force removal. Use an unpredictable invocation name, fail-closed preflight absence, and an invocation-established nonce before authorizing cleanup.

## Required moves

1. Make absence verification fail closed on Docker errors and deadlines.
2. Bind fixture cleanup to fresh invocation ownership while retaining exact-ID and label revalidation.
3. Add executable Bash/mock regressions for both cases.

## Coverage gaps

Reviewed the full base diff and relevant auth/config/privacy/runner context. Ran 262/262 Node tests, `git diff --check`, and two bounded literal-shell mocks. No image build, Docker operation, dependency audit, Python suite, external provenance check, or provider/network call was performed.

P01 release blocker: **YES**

Worktree clean: **yes**
