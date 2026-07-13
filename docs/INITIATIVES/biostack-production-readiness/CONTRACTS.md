# Contracts

| ID | Owner | Contract | Current state | Amendment rule |
|---|---|---|---|---|
| C1 | Product/billing | Launch offers monthly Observer, Operator and Commander behavior only; no annual promise | implemented; live approval blocked | Product approval plus UI/API/test/config updates |
| C2 | Auth/API | Magic-link session, protected-route return path and owner-scoped access | code evidence partial; live unverified | Auth/security review and scenario updates |
| C3 | Consent/legal | Versioned observational consent precedes authenticated writes | API enforced; frontend/human approval failing | Legal/privacy approval and versioned migration |
| C4 | Deployment | Tests pass before OIDC login; immutable SHA images; smoke then rollback | pre-mutation gate works; smoke/rollback absent | Platform review and rehearsed procedure |
| C5 | Evidence | Status is environment-, commit-, config-, command- and date-specific | adopted here | Coordinator approval; never silently upgrade status |

Contract changes require an entry in `DECISIONS.md`, affected scenario/gate updates, fixtures/tests where applicable, migration notes, and coordinator approval before dependent work resumes.
