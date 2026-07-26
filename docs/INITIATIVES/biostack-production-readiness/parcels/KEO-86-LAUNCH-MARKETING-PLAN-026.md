# Parcel: KEO-86-LAUNCH-MARKETING-PLAN-026

Status: documentation complete; marketing execution remains `NO-GO / HOLD`.

## Objective

Produce a bounded, evidence-led, low-cost launch and marketing plan grounded in
BioStack's current release posture and server-enforced capabilities.

## Base and isolation

- Repository: BioStack
- Base: `origin/main@53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`
- Branch: `codex/keo86-launch-marketing-plan-20260726`
- Sparse worktree: `D:\Repos\BioStack-keo86-launch-marketing-plan-20260726`
- Sparse paths: `docs`, `research`

## Deliverable

`docs/INITIATIVES/biostack-production-readiness/KEO-86-LAUNCH-MARKETING-PLAN.md`

The plan defines:

- evidence hierarchy and current capability boundaries;
- three ICPs plus excluded audiences;
- non-prescriptive positioning and message hierarchy;
- gated Observer, Operator, Commander, and provider-pilot offers;
- a low-cost owned/organic channel mix;
- a conditional 90-day calendar that starts only after GO;
- budget bands with activation gates;
- owner/dependency assignments;
- a privacy-safe measurement proposal;
- a bounded experiment backlog;
- explicit prohibited claims and stop conditions.

## Evidence used

- `docs/INITIATIVES/biostack-production-readiness/FINAL-HANDOFF.md`
- `docs/launch-readiness-ledger.md`
- `docs/INITIATIVES/biostack-production-readiness/KEO-66-CUSTOMER-SURFACE-INVENTORY.md`
- `backend/src/BioStack.Application/ProductContract/product-contract.v1.json`
- `frontend/src/lib/marketing.ts`
- `frontend/src/app/privacy/page.tsx`
- `frontend/src/app/providers/page.tsx`
- `docs/product/knowledge-engine-capability-map.md`

Historical commercialization material was treated as non-authoritative where
it conflicts with the current product contract, customer-surface inventory, or
non-prescriptive boundary.

## Allowed files

- `docs/INITIATIVES/biostack-production-readiness/KEO-86-LAUNCH-MARKETING-PLAN.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-86-LAUNCH-MARKETING-PLAN-026.md`
- `research/routing-events/keo-86-launch-marketing-plan-20260726.json`

## Forbidden

- Product code, configuration, workflows, tests, lockfiles, and shared indexes.
- Files owned by pull request #230 or any other active implementation parcel.
- Contacting users, providers, partners, communities, or media.
- Publishing content, buying advertising, configuring analytics, sending email,
  changing pricing, enabling checkout, or deploying.
- Live service, account, credential, database, Stripe, Azure, or DNS mutation.
- Medical guidance, clinical claims, individualized action, sourcing, dosing,
  administration, or synthetic evidence.
- Commit, push, pull request, merge, or deployment.

## Acceptance criteria

- The release posture remains explicitly `NO-GO / HOLD`.
- Actual capabilities and paid tiers are separated from activation authority.
- Claims exclusions cover medical, outcome, privacy, provider, billing, support,
  and future-feature overreach.
- The calendar and budget contain explicit gates and stop rules.
- Metrics exclude health/protocol payloads and require privacy approval.
- Only the three allowed documentation/evidence files change.
- Routing event validates against the canonical schema.
- `git diff --check` passes.

## Verification

```powershell
rtk proxy pwsh -NoProfile -Command "(Get-Content -Raw 'research/routing-events/keo-86-launch-marketing-plan-20260726.json') | Test-Json -SchemaFile 'D:/Repos/keon-omega/keon-skills/skills/optimized-model-routing-orchestrator/references/routing-event.schema.json'"
rtk git diff --check
rtk git status --short
```

## Closeout

This parcel authorizes no marketing execution. The next action is owner review
of the plan and closure of the existing release gates. Only the release owner
may start the conditional 90-day clock.
