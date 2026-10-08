---
ticket: BIO-LOCAL-003
title: Auth and tenancy isolation proof (local)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: elevated
surfaces:
  - backend/src/BioStack.Api/Endpoints/AuthEndpoints.cs
  - backend/src/BioStack.Application/Services/Consent
  - backend/src/BioStack.Api/Endpoints/ProviderAccessEndpoints.cs
  - frontend/src/middleware.ts
routing_class: architecture/risk
data_classification: internal
---

# BIO-LOCAL-003 — Auth and tenancy isolation proof

## Goal

Prove local sign-in, denial, ownership isolation, consent gating, and provider non-enumeration at the pinned SHA.

## Initiative

`biostack-local-readiness`.

## Project Track

T2/T5 auth-isolation.

## Wave

proof (after 002; serialized — auth boundary).

## Branch

`proof/bio-local-003-auth-isolation`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-003`

## Dependencies

BIO-LOCAL-001 (boot), BIO-LOCAL-002 (honesty baseline).

## Integration Surfaces

L3 + L4 (partial).

## Security Gate

SG-L1 (primary) + SG-L3 (partial: cross-user) + SG-L4 (provider intake). Dual adversarial review (charter: architecture/risk). Hostile probes: replayed/tampered magic links, direct-ID access across users, consent-version spoofing, provider enumeration oracle attempts.

## Intent

Prove the local auth story end to end with the Development in-memory inbox: real magic-link start→verify→return-path, passkey posture documented (not live-proven beyond local capability), protected-route denial, cross-user denial on profiles/protocols, consent-version enforcement, and provider intake that neither enumerates nor accepts abusive input.

## Constraints

- Local test identities only; no real email addresses; inbox is in-memory (Development).
- Atomic magic-link consumption asserted (replay of a consumed link fails).
- Read-only against product code; any bypass found is a High/Critical finding → gate fails, remediation parcel follows.
- Consent: server-selected version evidence asserted; client-supplied version strings never trusted.

## Acceptance Criteria

1. Magic-link start→verify completes locally and lands on the intended protected route; unauthenticated protected-route access denies/redirects; consumed/tampered/expired links fail closed (incl. replay attempt).
2. User A cannot read, write, or list user B's profile/protocol/check-in via direct ID or collection scoping (negative tests recorded).
3. Authenticated write without the current consent version is rejected; acceptance records server-selected version evidence.
4. Provider intake: identical responses for existing vs non-existing contact (non-enumeration), oversize/abusive input rejected, stored payload is privacy-minimal.
5. Passkey/WebAuthn + OAuth posture documented as code-present/config state (no live-provider claim).
6. Evidence file written; `git diff --check` clean; redaction attestation (test identities clearly synthetic).

## Out of Scope

- Fixing auth flaws (stop-and-report; remediation parcel follows).
- Live email/SMTP, production session security, OAuth live flows.
- Billing lifecycle, Postgres, backups.

## Existing Patterns To Follow

- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-AUTH-001.md` + `SEC-CONSENT-001.md` + `SEC-PROVIDER-001.md` — the exact remediations being proven locally (read as constraints, not as passing claims).
- `docs/guidance/RATIFICATION.md` — consent/projection boundaries that must not regress.

## Contract

None. Behavior contract: C3 (auth/consent) holds locally.

## Required Tests

Run the repo's existing auth/consent-focused backend tests that cover this surface (record exact `dotnet test --filter` invocations + counts); manual curl/browser traces for the link flow and denial probes. New persistent tests are NOT added here (a remediation parcel adds them if a gap is found).

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-003-auth-isolation-proof.md`

## Forbidden

- Editing auth/consent/provider code, session handling, middleware, or migrations.
- Hitting live email/OAuth/Stripe, using real identities, or persisting real PII.
- Downgrading any bypass finding without coordinator + named-owner waiver.

## Verification

- `dotnet test` (focused auth/consent filters; record counts, failures verbatim).
- Magic-link flow trace (start → inbox read → verify → return-path landing), plus replay/tamper/expired negatives.
- Cross-user direct-ID + list-scope denial transcripts (A vs B, synthetic).
- Consent-version negative + acceptance evidence check.
- Provider non-enumeration + limits probes.
- `git diff --check`.
- Success: AC1–AC6 hold; zero un-triaged bypasses.

## Evidence Required

- Evidence file (traces, transcripts, test outputs, probe table with finding IDs or explicit no-finding statement).
- PR link + rows for LS4/LS5/LS6 + SG-L1/L3(partial)/L4 for coordinator merge.

## Collision Risk

High. Auth/consent/provider endpoints are serialization points — this parcel sequences alone; 004/005 wait.

## PR Notes

- What changed: evidence file only.
- Why: LS4–LS6 + SG-L1/SG-L4.
- Risk: any ownership bypass is release-blocking by definition.
- Verification: reviewer replays link flow + denial probes from the evidence file.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Bypass, enumeration oracle, consent spoof, or missing product decision → stop, file finding with severity + reproduction, request remediation parcel. Do not fix in-proof.

## Verification Plan

Reviewer focus questions (dual review, disputed findings reproduced by coordinator):

- Does the replay test use a genuinely consumed link, or a fresh one (false negative)?
- Is cross-user denial proven at BOTH the API and the query-scoping layer, or just the UI?
- Does non-enumeration hold on timing AND content, or only on status code?

## Context & References

- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-AUTH-001.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-CONSENT-001.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/SEC-PROVIDER-001.md`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS4, LS5, LS6)
