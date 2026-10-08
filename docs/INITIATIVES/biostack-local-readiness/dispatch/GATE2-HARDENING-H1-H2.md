# Hardening Parcels Gate 2 Records — H1 / H2 (pre-production enablement)

Coordinator-owned records, frozen at dispatch (2026-10-08). These execute the FINAL-HANDOFF's
hardening register items H1 and H2. Both are bounded remediation contracts (the finding text is
the spec). H3 (SG-L8 draft-hook remediation) is scoped separately once its surface is confirmed.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcels": [
    {
      "parcel": "H1-POSITIVE-CONTROL",
      "contract": "bounded-remediation:H1-from-BIO-LOCAL-005-review-2",
      "builderId": "h1_builder",
      "branch": "test/h1-operator-positive-control",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/H1",
      "permissionEnvelope": "local-only:integration-test-plus-ratification-doc-plus-log-line",
      "builderSurfaces": [
        "one new/extended integration test proving the Operator tier receives the FULL response shape (the positive control that proves the gates discriminate, not blanket-deny)",
        "docs/INITIATIVES/biostack-local-readiness/RATIFICATION.md (fix the recorded test-command drift noted by the reviewer)",
        "the HasReasoningAccessAsync exception handler: one debug-log line for operational visibility (no behavior change)"
      ],
      "reviewerIds": ["h1_review_1"],
      "risk": "standard",
      "acceptanceCriteria": [
        "AC1: automated integration test proves entitled (Operator) tier receives full shape AND unentitled does not — the discrimination proof, runnable in CI-style locally",
        "AC2: RATIFICATION.md test commands match reality (re-run them; record)",
        "AC3: exception-handler debug logging added with zero behavior change",
        "AC4: focused suites green; git diff --check clean; scope confined or STOP-AND-REPORT"
      ]
    },
    {
      "parcel": "H2-TRUNCATION-DEFAULT",
      "contract": "bounded-remediation:H2-from-BIO-LOCAL-013-review-2",
      "builderId": "h2_builder",
      "branch": "fix/h2-truncation-default-posture",
      "worktree": "/home/cmorgan76/Repos/biostack-wt/H2",
      "permissionEnvelope": "local-only:spine-module-plus-tests",
      "builderSurfaces": "the governance spine module + its tests ONLY (backend/src/BioStack.Domain/Governance, backend/src/BioStack.Infrastructure/Governance, their test files)",
      "reviewerIds": ["h2_review_1", "h2_review_2"],
      "risk": "elevated (trust-adjacent — dual review)",
      "acceptanceCriteria": [
        "AC1: truncation detection is ON by default (F1) — the AC1-worthiness of the shipped posture matches its plain-language claim; opt-out (if any) explicitly documented as reduced posture",
        "AC2: content-substitution gap closed or bounded (F2) — the reviewer's Probe 2 class now fails closed OR its residual is explicitly documented in code docs with the exact attack preconditions",
        "AC3: prior regression suite green including the R1 rollback probe; legitimate recovery paths (clean restart, restore-latest) still pass",
        "AC4: git diff --check clean; scope confined or STOP-AND-REPORT"
      ]
    }
  ]
}
```

Dispatch notes: exact tested SHA in PR bodies; H2's scope mirrors BIO-LOCAL-013's envelope. Both:
stop-and-report on scope pressure; worktree cleanup; branch kept; Gate 3 = owner. These parcels
lift the H1/H2 rows of the FINAL-HANDOFF hardening register on merge + review.
