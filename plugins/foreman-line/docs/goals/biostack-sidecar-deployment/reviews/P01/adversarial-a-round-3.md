# P01 Adversarial Review A — Round 3 Candidate `5e423c9`

Worktree was clean at completion. The reviewer ran 197 Node tests and the full verifier, then performed an ownership-scoped real SIGTERM probe.

## Findings

1. **HIGH / release-blocking — external termination bypasses cleanup.** SIGTERM after 1.2 seconds left an actual verifier-created, random-label-owned census container in `Created` state. The reviewer verified exact ID/name/label before removing only that container (`workflow:200`; verifier `:967-1008,1224-1405`).
2. **HIGH — valid-but-conflicting stdout can strand a provably owned container.** If stdout names ID A while cidfile/exact-name/random-label uniquely prove ID B, reconciliation reports disagreement and withholds cleanup rather than cleaning B while preserving the verification failure (`verifier:753-795`).
3. **HIGH — reconciliation lacks one global wall-clock deadline.** Each lookup/inspect can consume the default process timeout, so the nominal 20×100 ms loop can reach the workflow's outer timeout before cleanup (`verifier:753-795,881,955-965`).
4. **MEDIUM — Hatchling build closure is unaudited/unhashed.** Exact versioning is disclosed but absent from the runtime lock and audit export.
5. **MEDIUM — duplicate canonical package identities pass.** Required packages plus both `fastapi` and `FastAPI` are accepted (`verifier:430-450`).

Release-blocking P01 defect remains: **yes**, finding 1.
