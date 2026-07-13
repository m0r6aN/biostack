# Evidence Index

| ID | Artifact | Commit/environment | What it proves | Limits/status |
|---|---|---|---|---|
| E1 | PR #181 | merged to `a37726a` | calculator, commerce, provider and CI implementation package | merged code is not release proof |
| E2 | Workflow `29166449446` | GitHub Actions, `a37726a` | backend gate passed; frontend gate failed; Azure untouched | failing, release-blocking |
| E3 | Workflow `29166449452` | GitHub Actions, `a37726a` | offline kit guard and diff hygiene passed | scoped only |
| E4 | `docs/launch-readiness-ledger.md` | reconciled to `a37726a` | detailed gate inventory and deterministic commands | current verdict NO-GO |
| E5 | `BIOSTACK_FRONTEND_READINESS_AUDIT.md` | static audit against `a37726a` | UX/IA findings and remaining live-test needs | not a live browser pass |
| E6 | `29cbc98` | local phase-two branch | removes credential-bearing base defaults, restores secret rule, and proves production fails closed without injection | external rotation and hosted scan pending |
| E7 | `ef4c15c` | local phase-two branch | aligns stale launch assertions and bounds Vitest concurrency in deploy CI | hosted run pending; local install timed out |
| E8 | `8ebb9ba` | local phase-two branch | adds tablet, result-consistency and keyboard/dialog accessibility coverage | focused runner hang and browser proof remain open |
| E9 | `b389db7` | local `claude/keo-64-release-ci` | repairs the calculator TypeScript blocker and stabilizes the profile mock; exact frontend suite and production build pass | no hosted workflow or browser session proof |
| E10 | `fb0ed84` | local `codex/security-integration` | integrates seven KEO-65 remediations plus patched PostCSS; 1,088 backend and 900 frontend tests pass; production build and audit pass | not pushed/deployed; SR-04, SR-07 and broader release gates remain open |
| E11 | `565805a` | local `codex/security-integration` | adds the one-line offline-verification fetch correction on top of verified code state `fb0ed84` | hosted workflow not run; no deployment evidence |

Evidence must not contain secrets, tokens, health payloads, payment details, or direct PII. Links and summaries must identify limitations.
