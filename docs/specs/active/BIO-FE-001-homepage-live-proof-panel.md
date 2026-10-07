---
ticket: BIO-FE-001
title: Homepage intelligence proof panel — replace hardcoded example with live knowledge sample
status: review-candidate
revision: 1
owner: clinton.morgan
created: 2026-10-07
updated: 2026-10-07
supersedes: null
superseded_by: null
risk: standard
delivery_classes: [standard]
guidance_classes: [curated-evidence-guidance]
substance_function_risk: [investigational-or-unapproved]
function_review_status: not-applicable
surfaces:
  - frontend/src/components/marketing/IntelligenceProofSection.tsx
  - frontend/src/app/page.tsx
  - frontend/src/app/how-it-works/page.tsx
  - frontend/src/__tests__/components/IntelligenceProofSection.test.tsx
  - docs/specs/EVIDENCE-BIO-FE-001.md
routing_class: implementation/standard
verification_class: component-testable
permission_profile: reviewer-readonly
data_classification: public
---

# BIO-FE-001 — Homepage live proof panel

## Intent

Close the top remaining item from `BIOSTACK_FRONTEND_READINESS_AUDIT.md` (July 12, 2026 update, §2 "Top 10 UX/IA Risks", §7 "Data Surfacing Gaps"): the homepage and how-it-works intelligence proof panel (`IntelligenceProofSection`) currently hardcodes its example (`compoundNames={['BPC-157', 'TB-500']}` and a static relationship label) instead of sampling from the now-public `/knowledge` data. This is a "missed credibility opportunity" — the real evidence-aware intelligence engine is public, but the homepage still shows a canned example. Replace the hardcoded example with a deterministic live sample sourced from `/knowledge` at request/render time, with a graceful offline/empty-data fallback that never breaks the panel.

**Classification rationale:** This parcel samples data already publicly served via `/knowledge` (per BIO-LOCAL-002, confirmed in `frontend/src/contracts/product-contract.v1.json` public route list). It creates **no new public claim, ranking, or recommendation** — the displayed compounds and their relationships are already public through the knowledge library. The panel is display-only, does not recommend doses or actions, and does not create numeric recommendations. Therefore `delivery_classes: [standard]` is justified. The `guidance_classes: [curated-evidence-guidance]` applies because the panel displays evidence-ranked context from public knowledge entries. The `substance_function_risk: [investigational-or-unapproved]` reflects that the sample compounds (BPC-157, TB-500, or any deterministically-sampled alternatives) are investigational peptides not FDA-approved for human use.

## Goal

Replace the hardcoded proof-of-intelligence example on the homepage and how-it-works pages with a live sample from the public knowledge library, improving credibility by showing that the real engine is reachable and surfacing actual compound intelligence rather than a static placeholder.

## Initiative (Product Lane)

**Frontend readiness polish** — this is NOT a governed-delivery P-series parcel and does NOT change the existing production-readiness verdict (per `BIOSTACK_FRONTEND_READINESS_AUDIT.md` §1: "Ready for provider acquisition. Close to ready for paid conversion."). It addresses a UX credibility gap, not a functional gate. Governed delivery of this parcel demonstrates that frontend polish items can be spec-driven and independently reviewed even when they do not alter product-readiness status.

## Wave

