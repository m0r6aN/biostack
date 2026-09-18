# P01 Review Directive — Round 5

Candidate: `fac342b2e5d3e55a524b34cca2e45af2e1b1d9c7`

## Required round-six fixes

1. **Exact production distribution allowlist:** replace the short provider denylist as the acceptance mechanism with exact equality against the canonicalized 24-distribution runtime set observed from the locked no-extra image. Reject every extra, missing, duplicate, nameless, or invalid identity. Retain explicit provider/build/install denylist messages as defense in depth. Tests must prove `anthropic`, `boto3`, `azure-ai-*`, arbitrary extras, case/separator aliases, and stale duplicates fail.
2. **Delayed-create claim custody:** after create timeout/error plus an initial no-proof reconciliation, retain the pending claim and perform the one bounded outer recovery. Test a label-proven container appearing only during outer recovery. Release only after verified cleanup or verified bounded absence; retain the hard-late-create residual beyond all bounded windows.
3. **Allowlisted Docker build context:** replace `COPY . .` with the smallest explicit source/metadata copies needed to build (`src` and already named metadata only). Static and image tests must prove untracked arbitrary/credential-shaped files cannot enter through the Dockerfile copy graph. Keep `.dockerignore` as defense in depth.
4. **Exact CI runtime patches:** set exact reviewed Python and Node patch versions supported by the pinned setup actions. Record them in evidence and bind with static tests.
5. **Real Docker TERM CI gate:** after image build, run a bounded Linux shell fixture that starts the real verifier, observes its P01-owned container, sends TERM to that exact PID, requires exit 1 inside the 20-second grace, and proves zero P01 ownership labels/names. Include trap-based recovery limited to the fixture's own proven artifacts. Then run the normal 9/9 verifier.
6. **Reproducibility receipt:** persist the exact local Linux Node image digest and hardened read-only/network-none/cap-drop/no-new-privileges reproduction command in P01's allowed documentation.

## Closed controls

Signal phase handling, 15-second clamp, lifecycle plus one outer recovery, proof-disagreement cleanup, canonical uniqueness, build-backend hash closure/audit, runtime tool absence, immutable/no-pull image use, warning logging and downstream custody, diagnostics, action/archive/bootstrap pins remain closed unless changed.

Run a fresh Step 0, preserve the unchanged nine-file P01 allowlist, rerun host and exact-Linux Node, Python, both audits, bootstrap, build, 9/9, real TERM in CI-equivalent shell, installer/build-tool/package exact-set, context-exclusion, missing-image/no-pull, gitleaks, YAML, scope/secret and zero-leftover chains. Commit locally and submit to fresh two-seat adversarial plus security review. P02 remains blocked.
