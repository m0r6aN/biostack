# BIO-LOCAL-012 Gate 2 Dispatch Record (bounded remediation — M1)

Coordinator-owned record, frozen at dispatch (2026-10-07). This is the local-readiness charter
loop's BOUNDED REMEDIATION PARCEL for finding M1 (BIO-LOCAL-003 closure record). The remediation
contract is the review finding verbatim, recorded here in lieu of a separate spec file:

**Finding M1 (MEDIUM):** `.env.example` inline comments may leave `Smtp__Host` treated as
non-blank → local dev defaults to SMTP and Development mode can expose a stack trace.

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-012",
  "contract": "bounded-remediation:M1-from-BIO-LOCAL-003-closure",
  "builderId": "bio_local_012_builder",
  "branch": "fix/bio-local-012-env-smtp-hardening",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-012",
  "permissionEnvelope": "local-only:env-example-plus-program-cs-plus-one-test",
  "allowedBuilderSurfaces": [
    ".env.example",
    "backend/src/BioStack.Api/Program.cs",
    "backend/tests/BioStack.Api.Tests/ (one new or extended regression test file)"
  ],
  "reviewerIds": ["bio_local_012_review_1"],
  "risk": "standard",
  "acceptanceCriteria": [
    "AC1: .env.example documents the inline-comment ambiguity and warns that values must be bare/trimmed",
    "AC2: SMTP (and equivalent inline-comment-prone) configuration values are trimmed at load (e.g. .Trim()), so commented placeholders never produce a non-blank host",
    "AC3: regression test proves blank/comment-only values do NOT activate the SMTP path and do NOT cause stack-trace exposure in Development",
    "AC4: focused test suite green; git diff --check clean; no other behavior change (rollback = revert commit)"
  ]
}
```

Dispatch notes: standard envelope (exact SHA in evidence/PR body, product code changes LIMITED to
the surfaces above, stop-and-report on anything wider). Dual review waived (standard class,
bounded scope); if the builder finds the fix cannot be confined to these surfaces, it stops and
the parcel returns to the coordinator.
