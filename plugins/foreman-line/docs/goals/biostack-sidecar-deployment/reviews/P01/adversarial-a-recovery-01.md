# P01 Adversarial Review A — Recovery 01

Candidate: `59102d3c8170968d2c9d316ec5b07a934042cee5`

## Verdict

Reject. Two release-blocking fail-closed gaps remain although the current image itself appears safe.

## Findings

1. **High — image entrypoint and baked environment are not verified.** `scripts/verify-research-sidecar-container.mjs:500-520,549-585,1328-1369` discards `Config.Entrypoint` and `Config.Env`; census containers override the entrypoint. A crafted observation with a malicious entrypoint and `OPENAI_API_KEY=embedded-secret` passed the exported evaluator. An image could run startup side effects before the compliant command and still pass. Parse and require a null/empty entrypoint, validate the baked environment fail-closed, reject `ENTRYPOINT` statically, and add mutations.
2. **High — the credential fixture is transmitted in the Docker build context.** `backend/research-sidecar/.dockerignore:1-19`, workflow lines 222-243, and tests around 380-413/1563-1564 do not exclude `p01-untracked-credential.json`. The workflow proves only that allowlisted `COPY` operations omit it from `/app`, not that it is absent from the transmitted context. Make `.dockerignore` default-deny for the exact build inputs and prove a fixture `COPY` fails because the source is absent from context.

## Required moves

1. Enforce exact entrypoint/environment invariants with negative mutations.
2. Default-deny the build context and replace the image-presence probe with a true context-absence test.
3. Rerun the complete P01 chain.

## Coverage gaps

No image build or crafted-image integration mutation was run under the review prohibition. The unsafe configuration acceptance was reproduced through the exported evaluator. Documented uncatchable/hard-late residuals were not treated as new findings.

P01 release blocker: **YES**

Worktree clean: **yes**
