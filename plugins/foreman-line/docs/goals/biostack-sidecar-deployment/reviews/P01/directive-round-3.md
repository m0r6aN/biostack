# P01 Review Directive — Round 3

Candidate: `5e423c9f451f8ddd2bf276ad6ff6aa09f96b3045`

## Convergence and disposition

| Topic | Lead | Adv A | Adv B | Security | Disposition |
| --- | --- | --- | --- | --- | --- |
| Process termination cleanup | residual only for uncatchable death | real SIGTERM blocker | bounded-window residual | reproduced callback bypass | **FIX catchable TERM/INT; retain SIGKILL residual** |
| Reconciliation global bound | expected bounded | disproved | notes fixed window | — | **FIX** |
| PEP 518 artifact closure | residual | challenges acceptance | confirms residual | confirms residual | **FIX if mechanically possible; otherwise stop for human Gate-1 residual decision** |
| Logging handoff | fixed | fixed | fixed | fixed | **CLOSED** |
| Runtime installer surface | fixed | fixed | fixed | fixed | **CLOSED** |

## Required round-four fixes

1. **Catchable process termination:** install bounded SIGTERM/SIGINT handling for the direct CLI. Register each pending ownership claim before Docker create, stop starting new work after termination, reconcile the active claim, revalidate its random label, clean its full ID, then exit nonzero. Do not claim coverage for SIGKILL, host loss, or runtime destruction.
2. **Real process-level regression:** reproduce the CI outer-timeout shape with a child verifier process and a real or faithful injectable Docker lifecycle. TERM during a `Created` census must exit nonzero and leave zero matching label/name containers. Preserve a bounded manual real-Docker probe in evidence.
3. **One global deadline:** propagate a monotonic/wall-clock deadline through create, cidfile/name/label reconciliation, attach/start, inspect, and cleanup. Every Docker subprocess timeout must be capped by remaining budget. Cleanup receives a small reserved budget and completes before the CI outer timeout's termination escalation.
4. **Conflicting evidence with safe cleanup:** if exactly one full ID has the expected random label and agrees with exact name/cidfile, retain that ID for cleanup even when stdout presents another syntactically valid ID. Preserve proof disagreement as the primary verification failure; never start/operate the container; clean only the uniquely label-proven ID.
5. **Canonical package uniqueness:** reject duplicate PEP 503-normalized distribution identities, including case/separator variants.
6. **Build-backend closure:** attempt a hash-locked, audited Hatchling 1.32.0 build closure inside the digest-pinned builder, build without isolated network resolution, and remove build tooling from the runtime environment. Bind all artifacts/versions in tests. If this cannot be done within the existing P01 Allowed Files without weakening runtime minimality, stop and request an explicit human residual decision rather than self-labeling it accepted.

## Retained residuals

- SIGKILL, host loss, or Docker daemon loss can interrupt cleanup; labels/cidfiles make artifacts attributable but do not make cleanup atomic.
- Hosted runner and language maintained labels can drift; immutable action/container/archive/bootstrap identities remain separately bound.
- The random local token remains same-host argv-visible and has no external authority.
- P01 secret scanning remains checkout-only; P03 owns history and ruleset coverage.

Run a fresh Step 0 on the exact candidate, keep the unchanged nine-file allowlist, rerun the full deterministic chain, commit locally, and submit the exact result to a fresh review cycle. Do not start P02.
