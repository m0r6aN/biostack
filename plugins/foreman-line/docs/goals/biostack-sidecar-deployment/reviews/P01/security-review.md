# P01 Security Review — Candidate `56063a4`

Worktree was clean at completion. Review covered the exact candidate diff, authentication/admission boundaries, container behavior, workflow, and verifier. It did not use live providers, networked scientific calls, or deployment infrastructure.

## Findings

1. **MEDIUM / high confidence — failure-path diagnostic redaction gap.** `inspectDarkEnvironment` invokes the Docker JSON helper without its sensitive values, so child-process stderr can reach the final CLI message unredacted (`scripts/verify-research-sidecar-container.mjs:715-725,891`).
2. **LOW / high confidence — malformed successful create output can orphan a container.** Ownership is recorded only after container-ID parsing (`scripts/verify-research-sidecar-container.mjs:740-750,865-878`).
3. **MEDIUM / high confidence — mutable or unhashed CI inputs.** Action tags, the gitleaks archive, and PyPI tool artifacts are not cryptographically bound.
4. **LOW / high confidence — writable runtime and retained installer.** The service UID can write the application/venv and the image retains `uv`.
5. **LOW / high confidence — package census can pass ambiguous input.** Empty input passes, and normalization does not canonicalize the full Python distribution separator set (`scripts/verify-research-sidecar-container.mjs:292-304`).
6. **LOW / high confidence — build-tool reproducibility is incomplete.** `build-system.requires` names `hatchling>=1.25.0`, but the build requirement is not locked by the current project lock.

## Requested moves

Thread sensitive values through every Docker subprocess, guarantee cleanup after successful creation, require a nonempty expected runtime package census with canonical distribution normalization, remove runtime installer/write access, cryptographically bind CI inputs, and either lock build requirements or record the residual explicitly.
