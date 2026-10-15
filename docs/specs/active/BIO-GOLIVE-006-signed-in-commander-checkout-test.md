---
ticket: BIO-GOLIVE-006
title: Assert signed-in Commander checkout uses the Commander plan
status: active
owner: clinton.morgan
created: 2026-10-14
updated: 2026-10-14
delivery_classes: [standard]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - frontend/src/__tests__/app/BillingPageIntent.test.tsx
routing_class: implementation
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-GOLIVE-006 — Signed-in Commander checkout test

## Objective

PR #544 made `startCheckout` pass the selected plan. The public pricing tests cover Operator and Commander. `BillingPageIntent.test.tsx` asserts a successful Operator session and only preserves `plan=commander` when authentication fails. This parcel adds the missing successful signed-in Commander assertion. It does not create a Stripe session.

## Initiative (Product Lane)

`biostack-go-live`. This parcel does not close the billing lifecycle gate. Refund, cancellation, renewal, and payment-failure downgrade stay untested.

## Branch and Worktree

Proposed only. Gate 2 assigns both.

- Branch: `test/bio-golive-006-commander-checkout`
- Worktree: `D:/Repos/BioStack/.worktrees/bio-golive-006-commander-checkout`

## Dependencies

- The production source already passes `plan` from the signed-in billing page. This parcel adds a test only. If the test fails because the page does not send `commander`, the builder stops and reports that. The builder does not change product code under this spec.

## Verified Facts

1. `frontend/src/app/billing/page.tsx` sends the plan code through its checkout function.
2. `BillingPageIntent.test.tsx` asserts Operator success. It does not assert that a signed-in Commander action calls `createCheckoutSession` with `commander`.
3. The security re-check ran `npm test` on this file with the Operator tests passing.

## Surfaces

- Add one test to `BillingPageIntent.test.tsx`. A signed-in user who chooses Commander causes `createCheckoutSession` to be called with `commander`. Follow the existing Operator success test's setup. Do not weaken the Operator test.

## Contracts

- Product prices stay Operator 1200 and Commander 2900 monthly cents.
- No live Stripe call. The test uses the existing mock.

## Acceptance Criteria

1. The new test fails if Commander checkout calls `createCheckoutSession` with `operator` or with no plan.
2. The existing Operator success test still passes.
3. Command from `frontend`: `npm test -- src/__tests__/app/BillingPageIntent.test.tsx` exits 0.

## Tests

The acceptance command is the test.

## Rollback

Revert the test commit. No product behavior changes.

## Out of Scope

- Checkout implementation changes.
- Live charges, webhooks, refunds, and cancellation.
- Pricing copy.

## Allowed Files

- `frontend/src/__tests__/app/BillingPageIntent.test.tsx`

## Forbidden

- Editing `billing/page.tsx`, `entitlements.tsx`, or `pricing-client.tsx`.
- A live Stripe call or a secret print.
- Skipping or deleting the Operator test.
