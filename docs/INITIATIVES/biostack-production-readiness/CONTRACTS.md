# Contracts

| ID | Owner | Contract | Current state | Amendment rule |
|---|---|---|---|---|
| C1 | Product/billing | `contracts/product-contract.v1.json` defines launch plans, monthly prices, entitlements, route aliases and health paths; no annual promise | implemented in v1.0.0; live approval blocked | Version bump plus generated mirrors, decision entry and dependent tests |
| C2 | Auth/API | Magic-link session, protected-route return path and owner-scoped access | code evidence partial; live unverified | Auth/security review and scenario updates |
| C3 | Consent/legal | Versioned observational consent precedes authenticated writes | API enforced; frontend/human approval failing | Legal/privacy approval and versioned migration |
| C4 | Deployment | Tests pass before OIDC login; immutable SHA images; smoke then rollback | pre-mutation gate works; smoke/rollback absent | Platform review and rehearsed procedure |
| C5 | Evidence | Status is environment-, commit-, config-, command- and date-specific | adopted here | Coordinator approval; never silently upgrade status |

Contract changes require an entry in `DECISIONS.md`, affected scenario/gate updates, fixtures/tests where applicable, migration notes, and coordinator approval before dependent work resumes.

## Authoritative product contract

`contracts/product-contract.v1.json` is the source of truth. `scripts/sync-product-contract.mjs` creates the frontend and backend build-context mirrors, and `--check` fails validation when either mirror drifts. Version 1.0.0 fixes these launch decisions:

- billing is USD monthly only: Observer $0, Operator $12, Commander $29;
- only `Active` and `Trialing` retain paid access; `PastDue` downgrades immediately with zero grace days;
- `/start` is canonical onboarding and `/tools/analyzer` is canonical analysis; `/onboarding` and `/map` remain redirect aliases;
- `/health` is liveness and `/health/keon` is the Keon dependency probe.
