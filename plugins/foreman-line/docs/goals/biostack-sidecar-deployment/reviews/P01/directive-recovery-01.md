# Directive — P01 Recovery 01 Review

Candidate reviewed: `59102d3c8170968d2c9d316ec5b07a934042cee5`

Council: coordinator lead plus two independent adversarial seats and one independent security seat. External model-provider CLIs were not used because the ratified recovery forbids provider calls.

## Verdict

Reject P01. One adversarial seat passed, but the other adversarial seat and the security seat produced four unique findings that the coordinator independently reproduced. All four are therefore verified facts, not majority-vote opinions. The current image is clean; its enforcement harness is not yet fail-closed.

## Confirmed findings

1. **High — image configuration gap:** `Config.Entrypoint` and `Config.Env` are neither retained nor enforced, permitting a crafted unsafe image observation to pass.
2. **Medium — build-context confidentiality gap:** the credential-shaped fixture is omitted from the final image but is not excluded from the transmitted Docker context.
3. **Medium — fail-open absence gate:** Docker query errors can be treated as proof that no owned container remains.
4. **Medium — cleanup confused-deputy gap:** the TERM fixture can adopt and request removal of a pre-existing exact-name, valid-label container.

## Shaped P01 round-seven remediation

Exact Allowed Files:

1. `.github/workflows/research-sidecar-ci.yml`
2. `scripts/verify-research-sidecar-container.mjs`
3. `scripts/verify-research-sidecar-container.test.mjs`
4. `backend/research-sidecar/.dockerignore`
5. `backend/research-sidecar/docs/PARCELS.md`

Required changes:

1. Preserve `Config.Entrypoint` and `Config.Env` from image inspection. Require a null/empty entrypoint. Enforce exact canonical baked-environment equality for the reviewed digest-pinned runtime image, including rejection of duplicate, malformed, unexpected, provider/credential-shaped, or missing entries. Make the Dockerfile contract reject any `ENTRYPOINT`. Add mutations for malicious entrypoint, embedded provider/secret variables, duplicates, missing values, and arbitrary extras.
2. Replace `.dockerignore` with a default-deny allowlist for exactly `pyproject.toml`, `uv.lock`, `README.md`, `src/`, and `src/**` as required by the build. Update the validator to enforce that exact effective allowlist and reject broader re-inclusion. In CI, prove a temporary Dockerfile attempting to `COPY p01-untracked-credential.json` fails because the fixture is absent from context; retain the final-image absence check only as defense in depth.
3. Give the TERM fixture an invocation-generated unpredictable name and perform an explicit, bounded, fail-closed preflight proving both the exact name and invocation nonce are absent before the verifier starts. Cleanup may learn the verifier's owner label only after that preflight and only while the exact spawned PID remains attributable. Retain exact full-ID targeting plus owner-label revalidation.
4. Capture both final name and label queries separately with explicit bounded success checks. Any Docker error, timeout, malformed output, or uncertainty fails the step and retains trap recovery; only two successful empty results may release ownership state and the trap.
5. Add executable literal-Bash/mock regressions proving Docker query failure rejects, pre-existing valid-label collision rejects without removal, random invocation identity is used, and successful cleanup still releases only its own exact ID. Static source-pattern assertions alone are insufficient.
6. Update exact test/evidence counts without overstating hosted, Azure, registry, production, or provider coverage.

Verification after fresh Step 0 must include the complete host and exact-Linux Node suite, Python 53+1, syntax/YAML, frozen sync, both strict audits, hash bootstrap, gitleaks, default-deny context negative build, exact image build/identity, package/environment/entrypoint/filesystem census, normal 9/9 verifier, missing-image/no-pull, one real-Docker TERM gate, scope/secret scan, and zero name/label leftovers. No provider/scientific/model/Azure/production/protected-data/secret/remote/deployment action is permitted. Any repeated failure or scope/network drift stops.

Fresh two-seat adversarial and security review is required on the exact corrected commit. P02 remains blocked until P01 is independently accepted. Gate 3A and Gate 3B remain ungranted.
