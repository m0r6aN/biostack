---
ticket: BIO-GOLIVE-003
title: Checkout creates the session for the plan the user chose
status: review-candidate
owner: clinton.morgan
created: 2026-10-14
updated: 2026-10-14
delivery_classes: [standard]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - frontend/src/lib/entitlements.tsx
  - frontend/src/app/pricing/pricing-client.tsx
  - frontend/src/components/monetization/UpgradeCard.tsx
  - frontend/src/__tests__/lib/entitlements.test.tsx
  - frontend/src/__tests__/app/pricing-client.test.tsx
routing_class: implementation/standard
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-GOLIVE-003 — Checkout creates the session for the plan the user chose

## Objective

`useEntitlements().startCheckout` always calls `createCheckoutSession('operator')`. The public pricing page uses that helper for Operator, and Commander is only a link. A caller can therefore start an Operator checkout while the chosen plan was Commander. Signed-in billing already passes the plan through a separate function. This parcel makes both paths create the Stripe Checkout session for the plan the user chose. Tests cover Operator and Commander. No live charge, refund, or cancellation is created.

## Initiative (Product Lane)

`biostack-go-live`, next-work item 3. Product contract prices stay monthly-only: Operator `monthlyPriceCents` 1200 and Commander `monthlyPriceCents` 2900. This parcel does not close the billing lifecycle gate. Refund, cancellation, renewal, and payment-failure downgrade remain human-owned and untested.

## Branch and Worktree

Proposed only. Gate 2 assigns both. No builder implements this spec while it is a review candidate.

- Branch: `fix/bio-golive-003-checkout-plan`
- Worktree: `D:/Repos/BioStack/.worktrees/bio-golive-003-checkout-plan`

## Dependencies

- `contracts/product-contract.v1.json` stays the price source. This parcel does not edit it. Frontend and backend mirrors stay untouched.
- `apiClient.createCheckoutSession` already accepts `'operator' | 'commander'`. No API change is required.
- Independent of BIO-GOLIVE-001 and BIO-GOLIVE-002. Separate worktree.

## Verified Facts

1. `frontend/src/lib/entitlements.tsx` declares `startCheckout: (returnPath?: string) => Promise<void>` and calls `createCheckoutSession('operator')`. The `returnPath` argument is unused.
2. `frontend/src/app/pricing/pricing-client.tsx` calls `startCheckout('/pricing')` from the Operator button only. Commander renders `tier.href`, which the product contract sets to `/billing?plan=commander`.
3. `frontend/src/components/monetization/UpgradeCard.tsx` calls `startCheckout(returnPath)`.
4. `frontend/src/app/billing/page.tsx` has its own `startCheckout(planCode)` and calls `createCheckoutSession(planCode)`. Its tests in `BillingPageIntent.test.tsx` already expect Operator. Those tests stay as they are.
5. The product contract marketing paths are `/billing?plan=operator` and `/billing?plan=commander`. Billing already consumes a `plan` query of `operator` or `commander`.
6. Launch ledger Lane 2 records one authorized $12 Operator charge on 2026-08-28. That charge is not evidence for this fix. This parcel forbids a new live charge.

## Surfaces

- `frontend/src/lib/entitlements.tsx` — `startCheckout` takes a plan code and passes it to `createCheckoutSession`.
- `frontend/src/app/pricing/pricing-client.tsx` — Operator and Commander buttons call that helper with the matching plan.
- `frontend/src/components/monetization/UpgradeCard.tsx` — pass `operator` as the plan and keep the return path behavior only if the helper still accepts it. The required behavior is the plan argument.
- New tests named in Tests.
- No backend file. No Stripe live-mode call.

## Contracts

- Paid plan codes remain the closed set `operator` and `commander`.
- `createCheckoutSession` is called with the plan the control named. Observer never calls it.
- Signed-in billing continues to call `apiClient.createCheckoutSession` with the plan it already resolved. This parcel does not replace that function.
- Prices, entitlements, and `pastDueGraceDays` stay as in `contracts/product-contract.v1.json`. This parcel does not change a price or a feature flag.
- Checkout still redirects the browser to the session URL returned by the API. Failure still shows the existing pricing error text.

## Acceptance Criteria

1. Public pricing Operator control calls `createCheckoutSession('operator')`.
2. Public pricing Commander control calls `createCheckoutSession('commander')`.
3. `UpgradeCard` calls `createCheckoutSession('operator')`.
4. Signed-in billing still calls `createCheckoutSession` with the plan argument it received, for both Operator and Commander.
5. An unknown plan string is not sent to `createCheckoutSession`.
6. Tests assert the plan argument. A test that only asserts that checkout was called does not satisfy this parcel.
7. No test, script, or manual step in this parcel creates a live Stripe charge, refund, or cancellation.
8. `npm test` in `frontend/` passes the new tests and the existing billing intent tests without weakening an assertion.

## Tests

1. `frontend/src/__tests__/lib/entitlements.test.tsx` renders a caller of `startCheckout('operator')` and `startCheckout('commander')` and asserts the mocked `createCheckoutSession` argument.
2. `frontend/src/__tests__/app/pricing-client.test.tsx` clicks the Operator and Commander controls and asserts the same two arguments.
3. Existing `frontend/src/__tests__/app/BillingPageIntent.test.tsx` stays unchanged and must still pass.
4. Focused command, from `frontend/`: `npm test -- entitlements.test.tsx pricing-client.test.tsx BillingPageIntent.test.tsx`.
5. Proof of the paid path stops at the mocked session call. The existing test-mode billing runbook is the only later path for a human Stripe test, and it is outside this parcel.

## Rollback

Revert the parcel commit. Public checkout returns to the Operator-only helper. No database or Stripe object is created by this parcel, so rollback has no payment object to reconcile.

## Out of Scope

- Refund, cancellation, renewal, webhook retry, and payment-failure downgrade.
- Price changes, annual plans, clinic tiers, and entitlement limits.
- Editing `contracts/product-contract.v1.json` or either mirror.
- A live or test-mode Stripe call by the builder.
- Copy changes on the pricing page beyond the control that starts checkout.

## Allowed Files

- `frontend/src/lib/entitlements.tsx`
- `frontend/src/app/pricing/pricing-client.tsx`
- `frontend/src/components/monetization/UpgradeCard.tsx`
- `frontend/src/__tests__/lib/entitlements.test.tsx`
- `frontend/src/__tests__/app/pricing-client.test.tsx`

Coordinator-owned: this spec, `docs/specs/INDEX.md`, and go-live status files.

## Forbidden

- Any Stripe secret, live charge, refund, cancellation, or customer portal mutation.
- Weakening `BillingPageIntent.test.tsx` or deleting its Operator assertion.
- Changing billing webhook code.
- Treating a green unit test as a closed billing lifecycle gate.
