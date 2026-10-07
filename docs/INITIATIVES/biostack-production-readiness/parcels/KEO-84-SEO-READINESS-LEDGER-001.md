# Parcel: KEO-84-SEO-READINESS-LEDGER-001

## Goal

Make the live `biostack.cc` origin the single repository source for public SEO URLs, add bounded canonical/social metadata, and reconcile the launch ledger to current evidence without changing the NO-GO decision.

## Initiative

BioStack production readiness

## Project Track

Public website and release evidence

## Wave

Hardening

## Branch

`codex/keo84-seo-ledger-sparse-20260726`

## Worktree

`D:\Repos\BioStack-keo84-seo-ledger-sparse-024-20260726`

## Dependencies

- `main@53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`
- Hosted deployment run `30211140857`
- Hosted secret-scan run `30211140879`

## Integration Surfaces

- Public Next.js metadata -> deployed `biostack.cc` crawler/social surfaces
- Hosted workflow evidence -> launch-readiness ledger

## Security Gate

Public-claim and release evidence review required before merge. This parcel does not close any launch security gate.

## Allowed Files

- `frontend/src/lib/site.ts`
- `frontend/src/app/layout.tsx`
- `frontend/src/app/page.tsx`
- `frontend/src/app/start/page.tsx`
- `frontend/src/app/providers/page.tsx`
- `frontend/src/app/knowledge/layout.tsx`
- `frontend/src/app/how-it-works/page.tsx`
- `frontend/src/app/safety/page.tsx`
- `frontend/src/app/pricing/page.tsx`
- `frontend/src/app/faq/page.tsx`
- `frontend/src/app/robots.ts`
- `frontend/src/app/sitemap.ts`
- `frontend/src/app/tools/page.tsx`
- `frontend/src/app/tools/analyzer/page.tsx`
- `frontend/src/app/tools/reconstitution-calculator/page.tsx`
- `frontend/src/app/tools/volume-calculator/page.tsx`
- `frontend/src/app/tools/unit-converter/page.tsx`
- `frontend/src/__tests__/app/SeoMetadata.test.ts`
- `docs/launch-readiness-ledger.md`
- `docs/INITIATIVES/biostack-production-readiness/parcels/KEO-84-SEO-READINESS-LEDGER-001.md`
- `research/routing-events/keo-84-seo-readiness-ledger-20260726.json`

## Forbidden

- Package or lockfile changes
- Analytics, tracking, Search Console, DNS, or deployment changes
- Legal-policy or consent-copy changes
- Stripe, auth, database, Keon runtime, infrastructure, or public-content remediation
- Any change in `D:\Repos\BioStack`, Claude worktrees, the stalled KEO-84 worktree, or PR #230 files
- Commit, push, pull request, or production mutation

## Out of Scope

This parcel does not prove rendered production metadata, indexing, social-card rendering, accessibility, legal approval, billing acceptance, production auth, dependency remediation, backup/restore, Keon availability, or release readiness.

## Existing Patterns To Follow

- `frontend/src/app/layout.tsx` — Next.js metadata root.
- `frontend/src/app/robots.ts` and `frontend/src/app/sitemap.ts` — metadata route contracts.
- `docs/launch-readiness-ledger.md` — evidence-state vocabulary and NO-GO discipline.
- `research/routing-events/*.json` — canonical routing-event envelope.

## Contract

- `SITE_URL` is the only hard-coded public website origin in the changed frontend metadata source.
- `robots()` publishes `${SITE_URL.origin}/sitemap.xml`.
- Every sitemap URL resolves under `SITE_URL.origin`.
- Every public page listed in the sitemap receives a page-specific canonical, Open Graph URL/title/description/site name, and Twitter summary metadata.
- Root metadata supplies `metadataBase` and default social metadata.
- Legal placeholders remain `noindex,nofollow`.
- Readiness evidence distinguishes repository checks, hosted exact-SHA evidence, passive public observations, and unverified human/live gates.

