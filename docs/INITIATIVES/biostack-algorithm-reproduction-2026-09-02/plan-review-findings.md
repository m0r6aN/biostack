# Plan-level adversarial review

Date: 2026-09-02
Reviewer: fresh context-free adversarial session; read-only
Scope: initiative control pack, parcel branch identity, Allowed Files, evidence and handoffs

| Finding | Triage | Disposition |
|---|---|---|
| Blocking: `PARCELS.md` declared a Collective test target whose project/file does not exist; the handoff and evidence called the result inconclusive. | Fix | `PARCELS.md` now preserves the exact lane but marks it reserved/inconclusive and forbids a substitute path. |
| Blocking: no explicit human acceptance/review gate was recorded. | Fix, remains a gate | `CHARTER.md` and `loop-directive.md` record Gate 1 as pending explicit developer ratification; Gate 3 remains ungranted. |
| Should-fix: Interaction allowed a second file that was absent from the handoff. | Fix | `PARCELS.md` marks the exact file as a reserved lane with no separate claim surviving shaping. |
| Should-fix: Evidence/provenance allowed an endpoint-coverage test that was not executed. | Fix | `PARCELS.md` marks the exact lane reserved and keeps endpoint-wide coverage explicitly inconclusive. |
| Should-fix: handoffs did not include exact commands and assertion-level outcomes required by `CONTRACTS.md`. | Fix | `SESSION-HANDOFFS.md` now records exact commands/results for all rerun lanes and distinguishes the frontend environment failure from historical evidence. |

## Overall verdict

The five-way decomposition is operationally disjoint and the parcel branch deltas are test-only, but the initiative is not fully closed: Gate 1 awaits explicit developer ratification, Gate 3 is not granted, and the frontend reproduction requires a dependency-complete environment for independent recollection. Inconclusive claims remain open residuals.
