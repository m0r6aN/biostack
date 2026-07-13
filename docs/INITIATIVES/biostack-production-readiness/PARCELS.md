# Parcel Index

| Parcel | Track | Wave | Status | Worktree | Branch | Collision risk |
|---|---|---|---|---|---|---|
| PR-DOC-001 | Evidence/release | Foundation | in-progress | `D:/Repos/BioStack-release-governance` | `codex/production-readiness-phase2` | low; docs only |
| PR-CI-001 | Frontend/platform | W1 | integrated-local | `D:/Repos/BioStack-keo64-claude` | `claude/keo-64-release-ci` | high: CI/tests |
| PR-AUTH-001 | Frontend/API | Integration | proposed | isolated required | TBD | high: auth boundary |
| PR-BILL-001 | Billing | Integration | blocked | isolated required | TBD | high: billing/config |
| PR-DATA-001 | Data/platform | Hardening | blocked | isolated required | TBD | high: migrations/ops |
| PR-PROV-001 | Provider operations | Hardening | blocked | isolated required | TBD | medium |
| PR-SEC-001 | Security | Release | integrated-local | `D:/Repos/BioStack-security-integration` | `codex/security-integration` | high |
| SEC-RECEIPT-001 | Security / receipt authorization | Release hardening | integrated-local | `D:/Repos/BioStack-sec-receipts` | `codex/sec-receipt-authorization` | medium: receipt API/UI |
| SEC-LINK-001 | Security / analyzer egress | Release hardening | integrated-local | `D:/Repos/BioStack-sec-link-analyzer` | `codex/sec-link-analyzer` | medium: analyzer/API composition |
| SEC-PROVIDER-001 | Security / provider intake | Release hardening | integrated-local | `D:/Repos/BioStack-sec-provider-access` | `codex/sec-provider-access` | low-medium: provider API |
| SEC-AUTH-001 | Security / authentication | Release hardening | integrated-local | `D:/Repos/BioStack-sec-magic-link` | `codex/sec-magic-link-atomic` | medium: auth endpoint |
| SEC-CONSENT-001 | Security / consent evidence | Release hardening | integrated-local | `D:/Repos/BioStack-sec-consent` | `codex/sec-consent-version` | low-medium: consent service |
| SEC-CONTAINER-001 | Security / container hardening | Release hardening | integrated-local | `D:/Repos/BioStack-sec-container` | `codex/sec-backend-container` | low: backend Dockerfile |
| SEC-DEPS-001 | Security / frontend dependencies | Release hardening | integrated-local | `D:/Repos/BioStack-sec-frontend-deps` | `codex/sec-frontend-postcss` | medium: package lock |
| PR-REL-001 | Release | Release | blocked | isolated required | TBD | high: ledger/evidence |

Dependency order: `PR-DOC-001 -> PR-CI-001 -> integration/hardening parcels -> PR-SEC-001 -> PR-REL-001`.

Shared CI, auth, billing, deployment, migration, and ledger surfaces are serialization points. Contract amendments must land before dependent work resumes.
