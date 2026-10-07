# Parcel: KEO-68-BILLING-PLAN-DEEP-LINK-028

Status: implementation complete; hosted frontend verification pending.

## Objective

Complete the existing pricing-to-billing handoff. Pricing already links to:

- `/billing?plan=operator`
- `/billing?plan=commander`

The billing page previously ignored `plan`, forcing an authenticated visitor to
select the same tier a second time.

## Contract

After the current subscription loads successfully and therefore confirms the
authenticated billing context:

1. Accept only `operator` or `commander`.
2. Remove the valid `plan` parameter immediately before creating checkout.
3. Create at most one checkout session under React Strict Mode, repeated
   effects, rapid clicks, or a remount/refresh after URL consumption.
4. Preserve the query when subscription/authentication loading fails.
5. Reject invalid plan values without creating checkout or hiding manual plan
   controls.
6. Show an honest failure message and allow manual retry if checkout creation
   fails.

Automatic and manual checkout share the same in-flight guard. This change does
not alter Stripe products, prices, webhooks, entitlements, portal behavior, or
production configuration.

## Scope

This parcel changes only:

- `frontend/src/app/billing/page.tsx`
- `frontend/src/__tests__/app/BillingPageIntent.test.tsx`
- this parcel and its routing event

The files do not overlap PR #230. No package manifest, lockfile, live Stripe
account, live data, deployment, or primary BioStack checkout is changed.

## Verification

Focused tests cover:

- one session under Strict Mode;
- URL consumption preventing remount/refresh duplication;
- unauthenticated/load-failure preservation of plan intent;
- invalid-plan rejection;
- honest API failure and manual retry.

Local Vitest execution is environment-blocked because this clean worktree has
no installed frontend dependencies (`vitest/config` is unavailable). Hosted CI
is the focused-test and production-build acceptance gate.

Static verification:

- PR #230 file-overlap audit: passed.
- `git diff --check`: passed.
- routing event schema: passed.

## Residual KEO-68 gates

This UI handoff does not close production billing. KEO-68 still requires
sandbox lifecycle proof, then human-only live charge/MFA/3DS, webhook
idempotency and replay evidence, entitlements, portal, payment failure and
recovery, cancellation/expiration, downgrade, refund, retained-data behavior,
and approved production products/prices/URLs/secrets.
