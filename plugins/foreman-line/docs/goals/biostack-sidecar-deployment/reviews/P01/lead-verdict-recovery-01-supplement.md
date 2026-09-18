# P01 Lead Verdict — Recovery 01 Supplement

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

This supplement supersedes the pre-review lead disposition in `lead-verdict-recovery-01.md`.

Verdict: **REJECT — FOUR INDEPENDENTLY CONFIRMED RELEASE-BLOCKING GUARDRAIL DEFECTS**

## Coordinator reproductions

1. A crafted Docker inspect payload containing `Entrypoint=["/bin/sh","-c","evil"]` and `Env=["OPENAI_API_KEY=embedded-secret"]` was parsed; both fields disappeared and `evaluateImageConfiguration` returned `{ok:true, errors:[]}` after adding the expected runtime UID.
2. `evaluateDockerignoreContract` returned `{ok:true, errors:[]}` for the checked-in `.dockerignore`, although `p01-untracked-credential.json` matches no exclusion and therefore remains eligible for context transmission.
3. The literal final-absence Bash block was executed with an in-memory `docker(){ return 1; }` function. It exited 0 and printed `ABSENCE_GATE_PASSED`.
4. The literal ownership-claim/cleanup logic was executed against in-memory responses for a pre-existing exact-name container with a valid 64-hex ID and 48-hex owner label. It printed `EXISTING_CONTAINER_REMOVAL_REQUESTED=yes`.

No real Docker container, network call, provider, source edit, build, installation, or external mutation was involved in these reproductions. Temporary mock scripts were removed and the candidate worktree remained clean.

The current built image was separately inspected and is not itself malicious: its entrypoint is `null`, its baked environment contains only the expected pinned Python-base/runtime variables, and no P01 container remains. The blockers concern the CI/verifier's failure to enforce those properties and its unsafe behavior under error/collision conditions.

P02 remains blocked. The recovery authorization permits review but no remediation edit, so execution stops pending a new human authorization.
