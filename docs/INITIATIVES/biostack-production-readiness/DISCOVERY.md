# Discovery

## Classification

Multi-project initiative mode within one repository: frontend, API, PostgreSQL, Stripe, email delivery, Azure Container Apps, CI, legal/privacy, security, and operations must compose. A merged PR is insufficient proof.

## Repository and environments

| Item | Role | State |
|---|---|---|
| `m0r6aN/biostack` | Product, API, deployment and evidence | `main@a37726a`; branch `codex/production-readiness-phase2` |
| Local | Focused implementation verification | partial |
| GitHub-hosted CI | Required pre-deploy gate | failing run `29166449446` |
| Azure production | Customer exposure | not deployed by latest run; blocked |
| Stripe/email/PostgreSQL | Live dependencies | configuration and lifecycle evidence incomplete |

## Current facts

- PR #181 merged calculator, monthly-only commerce, provider intake, CI restore, legal containment, and the launch ledger.
- Offline verification run `29166449452` passed at `a37726a`.
- Deploy run `29166449446` failed in frontend tests: three assertion failures plus a worker OOM/timeout; Azure login, builds, pushes, and updates were skipped.
- Legal/privacy approval, frontend consent acceptance, live billing lifecycle, deployed auth/email, backups/restore, probes, monitoring, rollback, accessibility/browser evidence, and support ownership remain open.

## Constraints

- Preserve observational, educational, evidence-aware, non-prescriptive behavior.
- No readiness claim without commit-, environment-, configuration-, command-, and artifact-specific evidence.
- No secrets, health payloads, PII, or production credentials in evidence.
