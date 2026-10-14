# BioStack go-live status

Initiative: `biostack-go-live`
Coordinator session: 2026-10-14 resume 2
Repository pin: `dev@6c9d8eccfd69f848d69c223e547c4d40dbf6ccee`
Decision: **NO-GO / HOLD**
Resume evidence: `SESSION-2026-10-14-resume-2.md`
This record does not change `docs/INITIATIVES/biostack-production-readiness/` and does not authorize production release.

## What the latest session did

Shaped three review-candidate specs and registered them in `docs/specs/INDEX.md`. The spec linter returned `valid` for each. No builder was dispatched. No product code was implemented. A read-only probe on 2026-10-14 still returned `classification: Unknown` for Glycine and Retatrutide, and HTTP 200 for production OpenAPI and Scalar. The first Glycine request was canceled at 20 seconds. The retry returned 200 in 250 ms. Detail: `SESSION-2026-10-14-resume-2.md`.

The earlier session inspected the charter, registry, production-readiness handoff, launch ledger, seed, product contract, and public site, and assigned three independent read-only reviewers. No production database was written. No money was spent. No legal text was published as approved.

## Decision

Do not declare launch. Six launch gates fail or are unverified. The public site answers HTTP 200 on `/`, `/pricing`, and `/knowledge`. That is availability, not readiness.

The long-term valuation objective of at least $500,000,000 is recorded here as a strategic aim. It is not a current claim. No repository artifact supports it. The only written commercial milestones are in `docs/plans/OPERATING-PLAN-2026-08-27.md`: close the launch ledger, grow evidence pages from 150 to 400, reach 50,000 organic sessions per month, then 2% free-to-paid, under 5% monthly churn, and $50,000 MRR. Those milestones are unmet.

## Launch gates

| Gate | Threshold | Status | Evidence |
|---|---|---|---|
| Premium experience and core journeys | Representative users can understand value, find information, and finish core tasks | **Fail** | Independent experience review, 2026-10-14. Public GET passes. Search, checkout, account, and billing completion do not. |
| Library of at least 150 qualifying entries | Distinct, substantive, schema-complete, processed, independently reviewed, and visible on the public site | **Pass for catalog size. Fail for reviewed class labels.** | Live Refresh created 106 and updated 52. Warm list returned 158 names in 334 ms. Resume 2 probe: Glycine and Retatrutide API `classification` are still `Unknown`. Stored class is an integer, so a mapper deploy alone cannot repair existing rows. See `SESSION-2026-10-14-resume-2.md`. |
| Paid services work as promised | Checkout, entitlement, cancel, failure, and recovery match the contract | **Fail** | Public checkout plan selection is fixed in the security-checkout worktree and covered by tests. It is not deployed. Refund, cancel, renewal, and failure downgrade are not tested. |
| Trust and legal review complete | Approved terms, privacy, disclaimers, and subscription policy for the launch jurisdictions | **Fail** | `/terms` and `/privacy` say legal review is required and are `noindex`. No counsel approval is on record. |
| Monitoring, support, recovery, spending controls | Owners, alerts, backup/restore drill, rehearsed rollback, and a budget | **Fail** | Runbooks exist. Drills, alert delivery, support SLA, and an approved acquisition budget do not. |
| No open critical or high defects | Independent review, source checks, and re-verification of fixes | **Fail until deploy and independent re-check** | Source fixes for F-001, F-002, F-003, F-004, F-006, and F-007 are in `.worktrees/bio-golive-security-checkout` and are not deployed. F-005 stays open. A reviewer who did not write the fix has not re-checked them. |

## Library census

Updated 2026-10-14 after agent review. Detail: `library/CENSUS-2026-10-14.md`.

| Measure | Count |
|---|---|
| Distinct canonical IDs | 158 |
| Duplicate IDs | 0 |
| Mission-qualifying | 158 |
| Worker schema valid | 158 |
| Worker seed acceptance test | passed |
| Written to production `KnowledgeEntries` | 158 distinct names after Refresh on 2026-10-14. See `library/PUBLISH-2026-10-14.md`. |

