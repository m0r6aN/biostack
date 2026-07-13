# Verification

## Current evidence baseline

| Run | Environment/build | Procedure | Result |
|---|---|---|---|
| `29166449446` | GitHub Actions, `a37726a` | `.github/workflows/deploy.yml` | **failing**: backend passed; frontend had 3 failed assertions, worker OOM/timeout; Azure steps skipped |
| `29166449452` | GitHub Actions, `a37726a` | offline verification kit guard + diff hygiene | passing |
| PR #181 lane evidence | isolated local/CI lanes, commits recorded in PR | focused backend, API, Docker, commercial and frontend checks | partial; not end-to-end release proof |
| PR-SEC-001 | local, `29cbc98` | serial API build; JSON/config assertions; production startup without injected secret; diff check | build/config checks passed and production failed closed; Gitleaks and external rotation remain blocked |
| PR-CI-001 | local source/diff, `ef4c15c` | stale expectation reconciliation; bounded worker configuration; diff check | diff clean; local dependency install timed out, so hosted rerun is required |
| PR-CALC-002 | local source/diff, `8ebb9ba` | tablet/numeric/keyboard/dialog coverage; diff check | implementation complete; focused Vitest still hangs and no tracked browser harness exists |
| KEO-64 local closeout | local, `b389db7` on `claude/keo-64-release-ci` | exact frontend suite; production build; diff check | 124 files and 898 tests passed; production build passed; undefined instruction references and unstable test mock repaired |
| KEO-65 integrated code state | local, `fb0ed84` on `codex/security-integration` | clean npm install; production audit; exact frontend suite; production build; serial backend build and full tests; diff check | PostCSS 8.5.10; 0 audit vulnerabilities; 125 frontend files/900 tests; 51 static pages; 15 backend projects/0 errors; 1,088 backend tests; diff clean |
| KEO-64 workflow closeout | local, `565805a` on `codex/security-integration` | inspect one-line offline-verification fetch change; diff check | local source/diff pass; hosted workflow rerun required |

The integrated candidate has no hosted workflow, deployment, revision, traffic, or live-smoke evidence. Local success does not change RG1 or the release verdict.

## Required verification record

Every new result records scenario, environment, exact commit, configuration class, UTC time, command/procedure, result, durable artifact/link, limitations, and redaction confirmation.

## Documentation parcel checks

Run `rtk git status --short`, `rtk git diff --check`, `rtk git diff --name-only`, and reference/link checks. Documentation validation does not change the release verdict.
