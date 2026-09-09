# P01 Adversarial Review A — Candidate `56063a4`

Worktree was clean at completion.

## Findings

1. **HIGH — container cleanup is not guaranteed.** A successful `docker create` with malformed or truncated stdout leaves `ownedContainerId` unset, so `finally` skips removal (`scripts/verify-research-sidecar-container.mjs:739-750,865-879`).
2. **HIGH — request-content log safety is overclaimed.** Rejected requests use the literal `P01Compound`, which is absent from the sensitive-value census (`scripts/verify-research-sidecar-container.mjs:732-735,785-803,854-862`).
3. **HIGH — deterministic supply-chain execution is unverified.** The workflow uses mutable runner/action tags, an unhashed PyPI bootstrap, and a checksum-free gitleaks archive (`.github/workflows/research-sidecar-ci.yml:13-50,100-116`).
4. **MEDIUM — fail-closed evaluators accept unsafe states.** Local probes accepted `.dockerignore` negations such as `!docs`/`!artifacts` and an extra `run_shell` workflow (`scripts/verify-research-sidecar-container.mjs:207-231,356-375`).
5. **MEDIUM — terminal-injection sanitization is incomplete.** CSI erase and OSC/BEL sequences survive; tests cover only SGR color codes (`scripts/verify-research-sidecar-container.mjs:143-153`).

## Requested moves

1. Record container ownership before parsing create output, clean by exact requested name if parsing fails, preserve primary and cleanup errors, and add injected orchestration tests for malformed output, timeout, and dual failure.
2. Replace literal request identifiers with random markers included in the sensitive list and prove simulated log leakage fails.
3. Pin actions to immutable commits, hash-lock dependency bootstrap artifacts, and verify the recorded gitleaks SHA-256 before extraction.
4. Reject exclusion-negating dockerignore rules and require exact allowed-workflow set equality.
5. Strip complete ANSI CSI/OSC sequences and remaining C0/C1 controls, with regression tests.