Wave 1 — frontend polish and credibility improvements following the completion of the codex/production-readiness merge (PR #181, 2026-07-11).

## Branch and Worktree

**Proposed:**
- Branch: `feat/bio-fe-001-live-proof-panel`
- Worktree: `/home/cmorgan76/Repos/biostack-wt/bio-fe-001`

Coordinator creates both at Gate 2 approval.

## Prerequisites

- BIO-LOCAL-002 merged (confirmed: `/knowledge` is in the public route list per `product-contract.v1.json`).
- `apiClient.getAllKnowledgeCompounds()` exists and is public-callable (confirmed in `frontend/src/lib/api.ts:337`).

## Verified Facts (coordinator + independent reviewer, main @ caab9ac)

- `IntelligenceProofSection` (frontend/src/components/marketing/IntelligenceProofSection.tsx) is a client component that currently hardcodes `compoundNames={['BPC-157', 'TB-500']}` and `relationshipCandidates` with a static overlap label (lines 32-38).
- The component is used on the homepage (`frontend/src/app/page.tsx:35`) and the how-it-works page (`frontend/src/app/how-it-works/page.tsx`, compact mode).
- `StackIntelligencePanel` (the child component) accepts `compoundNames?: string[]` and `relationshipCandidates?: OnboardingRelationshipCandidate[]` as props (frontend/src/components/marketing/StackIntelligencePanel.tsx:76-77).
- `apiClient.getAllKnowledgeCompounds()` returns `Promise<KnowledgeEntry[]>` where each entry includes `canonicalName`, `classification`, `evidenceTier`, `benefits`, `drugInteractions`, `pairsWellWith`, `avoidWith`, and other structured fields (frontend/src/lib/types.ts and frontend/src/lib/api.ts:337).
- The knowledge library page (`frontend/src/app/knowledge/page.tsx`) already demonstrates the pattern of fetching `getAllKnowledgeCompounds()` on mount with loading/error states (lines 45-52).
- No existing test file `IntelligenceProofSection.test.tsx` exists in `frontend/src/__tests__/components/` (verified via file search).
- The panel's `contentOverrides` prop allows customizing the `nextAction` text (line 37 of IntelligenceProofSection.tsx).

## Constraints

### Deterministic Sampling Rule

- **Primary sample strategy:** On component mount, fetch all knowledge compounds via `apiClient.getAllKnowledgeCompounds()`. If the response contains at least two entries, deterministically select the first two entries sorted alphabetically by `canonicalName`. Display those two compound names.
- **Relationship inference:** If both selected compounds have overlapping values in their `pairsWellWith` arrays, `benefits` arrays, or `pathways` arrays (case-insensitive comparison), display a single overlap candidate with type `'overlap'`, label constructed from the two canonical names, and a generic detail string: `"shared mechanism: educational reference only, with full evidence detail in Operator."` If no overlap is detected, display an empty `relationshipCandidates` array.
- **Offline/empty fallback (deterministic):** If the fetch fails (network error, API unavailable) OR the response is empty (zero entries), fall back to the current hardcoded example: `['BPC-157', 'TB-500']` with the current static relationship candidate. This ensures the panel never renders broken or shows a loading spinner indefinitely on marketing surfaces.
- **No loading spinner on initial render:** Use the hardcoded fallback as the initial state; replace it silently if the live fetch succeeds. This prevents a flash-of-loading on every homepage visit.

### Claims and Numeric Constraints

- **No new claims:** The panel displays only compound names already public in `/knowledge`. It does not introduce new claims, rankings, doses, or recommendations.
- **No numeric dose values:** The panel must not display any `recommendedDosage`, `frequency`, `weeklyDosageSchedule`, or other numeric dose recommendations. Marketing context cannot carry `biostack-recommended` numerics per the ratified product doctrine. If any number is needed, it must be data already publicly served and labeled `source-studied` or `deterministically-derived` — prefer zero numerics here.
- **Evidence tier display:** Do not display `evidenceTier` or `sourceReferences` in this panel — that level of detail belongs in the `/knowledge` dossier pages. The proof panel is a preview only.
- **No personalization:** The sample is deterministic and identical for all visitors (anonymous or authenticated). No profile context, no user-specific filtering.

### Privacy and Telemetry Prohibitions

- **No PII, no tracking:** The fetch of `getAllKnowledgeCompounds()` must not send user identifiers, session cookies, or tracking parameters. The endpoint is public and must remain anonymously callable.
- **No client-side caching of user-specific state:** The sample is computed from public data only; no localStorage or sessionStorage of user preferences for this panel.

### Presentation Constraints

- **Preserve existing layout and styling:** The panel's visual presentation (GlassCard, compound list, relationship labels, CTA buttons) must not change. Only the data source changes.
- **Preserve CTAs:** The "Start free" and "See what Operator unlocks" buttons and their routes remain unchanged.
- **Update nextAction text:** Change the `contentOverrides.simple.nextAction` from `'Browse the evidence library, or start free to keep a list of your own.'` to `'Explore these compounds in the evidence library, or start free to track your own stack.'` to reflect that the displayed compounds are now real and linkable.

## Change

### C0 — Convert IntelligenceProofSection to data-fetching component

Transform `IntelligenceProofSection` from a static display into a minimal data-fetching component:

1. **State management:** Add React state for `compounds: string[]` (initialized to the hardcoded fallback `['BPC-157', 'TB-500']`) and `relationships: OnboardingRelationshipCandidate[]` (initialized to the current static overlap candidate).
2. **Data fetch on mount:** In a `useEffect` with empty dependency array (runs once on mount), call `apiClient.getAllKnowledgeCompounds()`:
   - **Success path:** If response contains ≥2 entries, sort by `canonicalName` alphabetically, take the first two, set `compounds` to their canonical names. Compute overlap (shared `pairsWellWith`, `benefits`, or `pathways` array values, case-insensitive); if overlap exists, construct one relationship candidate; otherwise set `relationships` to `[]`.
   - **Failure/empty path:** On catch or if response.length < 2, leave `compounds` and `relationships` at their initialized fallback values (no state update). No error UI shown; the panel renders the fallback silently.
3. **Pass live data to StackIntelligencePanel:** Replace the hardcoded `compoundNames` and `relationshipCandidates` props with the state variables.
4. **Update nextAction:** Change the override text per Constraints.

### C1 — Component tests (new file `IntelligenceProofSection.test.tsx`)

New test file with focused component tests (no full integration; mock `apiClient`):

| # | Intent | Expectation |
|---|---|---|
| T1 | Mock `getAllKnowledgeCompounds` to return `[{canonicalName: 'Zorbatide', pairsWellWith: ['NAD+'], benefits: [], pathways: []}, {canonicalName: 'NAD+', pairsWellWith: ['Zorbatide'], benefits: [], pathways: []}]` — overlap detected | Renders compound names `Zorbatide` and `NAD+`; relationship candidate present with label `'Zorbatide + NAD+'` |
| T2 | Mock `getAllKnowledgeCompounds` to return two entries with NO overlapping arrays | Renders compound names; `relationshipCandidates` empty (no overlap label shown) |
| T3 | Mock `getAllKnowledgeCompounds` to throw network error | Renders fallback: `BPC-157` and `TB-500` with the original static overlap candidate |
| T4 | Mock `getAllKnowledgeCompounds` to return empty array `[]` | Renders fallback: `BPC-157` and `TB-500` |
| T5 | Compact mode (`compact={true}`) | CTAs hidden; proof panel content still renders |
| T6 | No-claims assertion | The rendered output contains no `recommendedDosage`, `frequency`, or numeric dose strings; assertion scans `container.textContent` |

Tests use `@testing-library/react`, mock `apiClient` via jest.mock, and verify rendered compound names via `screen.getByText` or `within(container).getByText`.

## Acceptance Criteria

1. **AC1 — Live path:** With a real or realistic-fixture knowledge response containing ≥2 entries, the panel displays the first two alphabetically sorted canonical names and infers overlap correctly. T1 and T2 pass.
2. **AC2 — Fallback path:** With a mocked fetch failure or empty response, the panel renders the hardcoded BPC-157/TB-500 fallback. T3 and T4 pass.
3. **AC3 — No-claims enforcement:** T6 passes — no numeric dose or recommendation text is rendered.
4. **AC4 — Full test suite green:** All of `IntelligenceProofSection.test.tsx` passes. Existing `StackIntelligencePanel.test.tsx` (if present) remains green (no regression).
5. **AC5 — Diff touches only Allowed Files:** `git diff --stat` lists only the files in Allowed Files. No route changes, no middleware changes, no API contract changes.
6. **AC6 — Visual regression:** Coordinator reviews a before/after screenshot (localhost dev server) of the homepage proof panel — layout, styling, CTAs unchanged; only compound names differ.
7. **AC7 — Nothing pushed:** Branch is local only; no PR opened.

## Out of Scope

- **Route consolidation** (`/start`, `/map`, `/onboarding` duplication) — separate audit item.
- **`/compounds` gating** — distinct from `/knowledge`; requires a separate decision and spec.
- **Calculator value explanations** — separate UX improvement.
- **SEO metadata for `/knowledge` dossier pages** — recommended in the audit but not this parcel's scope.
- **Live browser verification of the auth callback loop** — separate smoke-test task.
- **Onboarding vocabulary drift** ("Observations" vs "Check-ins") — copy-pass task, not this parcel.
- **Any other frontend-readiness audit item** — this parcel closes only the proof panel item.
- **Expanding the sample beyond two compounds** — the rule is deterministic: first two alphabetically.
- **User-selectable or randomized samples** — deterministic anonymous sample only.
- **Linkifying compound names in the panel** — the "Explore these compounds" nextAction text already encourages navigation; direct links in the panel would require UX design.

## Required Tests

All tests are component-level with mocked `apiClient`:

1. **T1 (live path with overlap):** Two-entry response with detected overlap → both names rendered, relationship candidate present.
2. **T2 (live path without overlap):** Two-entry response, no shared arrays → both names rendered, no relationship candidate.
3. **T3 (fetch failure):** Network error thrown → fallback BPC-157/TB-500 rendered.
4. **T4 (empty response):** Zero-length array response → fallback rendered.
5. **T5 (compact mode):** `compact={true}` → CTAs hidden, proof content shown.
6. **T6 (no-claims assertion):** Rendered output contains no dose/frequency numeric strings.

## Allowed Files

- `frontend/src/components/marketing/IntelligenceProofSection.tsx` (change: data fetch, state, live props)
- `frontend/src/app/page.tsx` (no change expected; verified for imports only)
- `frontend/src/app/how-it-works/page.tsx` (no change expected; verified for imports only)
- `frontend/src/__tests__/components/IntelligenceProofSection.test.tsx` (new)
- `docs/specs/EVIDENCE-BIO-FE-001.md` (new; builder writes test outputs)

Coordinator-owned (builder must not edit): this spec, `docs/specs/INDEX.md`, any `HANDOFF.md`, any goal `EVIDENCE.md` or `CHARTER.md`.

## Forbidden

- Any other file; `apiClient` implementation changes; `/knowledge` route or API endpoint changes; `StackIntelligencePanel` component changes (only props passed to it change); middleware changes; new public routes; editing existing test files (StackIntelligencePanel.test.tsx, if it exists); weakening or deleting existing tests; mocking the frontend knowledge page; push/PR/merge/deploy; altering the existing production-readiness audit verdict or status.

## Verification (run from `frontend/` in the Gate-2-named worktree)

1. `npm test -- IntelligenceProofSection.test.tsx` — all tests green.
2. `npm test` (full frontend test suite) — green, no regressions.
3. `npm run lint` — zero errors.
4. `npm run build` — successful production build.
5. `git diff --check` — no whitespace errors.
6. `git diff --stat` — lists only Allowed Files.
7. Manual localhost verification: `npm run dev`, visit `http://localhost:3000`, observe proof panel displays live data (or fallback if API is down); visit `/how-it-works`, observe compact proof panel.

## Evidence Required

1. Test output: `IntelligenceProofSection.test.tsx` green run.
2. Full test suite output: all frontend tests green.
3. Build output: successful production build log.
4. Screenshot: before (hardcoded BPC-157/TB-500) and after (live sample or fallback if localhost API unavailable) of the homepage proof panel.
5. Diff: `git diff --stat` and `git diff` showing only Allowed Files touched.

## Collision Risk

**Low.** The changed file (`IntelligenceProofSection.tsx`) is a leaf marketing component not currently targeted by any other active spec. The component is used only on two marketing pages (homepage, how-it-works) and does not interact with auth, profiles, billing, or protocols. No other BIO-FE specs exist yet.

## PR Notes (when Gate 3 opens)

**Title:** `feat(frontend): homepage proof panel pulls live knowledge sample`

**Summary:** Replace the hardcoded BPC-157/TB-500 example in the homepage intelligence proof panel with a deterministic live sample from the now-public `/knowledge` library. Fetches all knowledge compounds on mount, displays the first two alphabetically sorted entries, infers overlap from shared `pairsWellWith`/`benefits`/`pathways` arrays. Falls back gracefully to the original hardcoded example if the fetch fails or returns no data. No new claims, no numeric doses, no user tracking. Component-tested with mocked API client (live path, fallback path, no-claims assertion). Closes the top frontend-readiness audit item (missed credibility gap).

**Classification rationale included in PR body:** This parcel samples data already publicly served via `/knowledge` (BIO-LOCAL-002). It creates no new public claim, ranking, or recommendation. Display-only, no dose numerics, no personalization. Therefore `delivery_classes: [standard]`, `guidance_classes: [curated-evidence-guidance]`, `substance_function_risk: [investigational-or-unapproved]` (the sample compounds are investigational peptides).

**Gate 3 merge decision:** Coordinator or delegated reviewer with standing authorization (if granted). Spec remains `status: review-candidate` until independent review passes. Merge is permitted only when all AC items and verification steps are green.

## Session Handoff Fields

**Builder receives:**
- Approved spec (this document, rev 1, status active)
- Branch name: `feat/bio-fe-001-live-proof-panel`
- Worktree path: `/home/cmorgan76/Repos/biostack-wt/bio-fe-001`
- Baseline commit: main @ caab9ac (or latest at Gate 2 approval)
- Surfaces: see Allowed Files
- Required checks: see Verification

**Builder delivers:**
- Single commit on the named branch (message: `feat(frontend): homepage proof panel pulls live knowledge sample`)
- Test evidence: test output, build output, screenshots, diff
- Completion report: AC items checked, any judgment calls explained
- Unchanged: route list, middleware, API contracts, other components

**Reviewer receives (fresh session, frontier model, read-only):**
- This approved spec (rev 1)
- Builder's named branch
- Test evidence bundle
- Read-only worktree or diff access
- No builder interaction

## Stop-and-Report Rule

Builder stops and returns to the coordinator if:

- The `apiClient.getAllKnowledgeCompounds()` signature changes or is unavailable.
- The `StackIntelligencePanel` component's props interface changes in a way that breaks the current usage.
- An overlap-detection edge case appears that is not covered by the "shared array values" rule (report the case; do not invent new overlap logic).
- A test requires changes outside Allowed Files (e.g., mocking strategy needs a new test utility file).
- The fallback path cannot be verified without pushing to staging or production (local dev server + mocked failure is sufficient).
- Any AC item cannot be satisfied within the Allowed Files and approved scope.

## Rollback

Revert the single commit: restores the hardcoded `compoundNames` and `relationshipCandidates` props in `IntelligenceProofSection.tsx`. The component returns to its original static state. Tests are additive (safe to leave or remove; no other tests depend on them).

## Ratified Decisions (developer/coordinator, 2026-10-07)

1. **Deterministic sample strategy:** First two entries alphabetically by `canonicalName`. No randomization, no user filtering.
2. **Offline fallback is the current hardcoded example:** BPC-157/TB-500 with the static overlap candidate. This ensures the panel never breaks on marketing surfaces.
3. **No loading spinner on initial render:** Start with fallback, replace silently if live fetch succeeds. Avoids flash-of-loading on every homepage load.
4. **Overlap detection rule:** Shared values (case-insensitive) in `pairsWellWith`, `benefits`, or `pathways` arrays constitute overlap. If detected, show one relationship candidate; otherwise show none.
5. **No new claims:** The panel displays only compound names already public in `/knowledge`. No numeric doses, no recommendations.

## Context & References

- `BIOSTACK_FRONTEND_READINESS_AUDIT.md` (July 12, 2026 update, §2 item 10, §7 "Data Surfacing Gaps" item 4): "Homepage/how-it-works proof panel (`IntelligenceProofSection`) is still hardcoded... This is now the single biggest remaining lever — you have a real public dossier to link to or sample from, and the homepage still shows a canned example next to it."
- `frontend/src/contracts/product-contract.v1.json`: `/knowledge` confirmed in `publicPrefixes` array (BIO-LOCAL-002).
- `frontend/src/lib/api.ts:337`: `getAllKnowledgeCompounds()` method signature and return type.
- `frontend/src/lib/types.ts`: `KnowledgeEntry` interface definition.
- `frontend/src/app/knowledge/page.tsx`: Exemplar pattern for fetching knowledge data with loading/error states.
- `docs/INITIATIVES/biostack-governed-delivery/CHARTER.md`: Ratified product doctrine, guidance classes, substance/function risk vocabulary, D14/D15 classification mechanics.
