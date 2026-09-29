---
ticket: BIO-LOCAL-001
title: Local dev-stack boot proof (compose, health, reset)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: standard
surfaces:
  - docker-compose.dev.yml
  - backend/src/BioStack.Api/Program.cs
  - frontend/package.json
routing_class: standard-feature
data_classification: internal
---

# BIO-LOCAL-001 — Local dev-stack boot proof

## Goal

Prove `docker-compose.dev.yml` boots API + UI locally at the pinned SHA and resets cleanly.

## Initiative

`biostack-local-readiness` (`docs/INITIATIVES/biostack-local-readiness/`).

## Project Track

T1/T2 platform-local.

## Wave

proof (first batch, independent alongside BIO-LOCAL-006).

## Branch

`proof/bio-local-001-local-dev-boot`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-001`

## Dependencies

None (forks `origin/main@e5b75e0`).

## Integration Surfaces

L1.

## Security Gate

SG-L5 (partial: local config/secrets posture) + SG-L6 (partial: no payload in logs/artifacts). Findings triaged per `SECURITY-GATES.md` severity mapping.

## Intent

Replace "dev stack presumably boots" with a pinned run record: compose build + up, `/health` 200, `/health/keon` stubbed-dev posture stated (never oversold as governance), frontend `:3043` reachable, SQLite volume behavior, and `down -v` reset. This is the environment every later parcel's `local` claim stands on.

## Constraints

- OQ assumptions (Gate 1 OPEN — recommended defaults stated explicitly per charter):
  OQ2 = docker-compose boot REQUIRED (host `dotnet`/`npm` runs supplementary only);
  OQ1 = zero automated tests (manual boot verification per spec); OQ3 = no INDEX work in this parcel.
  Stop on conflict.
- Local only. No prod-shaped compose, no Azure, no Stripe live, no SMTP, no Postgres.
- `.env` from `.env.example` with blank/placeholder secrets only; never real credentials.
- Read-only against product code: run containers, probe, capture logs. If boot fails due to a code defect, STOP and report — do not fix in this parcel (a bounded remediation parcel follows under the charter loop).
- State the stubbed-Keon posture exactly: Development convenience, not a governance claim.

## Acceptance Criteria

1. `docker compose -f docker-compose.dev.yml up --build` reaches healthy API + UI from a clean volume state.
2. `curl http://localhost:5000/health` → 200; `curl http://localhost:5000/health/keon` response body recorded verbatim with stubbed posture noted.
3. Frontend `http://localhost:3043` returns the app shell (HTTP 200).
4. SQLite volume `biostack-dev-data` location/behavior documented; `docker compose -f docker-compose.dev.yml down -v` resets and a second boot succeeds.
5. Evidence file written; `git diff --check` clean; redaction attestation (no secrets/PII/health payload).

## Out of Scope

- Any fix to product code, compose files, or Dockerfiles (stop-and-report instead).
- Prod-shaped compose, image pushes, Azure, billing, email, Postgres drills.
- Performance tuning, seed-data curation, sidecar enablement.

## Existing Patterns To Follow

- `docker-compose.dev.yml` — the exact services/healthchecks/ports asserted.
- `README.md` "Run the development stack" — the documented procedure being proven.
- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs` — the fail-closed wording to quote correctly.

## Contract

None (no interface changes). Behavior contract: dev boot procedure in README holds at the pinned SHA.

## Required Tests

No new automated tests. Manual verification required (boot + probes + reset, commands below).

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-001-boot-proof.md`
- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-001-config-note.md`

## Forbidden

- Editing product code, compose files, Dockerfiles, `.env`, migrations, contracts.
- Starting prod compose, contacting Azure/Stripe/SMTP, loading production data.
- Pasting secrets, tokens, PII, or health payloads into any artifact.

## Verification

- `docker compose -f docker-compose.dev.yml up --build` (record UTC start/end, commit SHA, Docker version)
- `curl -i http://localhost:5000/health`; `curl -i http://localhost:5000/health/keon`
- `curl -i http://localhost:3043` (or `Invoke-WebRequest` equivalent; record exact command)
- `docker compose -f docker-compose.dev.yml down -v` then repeat boot once
- `git diff --check`
- Success: AC1–AC5 all hold; evidence file contains transcripts with timestamps.

## Evidence Required

- Evidence file above (compose log excerpt, curl transcripts, reset note).
- Config note (`evidence/BIO-LOCAL-001-config-note.md`): dev compose + `.env.example` review; prod-compose gap
  (`KeonRuntime__*` not passed through) RECORDED as known limitation, never fixed here.
- PR link (docs-only close) + verification-record row for coordinator to merge into `VERIFICATION.md`.

## Collision Risk

Medium. `docker-compose.dev.yml`/`.env.example` are read (not written) here; 001's config-review note is sequenced with 006 — no parallel writes to shared files.

## PR Notes

- What changed: evidence file only.
- Why: `biostack-local-readiness` LS1 + SG-L5/L6 (partial).
- Risk: local Docker resource cost (prior OOM history noted; record machine constraints).
- Verification: reviewer replays the recorded commands from a clean volume.
- Evidence: evidence file path + run SHAs.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

If boot fails from a product defect, missing decision, or contract change need: stop, report the exact failure + log excerpt, request a remediation parcel. Do not expand scope.

## Verification Plan

Reviewer focus questions:

- Does the `/health/keon` note state stubbed-dev posture without implying deployed governance?
- Is every transcript pinned to commit + UTC + config class, or does any claim float unpinned?
- Does any artifact contain a secret, token, PII, or health payload?

## Context & References

- `docker-compose.dev.yml`
- `README.md` (Local development)
- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS1)
