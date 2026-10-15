# BIO-GOLIVE-006 spec review

- Reviewer: spec-review-1
- Date: 2026-10-14
- Spec: `docs/specs/active/BIO-GOLIVE-006-signed-in-commander-checkout-test.md`
- Spec git blob: `f4130cf25d206d42d53b6dd7a7660c8f44177257` (content hash; file is untracked)
- Tree: HEAD `5b686e69536f2eedf0195ee77466f20b08a490d8`. `frontend/src/app/billing/page.tsx`, `frontend/src/__tests__/app/BillingPageIntent.test.tsx`, `frontend/src/lib/api.ts`, and `contracts/product-contract.v1.json` are clean.
- Verdict: **PASS**

One new test in the existing billing-intent file can assert `createCheckoutSession('commander')`. The signed-in session-start path already sends `commander`. The spec correctly forbids product edits. The stop rule is not the outcome on this tree. It is the right outcome if a later tree stops sending `commander`. This review does not authorize go-live, a live charge, or a product edit.

## Decisions

| Question | Result |
| --- | --- |
| Can one new test assert `createCheckoutSession('commander')` with the existing setup? | Yes. |
| Does the spec correctly forbid product edits? | Yes. The page already sends `commander`, so a product edit is not required. |
| Does the page send `commander` on the signed-in success path? | Yes. The stop rule therefore does not fire. |
| If it did not, is the stop rule the right outcome? | Yes. Report the failure and do not edit product code under this spec. |

## Definition of Done

| Item | Status |
| --- | --- |
| Add one test; a signed-in Commander choice calls `createCheckoutSession` with `commander` | **Pass** — implementable from the current test setup. Not implemented in this review. |
| New test fails if that call is `operator` or has no plan | **Pass** — `toHaveBeenCalledWith('commander')`, already used for Operator, fails both ways and also fails if the call is absent. |
| Do not weaken or delete the Operator success assertion | **Pass** — required by Surfaces, AC2, and Forbidden. |
| Focused command from `frontend` exits 0 | **Pass** as the acceptance command. Same command shape as the PR #544 re-check. Not re-run here; the new test does not exist yet. |
| No live Stripe session, charge, or secret | **Pass** — mock only. |
| Prices stay Operator 1200 and Commander 2900 monthly cents | **Pass** — those cents are in the product contract. This parcel cannot edit that file. |
| Product files stay unchanged, including `billing/page.tsx` | **Pass** — Allowed Files is only the test. Forbidden names the page and the other checkout callers. |
| Stop if the page does not send `commander` | **Pass** as a contingency. Not the expected result on this HEAD. |
| Billing lifecycle gate stays open | **Pass** — refund, cancellation, renewal, and payment-failure downgrade stay out of scope. |

## Findings

| id | severity | location | finding |
| --- | --- | --- | --- |
| N1 | nit | Surfaces (line 49); tests at `BillingPageIntent.test.tsx` lines 76–99 and 159–176 | "The existing Operator success test" matches two tests. Both use the default Observer subscription and the existing `createCheckoutSession` mock. Either can be copied. No spec change is required. |

No blocker or major findings.

### N1 — two Operator setups, either is enough

`consumes a valid plan once under Strict Mode` starts from `/billing?plan=operator`, leaves `createCheckoutSession` pending, and expects `toHaveBeenCalledWith('operator')`. That is the signed-in counterpart of the current `plan=commander` test, which only covers authentication failure.

`creates one session when the manual plan control is clicked rapidly` clicks `Upgrade to Operator` and expects the same argument. The Commander control is already addressed in this file as `Upgrade to Commander`.

Optional, not a required rewrite: copy the query test and change the plan to `commander`, or copy the click test onto `Upgrade to Commander`. Keep the pending-promise mock from those tests so the page does not assign `window.location.href`. Assert `toHaveBeenCalledWith('commander')`. Leave both Operator assertions in place.

## Verified facts

- Signed-in session start sends the selected plan. `startCheckout` takes `planCode: 'operator' | 'commander'` and calls `apiClient.createCheckoutSession(planCode)` (`billing/page.tsx` lines 75–96).
- The Commander button calls `startCheckout('commander')` (line 255). It is hidden only when the subscription tier is already Commander (line 249). The test default is Observer, so the button is present.
- A resolved subscription with `?plan=commander` calls `startCheckout(planIntent, true)` (lines 107–118). `planIntent` is the query string `commander`. There is no remap to `operator`.
- The `?checkout=success` banner (lines 141–147) does not start a session and does not send a plan. That return banner is not the success path this spec tests.
- Authentication failure never reaches checkout. `load` catches the rejection, `subscription` stays null, and the plan effect returns. The existing test at lines 101–113 correctly expects no `createCheckoutSession` call and preserves `?plan=commander`.
- The test file already mocks `createCheckoutSession`, resets history to `/billing`, and resolves an Observer subscription in `beforeEach`. No new mock, fixture, or helper is required.
- `apiClient.createCheckoutSession` posts `{ planCode }` (`api.ts` lines 486–490). This parcel's mock assertion is the right seam. It does not need an HTTP test or an `api.ts` edit.
- Product contract monthly cents are Operator `1200` and Commander `2900` (`contracts/product-contract.v1.json` lines 27 and 36). The frontend mirror matches. Pricing copy is out of scope.
- Billing `startCheckout` has passed `planCode` since `fa86b996` (PR #238). The Commander button literal dates to `298c45f9`. PR #544 (`b3cdde20`) is the later public checkout change named in the objective. That history does not open a product edit.
- The security re-check on this HEAD recorded Operator success in this file and the missing signed-in Commander assertion. This review did not re-run those tests.
- `validate-spec.ps1` returned `valid` for shape `ticket-spec`. Its `standingAuthorizationEligible` flag is a fold result. It is not dispatch authority and not a go-live approval. The spec still leaves the branch and worktree to Gate 2.

## Commands

- `rtk` is not on PATH. Git ran directly.
- `git rev-parse HEAD` → `5b686e69536f2eedf0195ee77466f20b08a490d8`
- `git hash-object docs/specs/active/BIO-GOLIVE-006-signed-in-commander-checkout-test.md` → `f4130cf25d206d42d53b6dd7a7660c8f44177257`
- `git status --short` for the spec, billing page, billing-intent test, `api.ts`, and the product contract → `?? docs/specs/active/BIO-GOLIVE-006-signed-in-commander-checkout-test.md` only.
- `pwsh -NoProfile -File docs/specs/scripts/validate-spec.ps1 -SpecPath docs/specs/active/BIO-GOLIVE-006-signed-in-commander-checkout-test.md -RepoRoot D:\Repos\BioStack` exited 0. Result: `shape` `ticket-spec`, `result` `valid`, `reason` null, `reviewerCount` 1.

## Fix checklist

None. No blocker or major finding.