Qualifying means all of the following: identity, classification, purpose, evidence quality, known risks, interactions, uncertainty, sources, review date, and regulatory status; not a draft; not inactive; not marked partial; and not carrying the launch-seed disclaimer that the record is intentionally conservative. Drafts lack purpose and review date. Every record's claim sources are internal `src:` tokens rather than a reviewed source set that an independent reviewer has checked. Aliases, placeholders, and duplicates are not counted. A later seed expansion that only adds stubs does not close this gate.

The public API list observed by the experience reviewer returned 52 compounds. A direct `GET https://biostack.cc/api/v1/knowledge/compounds/bpc-157` returned HTTP 200 with a conservative educational record, Limited evidence, and a note that regulated fields are limited. That one response is not a 150-entry proof, and it was not re-checked for every withheld dose field in this session.

The 2026-10-14 Refresh already ran. Do not run it again unless a later seed change requires another dry run. See `HANDOFF-2026-10-14.md`.

Resume probe on 2026-10-14, read-only: the compound list returned HTTP 200, 158 items, 185,797 bytes, in 334 ms after the API was warm. A first Glycine request in that session was canceled at 25 seconds. Resume 2 canceled a first Glycine request at 20 seconds and then received HTTP 200 in 250 ms with `classification: Unknown`. Retatrutide returned HTTP 200 in 9,856 ms with `classification: Unknown`. F-001 remains open: OpenAPI returned HTTP 200 and 92,651 bytes. Scalar returned HTTP 200.

## Product and paid-service facts

Product contract `contracts/product-contract.v1.json` (effective 2026-07-13), mirrored in frontend and backend:

- Observer: $0, active-compound limit 8.
- Operator: $12/month (`monthlyPriceCents` 1200).
- Commander: $29/month (`monthlyPriceCents` 2900).
- Paid access statuses: Active, Trialing. `pastDueGraceDays` is 0.

Live `GET /pricing` returned HTTP 200 and the experience reviewer saw `$0`, `$12/mo`, and `$29/mo` in the HTML.

Checkout defect, confirmed in source: `frontend/src/lib/entitlements.tsx` `startCheckout` always calls `createCheckoutSession('operator')` and ignores `returnPath`. `frontend/src/app/pricing/pricing-client.tsx` uses that helper for Operator and a link for Commander. Signed-in billing has a separate `startCheckout(plan)` that does pass the plan. The public pricing path can sell the wrong plan. That is a paid-promise failure.

Older docs still disagree with the contract: `docs/billing/tier-enforcement.md` and `docs/commercialization/01-pricing-and-packaging.md` describe a 5-compound cap, annual prices, and clinic tiers. The April commercialization overlay also claims better outcomes. Current home copy in `frontend/src/app/page.tsx` says BioStack does not prescribe, diagnose, recommend compounds, or replace care. Keep the home boundary. Do not publish the older outcome claims.

Launch ledger Lane 2 records one authorized $12 Operator charge on 2026-08-28 (invoice `56D1KVXI-0001`). Refund, cancellation, renewal, and payment-failure downgrade were not exercised. That partial charge is not a closed billing gate.

## Trust, operations, and money

- Terms and privacy routes state that copy is not final. Robots exclude them. Counsel has not approved them.
- No approved acquisition budget exists. KEO-86 holds paid acquisition at $0. Later dollar bands in that plan are not authorization.
- No infrastructure, model, content-processing, or acquisition cost baseline is recorded. List prices are not costs.
- Backup, monitoring, and rollback procedures are drafted in `docs/operations/production-operations-runbook.md`. The launch ledger marks restore drill, alert delivery, and rollback rehearsal as not established.
- Support ownership and an SLA are not staffed. `mailto:support@biostack.cc` is a footer link, not an operating support system.
- Governed delivery stops implementation without an approved active spec, a coordinator-named branch, and an isolated worktree. P4 is still a review candidate. P0, P8, P9, production deployment, and legal-policy effectiveness are human-gated. This initiative does not waive that.

## Owners and next actions