## Required Tests

- `frontend/src/__tests__/app/SeoMetadata.test.ts`
- Exact expected sitemap route-set assertion, including `/tools/analyzer`.
- Table-driven import and metadata assertions for every sitemap page export.
- Static source assertion that no changed metadata source contains `biostack.app`.
- Static source assertion that metadata routes and page metadata consume `frontend/src/lib/site.ts`.
- JSON parse validation for the routing event.
- `git diff --check`

## Acceptance Criteria

- Robots and sitemap use `https://biostack.cc`.
- One site URL source drives metadata URL composition.
- Every sitemap route, including `/tools/analyzer`, uses page-specific canonical and social metadata.
- Focused tests encode the host and metadata contract.
- The ledger records current green deployment/secret-scan and consent UI evidence.
- The ledger remains NO-GO and retains current safety, legal, dependency, billing, auth, Keon, and operational blockers.
- No forbidden files or external systems are changed.

## Verification

- `rtk proxy node -e <bounded static assertions>`
- `rtk proxy powershell -NoProfile -Command "Get-Content ... | ConvertFrom-Json"`
- `rtk git diff --check`
- Hosted draft-PR CI must run the focused Vitest and production build before acceptance.

## Evidence Required

- This parcel document
- `research/routing-events/keo-84-seo-readiness-ledger-20260726.json`
- Reviewable Git diff
- Hosted CI result for the focused Vitest and production build

## Collision Risk

High. `frontend/src/app/layout.tsx`, metadata routes, and the launch ledger are shared integration files. Merge sequentially and rebase immediately before publication.

## PR Notes

- What changed: centralized the live site origin, corrected crawler URLs, added bounded canonical/social metadata and tests, and reconciled the launch ledger.
- Why: KEO-84 market-readiness evidence showed the deployed crawler contract used an obsolete host and the ledger described obsolete deployment/consent state.
- Risk: metadata inheritance and evidence wording require independent review; runtime tests remain pending hosted CI.
- Verification: review static assertions, JSON validation, diff integrity, then run focused Vitest and the production build in hosted CI.
- Evidence: this parcel and its routing event.

## Session Handoff

- Starting commit: `53ed0df5a0c207e99b6b3582d6c40b64e6b4f11c`
- Ending commit: uncommitted review-ready diff on `codex/keo84-seo-ledger-sparse-20260726`
- Files changed: only the Allowed Files listed above
- Commands run: Git/GitHub evidence inspection, attributed passive-source reconciliation, two bounded `npm ci` attempts, one read-only external Vitest attempt, bounded static assertions, JSON Schema validation, and diff checks
- Tests passed: static metadata-host/import assertions; routing-event JSON parse; `git diff --check`
- Tests failed: none
- Tests blocked: `SeoMetadata.test.ts` and production build; two clean `npm ci` attempts timed out after five minutes and the external cached Vitest attempt did not complete
- Decisions needed: none for the bounded implementation; hosted CI remains the acceptance gate
- Blockers: local dependency materialization on this workstation
- Next safe action: independent review, then draft-PR hosted CI for focused Vitest and production build
- Do not touch: lockfiles, PR #230, the stalled KEO-84 worktree, production, analytics, legal copy, Stripe, auth, data, Keon runtime, or infrastructure

## Stop-and-Report Rule

If review requires a new public route contract, analytics/tracking, legal copy, deployment, or another file outside Allowed Files, stop and request a parcel amendment.

## Independent Review Repair

The 2026-07-26 independent review found four bounded gaps. This revision:

- adds `/tools/analyzer` to the sitemap;
- gives every sitemap page page-specific metadata through `createPublicPageMetadata`, using a route layout for the client-only knowledge page;
- makes the SEO regression assert the exact route set and every page metadata export;
- labels passive production statements as attributed dated audit observations because no durable raw response artifact is attached.

The release decision remains **NO-GO**. Local runtime validation remains unavailable; hosted focused Vitest and the production build are required before acceptance.
