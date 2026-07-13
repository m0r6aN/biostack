# Parcel Index

| Parcel | Track | Wave | Status | Worktree | Branch | Collision risk |
|---|---|---|---|---|---|---|
| PR-DOC-001 | Evidence/release | Foundation | in-progress | `D:/Repos/BioStack-release-governance` | `codex/production-readiness-phase2` | low; docs only |
| PR-CI-001 | Frontend/platform | W1 | ready | isolated required | TBD | high: CI/tests |
| PR-AUTH-001 | Frontend/API | Integration | proposed | isolated required | TBD | high: auth boundary |
| PR-BILL-001 | Billing | Integration | blocked | isolated required | TBD | high: billing/config |
| PR-DATA-001 | Data/platform | Hardening | blocked | isolated required | TBD | high: migrations/ops |
| PR-PROV-001 | Provider operations | Hardening | blocked | isolated required | TBD | medium |
| PR-SEC-001 | Security | Release | proposed | review scope required | TBD | high |
| SEC-RECEIPT-001 | Security / receipt authorization | Release hardening | blocked-on-KEO-64 | `D:/Repos/BioStack-sec-receipts` | `codex/sec-receipt-authorization` | medium: receipt API/UI |
| SEC-LINK-001 | Security / analyzer egress | Release hardening | ready | `D:/Repos/BioStack-sec-link-analyzer` | `codex/sec-link-analyzer` | medium: analyzer/API composition |
| PR-REL-001 | Release | Release | blocked | isolated required | TBD | high: ledger/evidence |

Dependency order: `PR-DOC-001 -> PR-CI-001 -> integration/hardening parcels -> PR-SEC-001 -> PR-REL-001`.

Shared CI, auth, billing, deployment, migration, and ledger surfaces are serialization points. Contract amendments must land before dependent work resumes.
