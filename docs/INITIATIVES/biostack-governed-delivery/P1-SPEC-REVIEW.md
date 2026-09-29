# P1 Spec Review and Triage

Status: **CLOSED — PASS**

- Spec: `docs/INITIATIVES/biostack-governed-delivery/parcels/P1.md`
- Approved SHA-256: `A55235E81FF251B7A67620917522B9173F697D9776336C98D03B441F366C22BA`
- Commit containing approved spec: `a511a076a20c1366450b219d7e4477697b87b2ab`
- Required reviewer count: 2
- Charter decision changed: no

## Triage history

| Finding | Reproduced | Disposition |
|---|---:|---|
| Root `AGENTS.md` is ignored and absent from isolated worktrees. | yes | `fix` — pinned normalized baseline/final hashes, exact append block, and force-add contract. |
| P1 pre-registry approval and post-merge closure were circular/ambiguous. | yes | `fix` — defined the one-time canonical external-spec exception and coordinator closure-only update. |
| Verification descriptions were not executable enough. | yes | `fix` — specified one deterministic verifier contract, exact paths, fail states, and evidence files. |
| Required blank line made the original final `AGENTS.md` hash wrong. | yes | `fix` — corrected to `AF175D88CD872D8ADD203294614BED3B164C16D2F14197BB03D239321BEC1276`. |
| Generated evidence was outside the authorized surface. | yes | `fix` — authorized only untracked `artifacts/p1-verification/` while retaining an exact tracked diff. |
| Dirty tracked files could produce evidence for a different state than committed `HEAD`. | yes | `fix` — require zero staged/unstaged tracked entries and only authorized untracked evidence. |
| A committed Gate 2 record could not name its own containing commit. | yes | `fix` — adopted comparison-base plus sole-child dispatch-anchor topology. |
| Gate 2 blob preservation did not validate its authorization fields. | yes | `fix` — added an exact JSON contract and field equality checks. |

## Final independent reviews

- Reviewer `p1_spec_review_13`: **PASS**, no blocker.
- Reviewer `p1_spec_review_14`: **PASS**, no blocker.

Both verified the exact approved spec hash independently and read-only. P1 may proceed to Gate 2 under the charter's standing authorization.
