# BIO-LOCAL-001 Gate 2 Dispatch Record

Coordinator-owned record, frozen at dispatch (2026-10-07).

```json
{
  "schema": "biostack.gate2.v1",
  "status": "approved",
  "parcel": "BIO-LOCAL-001",
  "specPath": "docs/specs/active/BIO-LOCAL-001-local-dev-boot-proof.md",
  "specSha256": "42a855a3064882d8ebdc766f3026dad987e56610a244d6c350c0b3a42aff77c9",
  "comparisonBase": "63bfd21eb9c5d68065ae7ee06c268fafcf1ad7d7",
  "builderId": "bio_local_001_builder",
  "branch": "proof/bio-local-001-local-dev-boot",
  "worktree": "/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-001",
  "permissionEnvelope": "local-only:dev-compose-boot:two-evidence-files",
  "evidenceDirectory": "docs/INITIATIVES/biostack-local-readiness/evidence/",
  "reviewerIds": ["bio_local_001_review_1"],
  "verificationContract": "spec-verification-section:compose-boot-curl-probes-downv-reset-git-diff-check",
  "allowedBuilderSurfaces": [
    "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-001-boot-proof.md",
    "docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-001-config-note.md"
  ],
  "frozenSurfaces": ["product-code", "compose-files", "Dockerfiles", ".env", "migrations", "contracts"]
}
```

## Coordinator amendment A1 (recorded, spec text unchanged)

The spec pins `origin/main@e5b75e0`; that SHA is 725+ commits stale and no machine state matches
it. Per the goal charter's named-successor clause ("or a named successor if a parcel forces a new
SHA, with re-verification"), `comparisonBase` is the named successor
`63bfd21eb9c5d68065ae7ee06c268fafcf1ad7d7` (current `main`, containing the closed
BIO-ANALYZER-001..004 chain). The full boot proof is re-verified at this SHA by this parcel's own
run. OQ assumptions in the spec (OQ2 = compose boot required; OQ1 = zero automated tests;
OQ3 = no INDEX work) stand as explicitly stated defaults per Gate 1; stop on conflict.

## Dispatch notes

- Env: Linux. The spec's `Invoke-WebRequest` alternative is moot; record the exact `curl`
  commands used. Machine constraint history (prior Docker OOM) must be recorded in the evidence.
- Stubbed-Keon posture wording is mandatory and must never be presented as governance:
  development convenience, not a governance claim.
- Stop-and-report rule (spec) applies verbatim: boot failure from a product defect ends the parcel
  with a remediation request; no fixes in this parcel.
- Deliverable flow: evidence commit on `proof/bio-local-001-local-dev-boot` → push → PR vs `main`
  (Gate 3 merge is the owner's) → worktree removed → branch kept for the PR.
- Reviewer `bio_local_001_review_1` (1 independent reviewer, `standard` class) replays the
  recorded commands from a clean volume and answers the spec's Verification Plan questions.
