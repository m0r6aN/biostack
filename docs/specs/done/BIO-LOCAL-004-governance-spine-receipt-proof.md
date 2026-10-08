---
ticket: BIO-LOCAL-004
title: Governance Spine and receipt proof (local)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: elevated
surfaces:
  - backend/src/BioStack.Domain/Governance
  - backend/src/BioStack.Infrastructure/Governance
  - backend/src/BioStack.Infrastructure/Keon
  - backend/src/BioStack.Cognition
routing_class: architecture/risk
data_classification: internal
---

# BIO-LOCAL-004 — Governance Spine and receipt proof

## Goal

Prove the Governed Spine, checkpoints, receipt anchoring posture, fail-closed boot check, and cognition separation hold locally.

## Initiative

`biostack-local-readiness`.

## Project Track

T4 governance-spine.

## Wave

proof (after 003; serialized — Spine/Keon/cognition surfaces).

## Branch

`proof/bio-local-004-spine-receipts`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-004`

## Dependencies

BIO-LOCAL-001 (boot), BIO-LOCAL-003 (isolation baseline for receipt scoping).

## Integration Surfaces

L5 + L6 (partial) + L4 (hash round-trip).

## Security Gate

SG-L3 (partial: hash round-trip + scoping) + SG-L5 (partial: fail-closed config) + SG-L6 (partial: receipt/log payload safety). Dual review. Probes: chain-tamper detection, checkpoint-signature negative, receipt-authz bypass, cognition-direct-render attempt.

## Intent

Prove locally that the Spine is append-only and hash-chained through SQLite (with the known `DateTimeKind`/precision behavior), checkpoints sign and verify, receipts anchor (with stubbed-runtime posture stated plainly), Production boot refuses stub without explicit acknowledgment (unit/config proof — never a prod boot), receipt views deny anonymous, and cognition produces envelopes, never user-facing text.

## Constraints

- SQLite only; document the round-trip behavior explicitly (do not claim Postgres equivalence — PG truncation is a recorded difference).
- Stubbed runtime in Development is a stated convenience; every receipt claim carries the posture label.
- Read-only against governance/cognition code; tamper-detection is proven by test + negative probe, never by mutating the chain in a shared DB.
- Signing-key handling: never record key material; posture only.

## Acceptance Criteria

1. Spine write→read round-trip is byte-identical through SQLite (existing chain/hash tests green; record counts).
2. Signed checkpoint verifies; tampered entry/checkpoint is detected (negative proof recorded).
3. Receipt write→read holds scoping; anonymous `/governance/receipts` denies; receipt-authz negative passes.
4. Fail-closed boot unit/config test passes (stub-without-acknowledgment refuses Production-track config; `StubAllowAll` rejected) — as POLICY proof, not a production boot.
5. Cognition separation asserted by assembly/citation: deliberation output flows via envelope, no direct user-text path (cite file:line per claim).
6. Evidence file written; `git diff --check` clean; redaction attestation (no key material, no payload dumps).

## Out of Scope

- Fixing chain/crypto/receipt code (stop-and-report).
- Postgres equivalence, checkpoint-cadence timing sign-off, live Keon wiring.
- Public receipt surfaces (authenticated-only by design).

## Existing Patterns To Follow

- `README.md` Governance section — the exact Spine/checkpoint/Keon claims being proven.
- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs` — fail-closed wording.
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-RECEIPT-001.md` + `BIO-RT-01-FAIL-CLOSED-OFFLINE-RECEIPTS.md` — prior remediations under proof.

## Contract

None. Behavior contract: C4 (governance) holds locally with stated stubbed posture.

## Required Tests

Run existing Spine/governance/Keon-focused backend tests (record exact filters + counts); manual probe transcripts for tamper/receipt-authz/cognition-path. No new persistent tests here.

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-004-spine-receipt-proof.md`

## Forbidden

- Editing chain, checkpoint, Keon adapter, cognition, migration, or receipt code.
- Recording signing keys, forging checkpoints, mutating shared chain state.
- Claiming live/production governance from stubbed runs.

## Verification

- Focused `dotnet test` governance/Spine/Keon filters (counts + failures verbatim).
- Round-trip + tamper-detection + checkpoint-verify outputs.
- `/health/keon` transcript (cite 001's run; add receipt-view denial transcript here).
- Fail-closed config/unit proof output.
- Cognition file:line citation table.
- `git diff --check`.
- Success: AC1–AC6 hold; every posture claim labeled stubbed-vs-live.

## Evidence Required

- Evidence file (test outputs, transcripts, tamper table, citation table).
- PR link + rows for LS7/LS8 + SG-L3/L5/L6 (partial) for coordinator merge.

## Collision Risk

High. Spine/Keon/cognition assemblies are serialization points — sequenced alone.

## PR Notes

- What changed: evidence file only.
- Why: LS7 + LS8 + SG-L3/L5/L6 (partial).
- Risk: hash-round-trip or fail-closed regression is release-blocking.
- Verification: reviewer replays focused tests + denial probes.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Chain break, checkpoint failure, receipt-authz bypass, cognition direct-render path, or key-handling question → stop, file finding, request remediation parcel. Never work around governance to keep the proof green.

## Verification Plan

Reviewer focus questions:

- Is the SQLite round-trip proven on REAL chain bytes (not a mock hash)?
- Does the tamper test mutate a COPY and detect, or does it touch shared state?
- Is every receipt/governance claim labeled with stubbed posture, or does any sentence read as live?

## Context & References

- `README.md` (Governance)
- `backend/src/BioStack.Infrastructure/Keon/KeonRuntimeDependencyInjection.cs`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-RECEIPT-001.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/BIO-RT-01-FAIL-CLOSED-OFFLINE-RECEIPTS.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS7, LS8)
