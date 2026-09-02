# Gate 1 Ratification Receipt — BioStack Algorithm Remediation

**Status:** RATIFIED 2026-09-02

**Requested:** 2026-09-02

**Authoritative charter:** `plugins/foreman-line/docs/goals/biostack-algorithm-remediation/charter.md`

**Ratified decision-set commit:** `0c4c0204d7ca512ad3de903e3caf257dc5b28b40`

**Ratified decision-set SHA-256:** `991151BF01718B3A8D283666B5A110F7D60CF360E290D84A440553F4B8D318B8`

**Verified base:** `main` / `origin/main` at `339f259b1a467034db4f57cf9d774c292f11b53a`

**Governing diagnostic coordination head:** `codex/test-repro-contracts` at `879def179654d0fb44ab57a8e29b02b20e2239d0`

## Decision requested

Ratify or amend charter decisions D1-D14 as a single Gate 1 decision set:

- D1: separate remediation lineage from verified `main`; do not merge diagnostic history.
- D2: preserve the original diagnostic and coordination branches untouched and unmerged.
- D3: use the seven implementation parcels P01-P07.
- D4: keep Q01-Q04 diagnostic-only; a newly reproduced defect requires a narrow charter amendment and reopened Gate 1.
- D5: adopt the charter's parser semantics.
- D6: adopt the charter's interaction-safety invariants.
- D7: adopt the charter's evidence eligibility and external-locator requirements.
- D8: enforce monotonic sidecar terminal state and `maximum_source_count` before materialization.
- D9: enforce current-user, server-side authentication and consent before OCR or frontend-provider outbound calls.
- D10: require offline synthetic fixtures and local intercepting fakes for verification.
- D11: require fresh independent adversarial review for every parcel and additional independent/security review for the named high-risk parcels.
- D12: grant contingent Gate 2 dispatch authority only for Q01-Q04 and P01-P07 after exact charter/spec alignment, isolated worktrees, Step 0, and Allowed Files checks.
- D13: reserve Gate 3 to the human for an exact commit/merge set and release action after the complete verification chain is green.
- D14: preserve evidence, residual claims, lessons, and diagnostic-branch custody through Stage-F closure.

## Authority boundary while pending

Until explicit human ratification is recorded:

- Gate 1 is not granted.
- Gate 2 is not active, so no investigation or implementation parcel may be dispatched.
- No production file or regression test may be changed under this goal.
- Plan-level adversarial review, shaping, and implementation remain pending.
- Gate 3 remains ungranted; no merge, push, pull request, deployment, provider enablement, release, or publication is authorized.

After ratification, the next action is a fresh independent plan-level adversarial review of the ratified charter. Findings will be dispositioned explicitly. Any finding that changes D1-D14 will trigger a narrowly reopened Gate 1 before shaping or dispatch.

## Exact ratification form

> Gate 1: I ratify D1-D14 as written and grant contingent Gate 2 dispatch authorization for Q01-Q04 and P01-P07. Gate 3 remains ungranted.

The developer supplied this exact statement on 2026-09-02. Gate 1 is granted, contingent Gate 2 is active only under D12, and Gate 3 remains ungranted.
