# Security Gates

| Gate | Surface/threat | Required evidence | Status | Blocking |
|---|---|---|---|---|
| SG1 | Auth/session/ownership bypass | live auth flow, deny tests, ownership isolation review | blocked | yes |
| SG2 | Billing forgery/replay/secrets | webhook signature/idempotency tests, secret/config review | blocked | yes |
| SG3 | Sensitive data loss/cross-user access | authz review, backup encryption/restore and redaction proof | blocked | yes |
| SG4 | Provider PII abuse/retention | rate-limit, minimization, access, retention and deletion review | blocked | yes |
| SG5 | Supply chain/deployment credentials | required scans, OIDC/RBAC, immutable artifact and rollback review | blocked | yes |
| SG6 | Logging/analytics leakage | structured logging and telemetry payload review | blocked | yes |
| SG7 | Public safety/legal claims | non-prescriptive copy, legal/privacy and consent approval | blocked | yes |

`29cbc98` remediates the tracked base-config secret defaults and makes missing production injection fail closed. SG2 and SG5 remain blocked until prior values are rotated or formally invalidated as needed, the hosted Gitleaks job passes, and billing/webhook security evidence exists.

Local candidate `565805a` adds integrated receipt authorization, analyzer egress controls, non-enumerating provider intake, atomic magic-link consumption, server-selected consent evidence, a non-root backend container, patched PostCSS resolution, and the offline-verification fetch repair. The application/dependency state at `fb0ed84` produced 1,088 passing backend tests, 900 passing frontend tests, a successful production frontend build, and a zero-vulnerability production npm audit. These results reduce code-level risk but do not satisfy the environment-specific requirements in the table; every gate therefore remains blocked until its hosted, operational, or approval evidence exists.

A dedicated defensive security review is required before release. No waiver exists. Waivers require named authority, scope, expiry, rationale, compensating controls, and release-owner acceptance.
