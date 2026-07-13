# Evidence Index

| ID | Artifact | Commit/environment | What it proves | Limits/status |
|---|---|---|---|---|
| E1 | PR #181 | merged to `a37726a` | calculator, commerce, provider and CI implementation package | merged code is not release proof |
| E2 | Workflow `29166449446` | GitHub Actions, `a37726a` | backend gate passed; frontend gate failed; Azure untouched | failing, release-blocking |
| E3 | Workflow `29166449452` | GitHub Actions, `a37726a` | offline kit guard and diff hygiene passed | scoped only |
| E4 | `docs/launch-readiness-ledger.md` | reconciled to `a37726a` | detailed gate inventory and deterministic commands | current verdict NO-GO |
| E5 | `BIOSTACK_FRONTEND_READINESS_AUDIT.md` | static audit against `a37726a` | UX/IA findings and remaining live-test needs | not a live browser pass |

Evidence must not contain secrets, tokens, health payloads, payment details, or direct PII. Links and summaries must identify limitations.
