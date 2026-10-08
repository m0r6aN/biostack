---
ticket: BIO-FE-002
title: Onboarding route consolidation — /start canonical, /map and /onboarding permanent redirects
status: review-candidate
revision: 1
owner: clinton.morgan
created: 2026-10-08
updated: 2026-10-08
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: []
substance_function_risk: []
function_review_status: not-applicable
surfaces:
  - frontend/src/app/map/page.tsx
  - frontend/src/app/onboarding/page.tsx
  - frontend/next.config.ts
  - frontend/src/contracts/product-contract.v1.json
  - frontend/src/__tests__/app/start/page.test.tsx
  - frontend/src/__tests__/lib/productContract.test.ts
  - frontend/src/__tests__/routing/legacy-onboarding-redirects.test.ts
  - docs/specs/EVIDENCE-BIO-FE-002.md
routing_class: implementation/standard
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-FE-002 — Onboarding route consolidation

## Intent

Close the remaining item from `BIOSTACK_FRONTEND_READINESS_AUDIT.md` (§2 item 6, §4 "Route & IA Inventory", §13, §14 item 9, §17 P1): three near-duplicate onboarding doors (`/start`, `/map`, `/onboarding`) persist in the route inventory. Per owner ruling D-F(1) (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`, "D-F — Owner rulings (2026-10-08, all 'as recommended')", item 1): **`/start` becomes canonical; `/map` and `/onboarding` become 301 redirects with a mode toggle inside the canonical experience.** This parcel implements that ruling exactly, with one documented interpretation (see Constraints, "301 vs 308") and one documented regression fix the audit did not know about (see "Verified Facts").

**Classification rationale:** This is pure routing/presentation consolidation. No claim, numeric, dose, evidence-tier, or recommendation surface is created, changed, or removed — the `OnboardingExperience` component and the data it renders are untouched; only which URL reaches it, and with what initial mode, changes. `delivery_classes: [standard]`. `guidance_classes` and `substance_function_risk` are empty — no curated-evidence or substance-risk surface is touched. `function_review_status: not-applicable` — there is no new user-facing function, intended use, automation, output, or claim; this parcel changes routing plumbing around an existing, already-reviewed-by-omission marketing funnel, with no new claim surface introduced.

## Goal

Make `/start` the single, canonical onboarding entry point. `/map` and `/onboarding` permanently redirect to `/start`, reproducing the mode state each legacy route used to represent (so the "existing user" and "new user" funnel entry points are not lost), with no 404s, no orphaned metadata, and no change to the `OnboardingExperience` component itself.

## Initiative (Product Lane)

Frontend readiness polish — this is NOT a governed-delivery P-series parcel and does NOT change the existing production-readiness verdict (`BIOSTACK_FRONTEND_READINESS_AUDIT.md` §1). It closes a P1 frontend-readiness backlog item (§17) explicitly unblocked by owner ruling D-F(1).

## Wave

Wave 1 — frontend polish, same wave as BIO-FE-001 (homepage proof panel), following `codex/production-readiness` (PR #181, 2026-07-11).

## Branch and Worktree

**Proposed:**
- Branch: `feat/bio-fe-002-route-consolidation`
- Worktree: `/home/cmorgan76/Repos/biostack-wt/bio-fe-002`

Coordinator creates both at Gate 2 approval.

## Dependencies

- None blocking. `/start` already exists as the canonical route rendering `OnboardingExperience` with `mode` resolved from `?mode=` (confirmed below). No backend, schema, or API change is required.
- Soft dependency: none on BIO-FE-001 (different files; both may proceed independently — see Collision Risk).

## Verified Facts (shaper, this session, main @ 1cfb827)

1. **`/start`** (`frontend/src/app/start/page.tsx`) is already the canonical route: it renders `MarketingNav` + `OnboardingExperience` + `MarketingFooter`, resolves `mode` from `searchParams.mode` (`'existing'` if the param equals `'existing'`, else `'new'`), and sets public SEO metadata via `createPublicPageMetadata({ path: '/start', ... })` (canonical tag already correct, already indexed in `sitemap.ts` and `robots.ts`).
2. **`/onboarding`** (`frontend/src/app/onboarding/page.tsx`) currently redirects to `/start` using `redirect(canonicalRoutes.onboarding)` (`next/navigation`'s `redirect()`, which issues a **307 Temporary Redirect**, not permanent) — and **does not forward the `mode` query param**: the existing frozen test (`frontend/src/__tests__/app/start/page.test.tsx`, `describe('/onboarding redirect')`) explicitly asserts `redirect` is called with the literal string `'/start'` even when `searchParams.mode === 'existing'` ("redirects to /start (not preserving mode) for an unrecognized mode value" / "redirects to /start when legacy mode is provided"). This is a **funnel-state-loss bug**, not intentional design — see Judgment Call 1.
3. **`/map`** (`frontend/src/app/map/page.tsx`) currently redirects to `canonicalRoutes.analyzer` (`/tools/analyzer`) via `redirect()` (307, temporary) — **not to `/start` at all**. Git history shows this is a merge regression: commit `7e7526b` (2026-07-06, "collapse /start, /map, /onboarding into one canonical route") correctly set `/map` to `redirect('/start?mode=existing')`, preserving the "existing user" entry state that `/map` originally represented (`frontend/src/app/map/page.tsx` pre-collapse rendered `OnboardingExperience mode="existing"`, commit `10cacb0`). A parallel branch (commit `434884a`, "integrate Kompress...", 2026-07-10) independently changed `/map` to redirect to `/tools/analyzer`. Neither commit is an ancestor of the other (`git merge-base --is-ancestor` confirms both orderings false); when both landed on `main`, the Kompress-branch version of `/map/page.tsx` won, silently reverting the mode-preservation fix. This is the regression the audit observed ("`/map` ... Still duplicates `/start`") without knowing its cause. The still-live internal link `frontend/src/components/tools/ToolsDecisionSurface.tsx:515` (`<Link href="/start?mode=existing">Already have a stack? Open map</Link>`) confirms `/start?mode=existing` is the product's intended "map" entry state today, independent of the `/map` URL itself.
4. **`/map-my-stack`** (`frontend/src/app/map-my-stack/page.tsx`) is an unrelated, **authenticated, paid-upgrade-funnel route** ("Premium stack funnel" — paste-and-map with `UpgradeCard`/`LockedInsightCard` gating via `useEntitlements`), listed in `frontend/src/lib/appRoutes.ts`'s `PROTECTED_ROUTE_PREFIXES`. It is not part of the audit's three-route onboarding duplication and is not touched by this parcel (see Out of Scope and Forbidden).
5. `frontend/src/app/onboarding/consent/page.tsx` is a distinct, authenticated consent-capture route at `/onboarding/consent` (not `/onboarding`). It is unaffected by this parcel: the redirect source for the legacy `/onboarding` route must match the exact path `/onboarding` only, not a prefix or wildcard, or `/onboarding/consent` would break.
6. `frontend/next.config.ts` already has a working precedent for a config-level permanent redirect: `/calculators` → `/tools` via `async redirects() { return [{ source: '/calculators', destination: '/tools', permanent: true }] }`. Next.js's `permanent: true` emits HTTP **308** (Permanent Redirect), not 301 — see Judgment Call 2. `frontend/src/middleware.ts` also independently 308-redirects `/calculators` (pre-existing duplication, not introduced by this parcel, not touched here).
7. `frontend/src/contracts/product-contract.v1.json` (`routes.aliases`) currently records `"/onboarding": "/start"` (already correct) and `"/map": "/tools/analyzer"` (reflects the regression in Fact 3, must be corrected to `"/start"`). `routes.publicPrefixes` already includes `/start`, `/map`, and `/onboarding` (all three stay public; no auth-wall change needed).
8. `frontend/src/app/sitemap.ts` already lists only `/start` in `STATIC_PATHS` — `/map` and `/onboarding` are not and have never been listed. No sitemap change needed.
9. `frontend/src/app/robots.ts` already allows only `/start` (not `/map`/`/onboarding`) in its `allow` list, and does not list either in `disallow` (they are simply absent, which is correct for routes that will permanently redirect). No robots change needed.
10. Neither `frontend/src/app/map/page.tsx` nor `frontend/src/app/onboarding/page.tsx` currently exports `metadata` or calls `createPublicPageMetadata`. There is no orphaned metadata to remove.
11. Next.js's routing order applies `next.config.ts` `redirects()` **before** `middleware.ts` and before filesystem page resolution. A config-level redirect for `/map` and `/onboarding` fires before the app-router page components would ever render, making the current `frontend/src/app/map/page.tsx` and `frontend/src/app/onboarding/page.tsx` the correct place to **remove** (not merely edit) once the config-level redirect exists, to avoid dead, misleading page files.
12. No other test file references `/map`'s or `/onboarding`'s redirect destination except `frontend/src/__tests__/app/start/page.test.tsx` (component-level, asserts destinations) and `frontend/src/__tests__/lib/productContract.test.ts` (asserts `routeAliases['/map']).toBe(canonicalRoutes.analyzer)` — both must be updated to reflect the corrected behavior this spec requires; see Required Tests. `frontend/src/__tests__/middleware.public-routes.test.ts` only asserts `/map` and `/onboarding` remain non-401/307-to-signin (i.e., stay public) — that assertion remains true and needs no change.
13. There is no E2E/Playwright harness wired into the frontend test suite (`frontend/vitest.config.ts` is the only configured runner; `.playwright-cli`/`output/playwright` are unrelated CLI artifacts, not a test suite). Runtime verification of actual redirect status codes and query-forwarding behavior therefore requires a manual check against a local dev or production build server (see Verification), not a unit test alone.
14. No UTM or analytics tracking parameters exist anywhere in the current `/start`, `/map`, or `/onboarding` code paths (confirmed via grep); the audit's "Analytics & Instrumentation Recommendations" section is explicitly "for later — do not implement" and does not mention onboarding-route tracking. There is no analytics-continuity obligation beyond generic query-string passthrough (Constraint below).

## Constraints

### Scope and mechanism
- The only sanctioned redirect mechanism for this parcel is `frontend/next.config.ts`'s `async redirects()` array, using `permanent: true`, matching the existing `/calculators` precedent (Fact 6). Do not introduce a second redirect mechanism (e.g., a new middleware branch) for `/map`/`/onboarding`.
- `frontend/src/app/map/page.tsx` and `frontend/src/app/onboarding/page.tsx` are deleted, not edited, once the config-level redirect exists (Fact 11). `frontend/src/app/onboarding/consent/page.tsx` is untouched.
- The `/onboarding` redirect `source` must be the exact literal path `/onboarding` (no wildcard, no `:path*`), so `/onboarding/consent` is never matched or affected (Fact 5). This is a hard requirement, not a preference.

### Mode-state preservation (no funnel behavior lost)
- `/map` → `/start?mode=existing` (restores the pre-regression behavior of Fact 3; `/map` historically meant "I already have a stack").
- `/onboarding` (no query) → `/start` (mode resolves to `new`, matching `/start`'s own default).
- `/onboarding?mode=existing` → `/start?mode=existing` (the query param must pass through; Next.js config redirects forward source query parameters that are not referenced in the destination path automatically — this is a framework-level behavior assumption that must be proven live, not just asserted in a unit test; see Verification).
- `/onboarding?mode=<anything else>` → `/start?mode=<anything else>` (pass the value through unchanged; `/start`'s own existing normalization logic, already tested, resolves any unrecognized value to `new` — this parcel does not duplicate that normalization in the redirect layer).
- The "mode toggle inside the canonical experience" referenced in D-F(1) is the already-existing `?mode=` query-driven behavior of `OnboardingExperience`/`StartPage` (Fact 1). This parcel does not add a new interactive UI toggle control inside the rendered page — see Out of Scope.

### SEO
- `/start`'s existing canonical tag, sitemap entry, and robots allow-listing are correct and unchanged (Facts 1, 8, 9) — do not modify `frontend/src/app/sitemap.ts` or `frontend/src/app/robots.ts`.
- No `metadata` export or `createPublicPageMetadata` call is added to `/map` or `/onboarding` — they must never render a page; a `metadata` export on a route that config-redirects before rendering would be dead code.
- `frontend/src/contracts/product-contract.v1.json`'s `routes.publicPrefixes` list stays unchanged (still includes `/start`, `/map`, `/onboarding`) — this is now functionally redundant for `/map`/`/onboarding` since config redirects fire before middleware (Fact 11), but removing entries carries real risk (a future config change could re-expose the page-resolution path) for zero benefit. Leaving them is the conservative, in-scope choice; removing them is explicitly out of scope.

### Excluded routes
- `/map-my-stack` (Fact 4) is never touched, read, or referenced by any change in this parcel. It is a distinct, authenticated, paid-upgrade route, not part of the `/start`/`/map`/`/onboarding` consolidation.

## Acceptance Criteria

1. **AC1 — `/map` redirects correctly:** A request to `/map` (no query) returns a Next.js permanent redirect (308, `permanent: true`) to `/start?mode=existing`.
2. **AC2 — `/onboarding` redirects correctly, mode preserved:**
   - `/onboarding` (no query) → 308 → `/start`.
   - `/onboarding?mode=existing` → 308 → `/start` with `mode=existing` present in the resulting query string (live-verified; see Verification).
   - `/onboarding?mode=anything-else` → 308 → `/start` with `mode=anything-else` passed through unchanged (live-verified).
3. **AC3 — `/onboarding/consent` is unaffected:** `/onboarding/consent` and `/onboarding/consent?returnTo=...` continue to render the existing consent page (no redirect, no 404).
4. **AC4 — No 404s from legacy paths:** `/map` and `/onboarding` (with or without query params) never return a 404; they always redirect.
5. **AC5 — `/start` canonical tag unchanged and correct:** `createPublicPageMetadata({ path: '/start', ... })` output still resolves `alternates.canonical` to `/start` (no page ever sets canonical to `/map` or `/onboarding`).
6. **AC6 — No orphaned metadata:** Neither `/map` nor `/onboarding` has a `metadata` export, a `generateMetadata` function, or any `createPublicPageMetadata` call after this change (they are pure redirect entries in `next.config.ts`; the page files are deleted).
7. **AC7 — `/map-my-stack` untouched:** `git diff` contains zero lines touching `frontend/src/app/map-my-stack/` or anything it imports.
8. **AC8 — Required Tests all pass:** every test in Required Tests passes; no existing test outside the explicitly-named frozen-test updates (`page.test.tsx`, `productContract.test.ts`) is weakened or deleted.
9. **AC9 — Diff touches only Allowed Files:** `git diff --stat` lists only files in Allowed Files (deletions and additions both count).
10. **AC10 — Zero regressions:** full frontend test suite (`npm test`), lint, and build remain green.

## Out of Scope

- **`/compounds` gating decision** (audit §4, §7 item 1, §17 P0) — a separate product decision (public-read vs. gated) requiring its own ruling and spec.
- **Homepage/how-it-works proof panel** (audit top item) — closed separately by BIO-FE-001 (`docs/specs/done/BIO-FE-001-homepage-live-proof-panel.md`).
- **Calculator math-clarity explanations** (audit §12) — a distinct UX improvement, not route consolidation.
- **A new interactive mode-toggle UI control** inside `OnboardingExperience` — D-F(1)'s "mode toggle" is satisfied by the existing `?mode=` query mechanism (see Constraints); adding a visible in-page switch control is a UX feature request, not implied by the audit finding or the ruling's text, and is not in this parcel.
- **`/map-my-stack`** (Fact 4) — a distinct, authenticated, paid-upgrade route; not part of the audited duplication.
- **Removing `/start`, `/map`, `/onboarding` from `routes.publicPrefixes`** in the product contract — conservative no-op left in place (see Constraints).
- **Removing or consolidating the pre-existing duplicate `/calculators` redirect handling** between `middleware.ts` and `next.config.ts` — pre-existing, unrelated to this ticket, not touched.
- **Vocabulary/copy-pass items** (audit §5, "Observation Debt," "Cohesion/Drift" tooltips, etc.) — separate backlog items.
- **Live auth-callback-loop smoke test** (audit §10) — unrelated, separate verification task.
- **Analytics/UTM instrumentation** — the audit explicitly marks this "for later — do not implement" (§"Analytics & Instrumentation Recommendations"); this parcel only guarantees generic query-string passthrough already required for mode preservation (Constraints), not new tracking.

## Required Tests

All tests are frontend (`frontend/`, vitest). No backend changes; no new API surface.

1. **T1 — `next.config.ts` redirect entries (new file `frontend/src/__tests__/routing/legacy-onboarding-redirects.test.ts`):** Import the default export of `../../../next.config` and call its `redirects()` function directly. Assert the returned array contains exactly these entries (order-independent, by `source`):
   - `{ source: '/map', destination: '/start?mode=existing', permanent: true }`
   - `{ source: '/onboarding', destination: '/start', permanent: true }`
   - (the pre-existing `/calculators` entry is also present and unchanged — assert it is not removed, do not assert on its exact shape beyond existence)
2. **T2 — `/onboarding` source is exact-path, not prefix:** In the same new test file, assert the `/onboarding` redirect's `source` value is the literal string `'/onboarding'` (not `/onboarding/:path*`, not a regex/wildcard), documenting the guarantee that `/onboarding/consent` is unmatched.
3. **T3 — `frontend/src/__tests__/app/start/page.test.tsx` updated:** Remove the `describe('/map redirect')` and `describe('/onboarding redirect')` blocks and their `MapPage`/`OnboardingPage` imports (the page components no longer exist — see Constraints). The `describe('/start canonical onboarding route')` block is unchanged and must still pass unmodified.
4. **T4 — `frontend/src/__tests__/lib/productContract.test.ts` updated:** Change the assertion `expect(routeAliases['/map']).toBe(canonicalRoutes.analyzer)` to `expect(routeAliases['/map']).toBe(canonicalRoutes.onboarding)` (i.e., `/map` now aliases to `/start`, not `/tools/analyzer`). All other assertions in this file are unchanged.
5. **T5 — `frontend/src/__tests__/middleware.public-routes.test.ts` unchanged, still green:** Confirm (no edit needed, run as regression check) that `/map` and `/onboarding` still pass the "allows anonymous access to public route" parameterized test.
6. **T6 (manual, documented in Evidence) — live redirect verification:** Against a local dev (`npm run dev`) or production (`npm run build && npm start`) server, run:
   - `curl -sI http://localhost:3000/map` → expect `HTTP/1.1 308` (or `HTTP/2 308`) and `location: /start?mode=existing`.
   - `curl -sI http://localhost:3000/onboarding` → expect 308 and `location: /start`.
   - `curl -sI "http://localhost:3000/onboarding?mode=existing"` → expect 308 and `location` containing `mode=existing` (proves query-passthrough empirically, since T1/T2 only prove the config array's shape, not runtime behavior).
   - `curl -sI http://localhost:3000/onboarding/consent` → expect 200 (or the consent page's normal auth-redirect-to-signin behavior, NOT a redirect to `/start`), proving AC3.

## Allowed Files

- `frontend/src/app/map/page.tsx` — delete.
- `frontend/src/app/onboarding/page.tsx` — delete.
- `frontend/next.config.ts` — add `/map` and `/onboarding` entries to the `redirects()` array; no other change to this file.
- `frontend/src/contracts/product-contract.v1.json` — change `routes.aliases["/map"]` from `"/tools/analyzer"` to `"/start"`. No other field changes.
- `frontend/src/__tests__/app/start/page.test.tsx` — remove the two defunct redirect `describe` blocks and their now-dead imports (`MapPage`, `OnboardingPage`); the `/start` describe block is otherwise unchanged.
- `frontend/src/__tests__/lib/productContract.test.ts` — update the single `routeAliases['/map']` assertion per T4.
- `frontend/src/__tests__/routing/legacy-onboarding-redirects.test.ts` — new file per T1/T2.
- `docs/specs/EVIDENCE-BIO-FE-002.md` — new; builder writes test/build/manual-verification output here.

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, any `HANDOFF.md`, any goal `EVIDENCE.md` or `CHARTER.md`.

## Forbidden

- Any file not listed in Allowed Files, including but not limited to: `frontend/src/app/map-my-stack/**` (Fact 4, AC7), `frontend/src/app/onboarding/consent/page.tsx`, `frontend/src/components/marketing/OnboardingExperience.tsx`, `frontend/src/app/start/page.tsx`, `frontend/src/middleware.ts`, `frontend/src/app/sitemap.ts`, `frontend/src/app/robots.ts`, `frontend/src/lib/productContract.ts`, `frontend/src/lib/appRoutes.ts`.
- Adding any new redirect mechanism outside `next.config.ts`'s `redirects()`.
- Adding a wildcard/prefix match for the `/onboarding` redirect source.
- Weakening, deleting, or skipping any existing test other than the two explicitly-named, explicitly-justified updates in Required Tests (T3, T4). All other existing tests, including `frontend/src/__tests__/middleware.public-routes.test.ts`, remain untouched and must stay green.
- Any new interactive mode-toggle UI component (Out of Scope).
- Any claim, numeric, dose, evidence-tier, or recommendation change anywhere.
- Push/PR/merge/deploy by the builder.

## Verification (run from `frontend/` in the Gate-2-named worktree)

1. `npm test -- legacy-onboarding-redirects.test.ts` — T1/T2 green.
2. `npm test -- page.test.tsx` — T3 green (`/start` block passes; no `/map`/`/onboarding` block remains).
3. `npm test -- productContract.test.ts` — T4 green.
4. `npm test -- middleware.public-routes.test.ts` — T5 green (regression check, unedited).
5. `npm test` (full suite) — green, zero regressions (AC8, AC10).
6. `npm run lint` — zero errors.
7. `npm run build` — successful production build (proves `next.config.ts`'s `redirects()` is syntactically and structurally valid to Next.js).
8. Manual live verification (T6): `npm run build && npm start` (or `npm run dev`), run the four `curl -sI` checks listed in T6, capture output.
9. `git diff --stat` — lists only Allowed Files (deletions and additions).

## Evidence Required

1. Test output: T1–T5 (all four vitest invocations plus full-suite run), green.
2. Lint output: zero errors.
3. Build output: successful production build log.
4. Manual verification transcript (T6): the four `curl -sI` command outputs showing status codes and `location` headers, pasted verbatim into `docs/specs/EVIDENCE-BIO-FE-002.md`.
5. `git diff --stat` and `git diff` showing only Allowed Files touched (two deletions, config/JSON edits, three test-file edits/additions).

## Collision Risk

**Low.** Touched files (`frontend/src/app/map/page.tsx`, `frontend/src/app/onboarding/page.tsx`, `frontend/next.config.ts`, `frontend/src/contracts/product-contract.v1.json`, three test files) do not overlap with BIO-FE-001's allowed files (`IntelligenceProofSection.tsx`, its test, `page.tsx`/`how-it-works/page.tsx` read-only). No other active BIO-FE spec exists at shaping time. `next.config.ts` is a single shared file but this parcel's change is additive (two new array entries) and does not touch the existing `/calculators` entry, rewrites, or headers blocks — low surface for conflict even if another parcel touches `next.config.ts` concurrently.

## PR Notes (when Gate 3 opens)

**Title:** `fix(frontend): /map and /onboarding become permanent redirects to canonical /start`

**Summary:** Per owner ruling D-F(1) (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`), `/start` is the sole canonical onboarding route. `/map` and `/onboarding` are now permanent (308, Next.js `permanent: true`) config-level redirects to `/start`, each reproducing the mode state the legacy route represented (`/map` → `?mode=existing`, `/onboarding` → mode passed through or defaulted). This also fixes an unrelated-but-discovered regression: `/map` had drifted to redirect to `/tools/analyzer` (losing the "existing user" funnel entry state) as a side effect of two divergent branches merging without reconciling their conflicting `/map/page.tsx` changes (see spec's Verified Facts for the git-history trace). `/onboarding/consent` (a distinct, authenticated route) is unaffected — the redirect source is an exact path match, not a prefix. No claim, numeric, dose, or evidence-tier surface is touched. `delivery_classes: [standard]`, `function_review_status: not-applicable`.

**Note on 301 vs 308:** D-F(1)'s text says "301 redirects." This PR uses Next.js's native `permanent: true` mechanism, which issues HTTP 308 (not 301), matching the codebase's one existing permanent-redirect precedent (`/calculators` → `/tools`). 308 and 301 are both treated as permanent/cacheable by search engines and browsers; 308 additionally guarantees method preservation (irrelevant here — these are GET-only page routes). This is flagged explicitly for reviewer/owner visibility as an interpretation of "permanent redirect" intent, not a deviation from the ruling's substance.

**Gate 3 merge is the owner's decision; review precedes dispatch.**

## Session Handoff Fields

**Builder receives:**
- Approved spec (this document, rev 1, status active)
- Branch name: `feat/bio-fe-002-route-consolidation`
- Worktree path: `/home/cmorgan76/Repos/biostack-wt/bio-fe-002`
- Baseline commit: main @ latest at Gate 2 approval
- Surfaces: see Allowed Files
- Required checks: see Verification

**Builder delivers:**
- Single commit on the named branch (message: `fix(frontend): /map and /onboarding become permanent redirects to canonical /start`)
- Test evidence: four targeted test runs, full-suite run, lint, build, manual curl transcript, diff stat
- Completion report: AC items checked off, judgment calls confirmed or flagged
- Unchanged: `OnboardingExperience`, `/start` page itself, `/map-my-stack`, `/onboarding/consent`, sitemap, robots, middleware

**Reviewer receives (fresh session, frontier model, read-only):**
- This approved spec (rev 1)
- Builder's named branch
- Test evidence bundle (including the manual curl transcript — this is the one AC that cannot be unit-tested)
- Read-only worktree or diff access
- No builder interaction

## Stop-and-Report Rule

Builder stops and returns to the coordinator if:

- Next.js's `next.config.ts` `redirects()` does not, in practice (per the T6 manual curl check), forward the `mode` query parameter from `/onboarding?mode=existing` to `/start` automatically (i.e., the Constraints assumption about query passthrough proves false). Do not invent a middleware-level workaround — report the finding; the spec's mechanism choice would need revision.
- `/onboarding/consent` is observed to be unintentionally caught by the `/onboarding` redirect (AC3 fails) — report immediately; do not broaden or narrow the match pattern without coordinator sign-off.
- Any test outside the two named frozen-test updates (T3, T4) requires modification to pass.
- The production build (`npm run build`) fails or warns about the new `redirects()` entries.
- Any AC item cannot be satisfied within Allowed Files and approved scope.

## Rollback

Revert the single commit: restores `frontend/src/app/map/page.tsx` (redirect to `/tools/analyzer`) and `frontend/src/app/onboarding/page.tsx` (redirect to `/start`, mode-dropping), restores the two `next.config.ts` entries' absence, restores `product-contract.v1.json`'s `/map` alias to `/tools/analyzer`, and restores the three test files to their prior assertions. No other surface is affected; rollback is a clean single-commit revert.