| Work | Owner | Depends on | Acceptance | Next safe action |
|---|---|---|---|---|
| Public class labels and list read | Builder on BIO-GOLIVE-001 after Gate 2 | Two independent spec reviews, then a named worktree | Public Glycine `classification` is `AminoAcid` and Retatrutide is `ResearchCompound` after deploy and a human row repair; cold list finishes inside a page load | Spec is a review candidate. Do not dispatch. Do not Refresh. |
| Pricing checkout plan selection | Coordinator implementation in `.worktrees/bio-golive-security-checkout` | Independent reviewer who did not write the fix | Anonymous and signed-in paths create the session for the plan the user chose; tests cover Operator and Commander | Tests passed in the worktree. Do not create a live charge. Deploy remains human. |
| Billing lifecycle | Billing owner (Clint for live Stripe) | Test-mode or explicitly authorized live cycle | Checkout, webhook, cancel, refund, and payment-failure downgrade evidenced | Follow `KEO-68` in test mode. Do not create new live charges from this session. |
| Legal effectiveness | Counsel plus product owner | Final product behavior and jurisdictions | Dated approval of terms, privacy, disclaimer, and subscription policy; pages no longer say review is required | Escalate. Agents must not mark policy effective. |
| Security clearance | Coordinator implementation in `.worktrees/bio-golive-security-checkout`, then a reviewer who did not write the fix | Human production deploy | Production OpenAPI/Scalar return 404; auth rate limit uses trusted client identity; lead capture is validated and rate-limited; F-005 stays open | Source fixes are uncommitted in the worktree. Production still serves OpenAPI. |
| Operations | Platform owner | Production access Clint controls | Restore drill receipt, alert delivery, rehearsed rollback, named support owner | Documented commands are not drills. Clint executes anything that touches production. |
| Acquisition | Marketing owner | Budget decision | A written budget and a measurement plan for qualified traffic, activation, paid conversion, retention, and feedback | Continue $0 acquisition. Present costed options before any spend. |

## Escalations (human)

1. Legal: no qualified professional has approved terms, privacy, or subscription policy. Launch jurisdictions are not named in an approval record.
2. Money: no acquisition or operating budget is approved. Any new spend needs a decision.
3. Production: Refresh, deploy, secret rotation, backup drills, and live billing changes are Clint-only. This session will not perform them.
4. Release verdict: only the production-readiness release owner may change that initiative's NO-GO. This file does not.

## Costs this session

New financial commitment: **$0**. Reviewers were local read-only agents. No Stripe call. No ad spend. No model-provider invoice was created by this coordinator.

## Evidence index

- `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`
- `docs/INITIATIVES/biostack-governed-delivery/PLAN-REVIEW.md` (plan review closed; does not authorize production)
- `docs/specs/INDEX.md` (P4 review-candidate; no go-live parcel approved)
- `docs/INITIATIVES/biostack-production-readiness/FINAL-HANDOFF.md` and `RELEASE-GATES.md` (NO-GO / HOLD)
- `docs/launch-readiness-ledger.md` (NO-GO, reconciled through 2026-09-12)
- `backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` (99 records)
- `contracts/product-contract.v1.json`
- `frontend/src/lib/entitlements.tsx` (Operator-only checkout helper)
- `frontend/src/app/terms/page.tsx`, `frontend/src/app/privacy/page.tsx`
- Passive GETs on 2026-10-14: `/` 200, `/pricing` 200, `/knowledge` 200, `/api/v1/knowledge/compounds/bpc-157` 200
- Independent experience review, commercial census, and security review returned in this session.
- `docs/INITIATIVES/biostack-go-live/reviews/SECURITY-REVIEW-2026-10-14.md`
- `docs/specs/active/BIO-GOLIVE-001-public-classification-and-list-read.md`
- `docs/specs/active/BIO-GOLIVE-002-production-exposure-and-intake-limits.md`
- `docs/specs/active/BIO-GOLIVE-003-checkout-plan-selection.md`
- `docs/INITIATIVES/biostack-go-live/SESSION-2026-10-14-resume-2.md`
