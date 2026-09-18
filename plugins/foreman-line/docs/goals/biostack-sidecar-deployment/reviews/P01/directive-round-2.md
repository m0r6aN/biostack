# P01 Review Directive — Round 2

Candidate: `6409fc773331a253c7a46f5763f3f3d51deb4308`

## Convergence and disposition

| Topic | Lead | Adv A | Adv B | Security | Disposition |
| --- | --- | --- | --- | --- | --- |
| Warning-level production custody | yes | yes | — | yes | **FIX** |
| PEP 518 artifact-hash residual | yes | yes | yes | yes | **ACCEPT-RESIDUAL**, keep exact |
| Runtime installer/write surface | lead expected closed | reproduced system installer | saw app-tree closure | prior focus | **FIX** |
| Immutable local image custody | expected closed | — | bounded | validate-after-use reproduced | **FIX** |

## Required round-three fixes

1. **Owned census lifecycle:** replace every anonymous static `docker run --rm` with a named, cidfile/label-owned lifecycle whose timeout, malformed output, and cleanup paths are tested. A forced subprocess timeout must leave zero matching containers.
2. **Late-proof reconciliation:** attach a cryptographically random ownership label to every test container and perform bounded cidfile plus exact-name-and-label reconciliation after ambiguous/timeout creation. Remove only a full ID bearing the expected ownership label. Test delayed proof, collision, disagreement, interruption-equivalent timeout, and no-proof withholding.
3. **Complete installer removal:** remove runtime `pip`, `pip3`, `pip` modules, `ensurepip`, `uv`, and `uvx`. Census both system and virtualenv interpreters/PATH and prove every installer surface is absent while the application and nine dark checks still run.
4. **Validate before use / no pulls:** reject a non-`sha256:` inspected ID before the first container execution. Add `--pull=never` to every local-image `create`/`run` path. Tests must show malformed IDs cause zero execution and missing images cannot trigger registry access.
5. **Log-level custody:** include `log_level: "warning"` in the dark-environment observation and required contract. Record in P01's allowed parcel documentation that P02 must set and statically verify `BIOSTACK_RESEARCH_LOG_LEVEL=warning`, and P03 must verify the effective revision setting. Keep request/job markers in the leakage test.
6. **Malformed package evidence:** do not filter nameless/invalid distribution metadata. Make collection/evaluation fail closed and add collector-level regressions.

## Accepted residuals

- Exact `hatchling==1.32.0` in the digest-pinned builder does not provide PEP 518 artifact-hash closure. Retain the limitation and do not label it fully locked.
- The audit export is version/lock evidence rather than install-artifact authentication; the hash-locked audit tool bootstrap and immutable production image build remain separate controls.
- Hosted runner, Python, and Node patch identity can drift within fixed maintained labels; action code, container bases, archive, and bootstrap wheels remain immutable.
- The local synthetic token can be observed by a same-host process while Docker receives it; it is random, unprivileged outside the test container, and prohibited from output.
- OS-level forcible verifier termination cannot guarantee cleanup; ownership labels must make the artifact attributable and safely recoverable on the next bounded reconciliation.

Run a fresh Step 0, change only the smallest subset of the unchanged nine-file P01 allowlist, rerun the entire deterministic chain, commit locally, and submit the new exact commit to another fresh two-seat adversarial plus security review. No downstream parcel starts before acceptance.
