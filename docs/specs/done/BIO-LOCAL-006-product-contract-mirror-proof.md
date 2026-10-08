---
ticket: BIO-LOCAL-006
title: Product contract mirror proof (v1.0.0)
status: active
owner: clinton.morgan
created: 2026-09-18
updated: 2026-09-18
supersedes: null
superseded_by: null
risk: standard
surfaces:
  - contracts/product-contract.v1.json
  - backend/src/BioStack.Application/Services/FeatureGate.cs
  - frontend/src/components/protocol-portal/TierGate.tsx
routing_class: standard-feature
data_classification: internal
---

# BIO-LOCAL-006 — Product contract mirror proof

## Goal

Prove `product-contract.v1.json` v1.0.0 is intact, mirrors are in sync, and entitlements gate honestly locally.

## Initiative

`biostack-local-readiness`.

## Project Track

T5 contracts.

## Wave

proof (first batch, independent alongside BIO-LOCAL-001).

## Branch

`proof/bio-local-006-contract-mirrors`

## Worktree

`D:\Repos\BioStack.BIO-LOCAL-006`

## Dependencies

None (forks `origin/main@e5b75e0`).

## Integration Surfaces

L7.

## Security Gate

SG-L5 (partial: entitlement/paywall honesty). No hostile PII/billing probes beyond contract-shape assertions; no live Stripe contact.

## Intent

Replace "contract presumably in sync" with a pinned record: v1.0.0 shape (monthly-only USD 0/1200/2900¢; paid access Active/Trialing; grace 0; canonical `/start` + `/tools/analyzer`; aliases; health paths), `sync-product-contract.mjs --check` green, `FeatureGate` entitlement mapping asserted, TierGate enforcement observed locally (real tier-rank, honest upgrade card), and drift guard understood.

## Constraints

- OQ assumptions (Gate 1 OPEN — recommended defaults stated explicitly per charter):
  OQ1 = focused existing contract/entitlement/tier tests + `--check` (full backend suite NOT required);
  OQ3 = no INDEX work in this parcel. Stop on conflict.
- Frozen contract: any shape change (price, tier, route, grace) is a loop-stop (version bump + mirrors + decision + tests), never a parcel edit.
- No live Stripe calls, no real price IDs, no checkout execution — shape and gating only.
- Read-only against contract, mirrors, `FeatureGate`, TierGate; drift found → stop-and-report.

## Acceptance Criteria

1. `contracts/product-contract.v1.json` parses as v1.0.0 effective 2026-07-13 with the exact billing/routes/health values in `CONTRACTS.md` C1.
2. `scripts/sync-product-contract.mjs --check` exits 0 (mirrors in sync); mirror paths recorded.
3. `FeatureGate` maps every `features.*.minimumTier` + Observer 8-compound limit to enforcement observed in tests or local run (record which).
4. TierGate renders honestly for locked tiers locally (upgrade card, no fake content behind the gate); monthly-only + grace-zero behavior asserted by test or observed logic (cite file:line).
5. Route aliases (`/onboarding`→`/start`, `/map`→`/tools/analyzer`) + health paths (`/health`, `/health/keon`) asserted against code (cite file:line); runtime cross-cite deferred to BIO-LOCAL-002's transcripts (record 002 PR link as follow-up — 002 runs after 006, so its transcripts cannot exist at 006 close).
6. Evidence file written; `git diff --check` clean.

## Out of Scope

- Changing prices, tiers, routes, entitlements, or copy.
- Live billing lifecycle, webhooks, refunds, portal execution.
- Fixing drift (stop-and-report; amendment parcel follows).

## Existing Patterns To Follow

- `contracts/product-contract.v1.json` — source of truth (read, never edit here).
- `scripts/sync-product-contract.mjs` — the mirror mechanism under proof.
- `BioStack.Application/Services/FeatureGate.cs` — the consumer mapping.
- `docs/INITIATIVES/biostack-production-readiness/CONTRACTS.md` §"Authoritative product contract" — the v1.0.0 launch decisions being re-proven at this SHA.

## Contract

None (frozen C1 re-proven, not changed).

## Required Tests

Run existing contract/entitlement/tier tests that cover this surface (record exact `dotnet test --filter` / `vitest` invocations + counts); `sync-product-contract.mjs --check` (mandatory). No new persistent tests here.

## Allowed Files

- `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-006-contract-mirror-proof.md`

## Forbidden

- Editing the contract, mirrors, `FeatureGate`, TierGate, pricing/billing copy.
- Contacting Stripe, using real price IDs or customer data.
- Bumping versions or "fixing" drift silently.

## Verification

- JSON shape assertions (version, plans, grace, routes, aliases, health paths) with `jq`/equivalent output saved.
- `node scripts/sync-product-contract.mjs --check` (exit code + output saved).
- Entitlement/tier test runs (commands + counts + failures verbatim).
- TierGate + alias + health-path citations (file:line) + cross-cites to 001/002 runs.
- `git diff --check`.
- Success: AC1–AC6 hold; zero drift.

## Evidence Required

- Evidence file (shape dump, `--check` output, test outputs, citation table).
- PR link + row for LS10 + SG-L5 (partial) for coordinator merge.

## Collision Risk

High if drift repair is needed — but this parcel only READS the serialization points (`contracts/*`, sync script, `FeatureGate`). Any repair is a NEW parcel, sequenced.

## PR Notes

- What changed: evidence file only.
- Why: LS10 + SG-L5 (partial) + C1 re-proof at `e5b75e0`.
- Risk: contract drift voids every entitlement gate — report immediately.
- Verification: reviewer replays `--check` + test filters.
- Evidence: evidence file path.

## Session Handoff

- Starting commit: / Ending commit: / Files changed: / Commands run: / Tests passed: / Tests failed: / Decisions needed: / Blockers: / Next safe action: / Do not touch:

## Stop-and-Report Rule

Drift, shape mismatch, entitlement bypass, or missing decision → stop, record verbatim, request amendment/remediation parcel. Never edit the contract to make `--check` pass.

## Verification Plan

Reviewer focus questions:

- Is `--check` run against the PINNED commit's mirrors (not regenerated-then-checked)?
- Does the entitlement proof cite enforcement code + observed behavior, or just the JSON?
- Is grace-zero (PastDue downgrades immediately) asserted anywhere, or silently skipped?

## Context & References

- `contracts/product-contract.v1.json`
- `scripts/sync-product-contract.mjs`
- `BioStack.Application/Services/FeatureGate.cs`
- `docs/INITIATIVES/biostack-local-readiness/CONTRACTS.md` (C1)
- `docs/INITIATIVES/biostack-local-readiness/SCENARIOS.md` (LS10)
