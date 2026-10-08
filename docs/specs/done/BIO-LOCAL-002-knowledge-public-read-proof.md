---
ticket: BIO-LOCAL-002
title: Public knowledge and tools read proof
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: standard
surfaces:
  - frontend/src/app/knowledge
  - frontend/src/app/tools
  - frontend/src/middleware.ts
  - backend/src/BioStack.Api/Endpoints/KnowledgeEndpoints.cs
routing_class: standard-feature
data_classification: internal
---

# BIO-LOCAL-002 — Public knowledge and tools read proof

## Goal

Prove anonymous public read of the evidence library and tools holds locally with no reasoning leak.

## Initiative

`biostack-local-readiness`.

## Project Track

T1/T2 public-read.

## Wave

proof (after 001 boot + 006 contract shape).

## Branch

`proof/bio-local-002-public-read`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-002`

## Dependencies

BIO-LOCAL-001 (boot), BIO-LOCAL-006 (contract shape for route/entitlement assertions).

## Integration Surfaces

L2 (partial: read paths + reduced-shape honesty on public endpoints).

## Security Gate

SG-L2 (partial: B4/B5 leak-proof on the two public POST surfaces) + SG-L7 (partial: public copy honesty). Reviewer runs hostile-input probes (overlarge/odd compound lists, unauthenticated tier-spoof attempts).

## Intent

Prove at the pinned SHA, against the local stack, that `/knowledge`, dossier, methodology, `/tools/*`, and the canonical/alias routes serve anonymous visitors, while `POST /api/v1/knowledge/overlap-check` and `interaction-check` return ONLY the reduced Observer shape (pairs/ids + explicit `severity: null`, no Description/Reason/Confidence/mechanism/direction).

## Constraints

- Anonymous requests only for the public assertions; authenticated comparison only where the spec says so.
- Reduced-shape rule is load-bearing (charter D6): null means unavailable, never low/no-risk; any reasoning exposure to anonymous fails the parcel.
- Read-only against product code; failures become stop-and-report, not silent fixes.
- No PII/health payload in fixtures; use synthetic compound selections.

## Acceptance Criteria

1. Anonymous GETs return 200: `/knowledge`, one real dossier, `/knowledge/methodology`, `/tools/analyzer`, one calculator; `/onboarding` + `/map` redirect to `/start` and `/tools/analyzer` respectively.
2. Anonymous `overlap-check` + `interaction-check` responses contain no `Description`/`Reason`/`Confidence`/mechanism/direction/consequence keys and carry explicit `severity: null` on reduced items.
3. An entitled Operator identity (local seed) receives the full shape from the same endpoints (positive control that gating discriminates, not blanket-blanking).
4. `middleware.public-routes` test file + contract `publicPrefixes` agree with observed behavior; drift noted, not patched here.
5. Evidence file written; `git diff --check` clean; redaction attestation.

## Out of Scope

- Fixing leaks, copy, SEO, homepage proof-panel content, `/compounds` gating intent (record intent question for DECISIONS.md, do not decide it here).
- Authenticated personalization, billing, provider flows.
- Browser-matrix or accessibility sign-off.

## Existing Patterns To Follow

- `frontend/src/__tests__/middleware.public-routes.test.ts` — the route contract being proven live.
- `frontend/src/components/knowledge/OverlapResults.tsx` — reduced-shape rendering honesty pattern.
- `docs/guidance/RATIFICATION.md` (B3/B4/B5 + severity-null correction) — the exact shaping rule.

## Contract

None (no interface changes). Behavior contract: contract `routes` + RATIFICATION reduced shapes hold locally.

## Required Tests

No new automated tests required unless the parcel elects a throwaway local script (not committed). Manual verification required (HTTP transcripts below).

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-002-public-read-proof.md`

## Forbidden

- Editing endpoints, middleware, UI, contracts, or projections.
- Creating public surfaces, calling prod/staging, using real user data.
- Recording secrets, PII, or health payloads.

## Verification

- Boot per 001 record (reference its SHA/run; do not re-prove boot, cite it).
- Anonymous `curl` GETs for AC1 (record status + final URL after redirects).
- Anonymous POSTs to both check endpoints with a small synthetic payload; full JSON bodies saved in evidence file; key-absence asserted by inspection (`jq`/equivalent key listing).
- Entitled POST pair as positive control (local seeded Operator; record how entitlement was granted, fail-closed default otherwise).
- `git diff --check`.
- Success: AC1–AC5 hold verbatim.

## Evidence Required

- Evidence file (request/response transcripts, key listings, redirect chains, entitlement note).
- PR link + verification rows for LS2/LS3 (partial) for coordinator merge into `VERIFICATION.md`.

## Collision Risk

Medium. Knowledge endpoints + middleware + overlap UI are read-only here; any leak fix is a NEW parcel (serialized after this proof).

## PR Notes

- What changed: evidence file only.
- Why: LS2 + LS3 (partial) + SG-L2/SG-L7 (partial).
- Risk: reduced-shape bypass would be a High finding — report immediately, do not downplay.
- Verification: reviewer replays the recorded curls against a fresh local boot.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Reasoning leak, route drift, or missing-decision (`/compounds` intent, homepage panel) → stop, record verbatim response, request coordinator ruling/remediation parcel. Never silently reclassify a leak as acceptable.

## Verification Plan

Reviewer focus questions:

- Is every "no Description/Reason" claim backed by a full saved body, or by an excerpt that could hide the key?
- Does the positive control prove the gate discriminates (entitled gets full), or does everything return reduced (blanket-blanking)?
- Is `severity: null` presented as unavailable everywhere, with no low-risk language?

## Context & References

- `contracts/product-contract.v1.json` (routes section)
- `docs/guidance/RATIFICATION.md` (B3/B4/B5 + correction)
- `frontend/src/__tests__/middleware.public-routes.test.ts`
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS2, LS3)
