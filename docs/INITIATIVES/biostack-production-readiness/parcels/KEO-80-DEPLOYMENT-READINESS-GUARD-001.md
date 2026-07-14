# KEO-80 Deployment Readiness Guard — Parcel 001

## Contract

- Linear issue: KEO-80
- Objective: make the deployment workflow reject an exact-SHA Container App revision that never becomes the latest ready revision, then smoke-check only after that identity is proven.
- Starting branch and immutable commit: `main` at `efd02f2b77d841841a4404dff6733e9a3a9a6d94`.
- Isolated worktree: `D:\Repos\BioStack-keo80-deploy-readiness`.
- Allowed writes: `.github/workflows/deploy.yml`, `scripts/verify-containerapp-deployment.mjs`, `scripts/verify-containerapp-deployment.test.mjs`, and this parcel handoff.
- Do not touch: application behavior, Stripe values, Azure configuration, traffic, DNS, credentials, migrations, rollback, probes, or production resources.
- Expected artifact: a deterministic exact-image/latest-ready gate plus API and web smoke checks in the hosted deployment workflow.

## Acceptance and validation

- The observed `Unhealthy/ActivationFailed` API candidate is classified as failed.
- A previous latest-ready revision cannot make the new candidate pass.
- Readiness requires exact immutable image identity, `latestRevisionName == latestReadyRevisionName`, `Healthy`, and `Provisioned`.
- The public smoke check runs only after the revision identity and readiness checks pass.
- Validation: `node --test scripts/verify-containerapp-deployment.test.mjs` and workflow syntax inspection.

## Dependencies and gates

- Depends on the existing GitHub OIDC Azure login and exact Container App/resource-group secrets.
- Human gates remain for approved production Stripe configuration, merge, redeploy, traffic or rollback actions, and live qualification.
- KEO-83 owns platform probes and rollback rehearsal; this parcel does not implement either.
- Retry limit: three materially similar hosted failures. Escalate with the exact run, revision, state, and non-secret log evidence.

## Session handoff

The 2026-07-14 main deployment run `29357949082` reported success although API revision `biostackmissionctrl-api--0000071` entered `ActivationFailed` and the previous revision remained latest-ready. This parcel converts that state into a failed workflow without mutating live resources beyond the already-authorized update performed by the workflow.
