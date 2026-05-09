# Research Review UI — Design Spec

**Date:** 2026-05-05  
**Status:** Approved for implementation planning  
**Scope:** Internal admin workbench — not a public-facing surface  

---

## 1. Purpose

Build an internal compound research review UI that allows BioStack reviewers to inspect research pipeline runs, triage compounds by risk/readiness, inspect evidence claims, understand blockers and required actions, and prepare review decisions for export.

This is a **review workbench, not an analytics dashboard.** Every design decision should prioritize blockers, required action, evidence status, and decision readiness over charts or summary visualizations.

---

## 2. Guiding Rules

- Do not invent claims. Display evidence exactly as supplied by pipeline artifacts.
- Do not generate medical advice, prescriptions, dosing recommendations, or personalized guidance.
- All safety / regulatory / dosing fields must show provenance and review status.
- Missing fields: use `Unknown` for semantically meaningful unknowns (e.g. evidence tier, classification, completeness status). Use `—` only for truly not-applicable optional context fields (e.g. claim context fields like `population`, `route`, `doseText` that are nullable by schema). Never infer a value.
- Treat blocked and review-required records as internal only.
- Maintain clear separation between: approved label uses, studied uses, common claims, misinformation claims, evidence gaps, safety warnings, regulatory status.
- The Review Decision form must be guarded: blocked compounds show a disabled/locked form with hard blockers repeated. Approving around hard blockers is not possible through the UI.

---

## 3. Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Navigation | Flat Next.js app-router route tree | Matches existing codebase pattern; every view has a shareable URL |
| Data loading | Fixtures + API route, env-switched | Fixtures enable full UI development before pipeline runs; API route enables live artifact preview |
| Auth | Existing admin dev-token pattern | Consistent with `/admin` page; no new auth surface |
| Decision persistence | React state batch, exported as JSON | `review-decision-batch` schema; no DB write in v1 |
| Visual system | Existing dark glass system (GlassCard, tokens, Tailwind) | No new design tokens introduced |

---

## 4. Route Tree

```
/admin/research                              → Research Runs Dashboard
/admin/research/compounds                    → Compound Review List
/admin/research/compounds/[slug]             → Compound Review Detail
/admin/research/pipeline                     → Promotion Pipeline Panel
```

`[slug]` is a URL-safe identifier derived from the canonical compound name (lowercase, punctuation → hyphen, e.g. `bpc-157`, `testosterone-cypionate`). A `slugToCanonicalName` lookup is built at load time from the artifact data so the detail page can resolve back to the canonical name for all data fetching.

All routes are under the existing admin shell. A "Research" link is added to the admin navigation.

### Route states
- **`/admin/research/compounds/[slug]` — unknown slug:** renders a "Compound not found in current research run" state with a back link to the list. No crash, no redirect loop.
- **`/admin/research/compounds/[slug]` — missing artifact:** renders a "Research artifacts not yet generated" state for each missing section. The breadcrumb and identity header still render if the summary data is available; missing evidence-packet data shows an empty state per section, not a page-level error.

---

## 5. Data Loading

### Fixture path
`frontend/src/fixtures/research/`

Files mirroring pipeline artifact structure:
- `draft-substances.json`
- `review-queue.json`
- `research-summary.json`
- `promotion-manifest.json`
- `review-resolution-plan.json`
- `promotion-export/promotion-export-manifest.json`
- `promotion-export/substances.promotable.json`
- `promotion-import-preview.json`
- `import-dry-run/promotion-import-dry-run-report.json`

### API route
`frontend/src/app/api/research/artifacts/route.ts`

**Auth guard:** The route handler reuses the existing admin dev-token check (same pattern as `/api/v1/admin/*`). Requests without a valid admin token receive `401`. The handler fails closed — any auth error returns 401, never a partial response.

**Path safety:** The route reads only from `RESEARCH_ARTIFACTS_PATH` (default: `research/pilot/`). The path is resolved at server startup and validated to be within the project root. Artifact filenames are a fixed allowlist (the 9 files above). No user-supplied paths or filenames are accepted. Directory traversal is not possible.

### Switching
`NEXT_PUBLIC_RESEARCH_DATA_SOURCE=fixtures|api` (default: `fixtures`).

### TypeScript types
`frontend/src/lib/research/types.ts` — derived from JSON schema definitions in `backend/src/BioStack.KnowledgeWorker/Schemas/`. One type file; no runtime schema validation in the UI.

---

## 6. Page Designs

### 6.1 Research Runs Dashboard — `/admin/research`

**Layout:** Stat bar (top row) + two-column body.

**Stat bar (6 chips):**
- Total Drafts
- Blocked (red)
- Review Required (amber)
- Candidates for Promotion (green)
- Dry-Run Safe (green)
- Last Run (date/time, neutral)

Each chip uses the existing `StatMiniCard` color pattern (red/amber/green/neutral).

**Left column:**
- Review Categories list — name, compound count badge, color-coded by category type (Safety Critical = red, Regulatory = purple, Misinformation = orange, Weak Evidence = amber, Route/Formulation = blue, Source Registry = slate). Clicking a category navigates to `/admin/research/compounds?category=<name>`.
- Quality Flags breakdown — top flags with compound counts.

**Right column:**
- Promotion Readiness breakdown — horizontal bar segments (blocked / review-required / candidate) with counts.
- Resolution Plan summary — total items, counts by resolution type.

**Data source:** `research-summary.json` + `promotion-manifest.json` + `review-resolution-plan.json`.

---

### 6.2 Compound Review List — `/admin/research/compounds`

**Layout:** Filter bar (top) + scrollable compound card list.

**URL state:** All active filters and the selected sort are synced to query params. Example: `/admin/research/compounds?readiness=blocked,review-required&category=Safety+Critical&sort=risk`. This makes the current queue shareable and bookmarkable, and aligns with the flat-route decision.

**Filter bar:**

*Primary filters (always visible):*
- Readiness chips: `Blocked (n)` / `Review Required (n)` / `Candidate (n)` — color-coded red/amber/green, multi-select toggle.
- Review Category chips: one per category present in the run, neutral style, multi-select.

*Secondary filters ("More filters" expander, collapsed by default):*
- Evidence tier (Strong / Moderate / Limited / Insufficient / Unknown / Anecdotal)
- Classification (Peptide / Small Molecule / Pharmaceutical / SARM / etc.)
- Quality flags (multi-select chips)

*Sort:*
- Risk Priority (default — blocked first, then review-required, then candidates; within each group sort by queue item count desc)
- Compound Name (A–Z)
- Evidence Tier
- Completeness

**Compound cards:**

Left border color = readiness (red = blocked, amber = review-required, green = candidate).

Each card shows:
- Compound name (prominent)
- Readiness badge (top-right)
- Metadata row: Classification · Evidence Tier · Completeness · Queue count — wraps to two lines under long names; minimum font size 11px; no truncation of metadata values.
- First promotion blocker (italic, truncated with ellipsis) — only shown for blocked/review-required compounds.

Selected card gets a blue highlight. Clicking navigates to `/admin/research/compounds/[slug]`.

**Data source:** `research-summary.json` + `promotion-manifest.json`.

---

### 6.3 Compound Review Detail — `/admin/research/compounds/[slug]`

**Layout:** Breadcrumb nav + four-tab panel.

**Tabs (always show counts):**
`Overview` · `Claims (n)` · `Resolution Plan (n)` · `Review Decision`

#### Tab 1 — Overview (command center)

Shows the current truth and the next move only. Sections in order:

1. **Identity header** — canonical name (large), aliases (subtitle), classification tag.
2. **Status grid** (3-column) — Evidence Tier, Completeness, Readiness, Review Queue count, Needs Review flag, Review Decisions count. Each cell color-codes its value (Strong/Substantial/Candidate = green; Moderate/Partial/Review-Required = amber or blue; Insufficient/Minimal/Blocked = red). Unknown values display as `Unknown` in neutral gray, never inferred.
3. **Promotion Blockers** — each blocker as a card with severity icon (✕ = hard block, ⚠ = review-required). `blocked:` prefix = red; `review-required:` prefix = amber. If none: green "No blockers — eligible for promotion review" state.
4. **Required Next Actions** — each action as a card with → icon, blue tint. Sourced from `PromotionManifestCandidate.RequiredNextActions` + `ResearchReviewCategory.RecommendedActions`.
5. **Quality Flags** — chip list, red tint for authority-related flags (`missing-authoritative-support`, `source-registry-*`), neutral otherwise.
6. **Review Categories** — each category with its matching signals (quality flags / review reasons).
7. **Source Provenance** — list of source IDs referenced by claims, with authority tier and type. Sourced from evidence packet `sources[]`.

#### Tab 2 — Claims (n)

Evidence claim viewer. Each claim card shows:
- Claim type (badge, uppercased)
- Evidence tier badge (color-coded: Strong=green, Moderate=blue, Limited=amber, Insufficient=red, Unknown/Anecdotal=neutral)
- Confidence (low/moderate/high/unknown)
- Statement (full text, no truncation)
- Context (population, route, formulation, use case, dose text) — these are nullable schema fields; display `—` when null (not-applicable optional context), `Unknown` when the field is present but its value is semantically unknown.
- Source refs (chip list)
- `fieldAuthorityRequired: true` → red warning chip "Field Authority Required"
- Review flags (chip list)
- Extracted evidence (collapsible — quote + source ref + page/section)

Claim type determines display order: safety/regulatory/warning/contraindication claims first, then studied-use/approved-indication, then mechanism/efficacy, then misinformation/evidence-gap/controversy last.

#### Tab 3 — Resolution Plan (n)

Each resolution plan item as a card:
- Resolution type (colored label)
- Readiness severity badge
- Issue text
- Recommended action (blue card)
- Related blockers (chip list)
- Related quality flags (chip list)

#### Tab 4 — Review Decision

**Guard condition — hard blockers:** If the compound has any `blocked:` prefix blocker, the form is fully disabled. A banner at the top lists each hard blocker and displays: "This compound has unresolved hard blockers. Resolve all `blocked:` items before submitting a review decision." The "Add to Decision Batch" button is hidden.

**When unlocked (review-required or candidate — no `blocked:` prefix blockers):**
- Decision radio: `approve-for-promotion` / `approve-claims` / `request-changes` / `reject`
- Reviewer ID (text input, required)
- Notes (textarea, required when decision is `request-changes` or `reject`)
- Scope (auto-populated from compound record: claim IDs, quality flags, review categories, promotion blockers — displayed as read-only chips, not editable in v1)
- **Clears Soft Promotion Blockers** (yes/no radio):
  - If decision is `approve-for-promotion` and soft blockers (`review-required:` prefix) remain, this field is **required** and must be set to `yes` with at least one note explaining why the soft blockers are cleared. The form will not submit without this.
  - For all other decisions the field is optional.
- "Add to Decision Batch" button — appends to in-memory batch (React state, shared via context)
- Batch status: "n decision(s) in current batch · Export batch as JSON"

**Data source:** `promotion-manifest.json` for blockers; decision state is local React context.

---

### 6.4 Promotion Pipeline Panel — `/admin/research/pipeline`

Four expandable sections:

1. **Promotion Manifest** — counts summary (total, blocked, review-required, candidates), table of candidates with name/classification/readiness/evidence tier.
2. **Export Manifest** — sourced from `promotion-export/promotion-export-manifest.json`. Shows exported compound count, schema version, output paths.
3. **Import Preview** — sourced from `promotion-import-preview.json`. Table: name, action (create/update/skip), schema valid badge, active badge, existing seed match, reasons.
4. **Dry-Run Report** — sourced from `import-dry-run/promotion-import-dry-run-report.json`. Pass/fail indicator per compound, error details if any. Safe count vs blocked count prominently displayed.

All sections have empty states for missing artifacts ("Artifact not yet generated for this run").

---

## 7. Shared Components

| Component | Location | Purpose |
|---|---|---|
| `ResearchStatChip` | `components/research/ResearchStatChip.tsx` | Colored stat chip for dashboard bar |
| `ReadinessBadge` | `components/research/ReadinessBadge.tsx` | Blocked / Review-Required / Candidate badge |
| `EvidenceTierBadge` (extended) | `components/knowledge/EvidenceTierBadge.tsx` | Add research pipeline tiers (Insufficient, Anecdotal, Unknown) as a scoped extension. The new tier mapping lives in a separate `researchTierLabels` constant alongside the existing `labels` map; existing consumers of the component are not broken. `theoretical` is not a pipeline tier and is not added. |
| `CompoundCard` | `components/research/CompoundCard.tsx` | List card with left-border readiness color |
| `BlockerCard` | `components/research/BlockerCard.tsx` | Hard/soft blocker display card |
| `ClaimCard` | `components/research/ClaimCard.tsx` | Evidence claim viewer card |
| `ResolutionPlanItem` | `components/research/ResolutionPlanItem.tsx` | Resolution plan action card |
| `ReviewDecisionForm` | `components/research/ReviewDecisionForm.tsx` | Guarded decision form |
| `ReviewDecisionBatch` | `lib/research/reviewDecisionBatch.ts` | In-memory batch logic + JSON export |
| `FilterBar` | `components/research/FilterBar.tsx` | Primary + secondary filter chips + sort; syncs to query params |
| `ResearchDataLoader` | `lib/research/loader.ts` | Fixture vs API route switching |
| `slugToCanonicalName` | `lib/research/slugs.ts` | Slug ↔ canonical name lookup built from artifact data |

---

## 8. TypeScript Types (summary)

All derived from pipeline artifact schemas. Key types:

```ts
// From research-summary.json
type ResearchSummary = { draftSubstanceCount, reviewQueueItemCount, compounds, reviewCategories, promotionReadiness, qualityFlags, reviewReasons, classifications, evidenceTiers }
type ResearchSummaryCompound = { name, classification, overallEvidenceTier, completeness, needsReview, reviewQueueItemCount, promotionReadiness, promotionBlockers, reviewDecisionIds, qualityFlags, reviewReasons }
type ResearchReviewCategory = { name, count, compounds, signals, recommendedActions }

// From promotion-manifest.json
type PromotionManifestCandidate = { name, classification, readiness, overallEvidenceTier, completeness, reviewQueueItemCount, reviewDecisionIds, blockers, qualityFlags, requiredNextActions }

// From evidence-packet.schema.json
type EvidenceClaim = { claimId, claimType, statement, context, evidenceTier, confidence, fieldAuthorityRequired, sourceRefs, extractedEvidence, reviewFlags }

// From review-decision.schema.json
type ReviewDecision = { decisionId, compoundName, decision, reviewerId, reviewedAt, scope, clearsSoftPromotionBlockers, expiresAt, notes }
type ReviewDecisionBatch = { schemaVersion, recordType, batch, decisions }

// Slug utility
type SlugMap = Map<string, string>  // slug → canonicalName
```

---

## 9. Tests

- Filter logic (readiness, review category, evidence tier, classification, quality flag — single and combined)
- Sort logic (risk priority ordering: blocked before review-required before candidate; secondary sort by queue count)
- Query param sync: filter/sort state serializes to URL and restores correctly on load
- `ReadinessBadge` renders correct color for each readiness value
- `BlockerCard` distinguishes `blocked:` prefix from `review-required:` prefix
- Review Decision form is disabled when compound has `blocked:` blocker; enabled for review-required / candidate
- `approve-for-promotion` with remaining soft blockers requires `clearsSoftPromotionBlockers = yes` and at least one note; form blocks submission otherwise
- Unknown slug renders "not found" state, not a crash
- Missing artifact file renders section-level empty state, not page-level error
- Counts on tabs and filter chips reflect fixture data correctly
- `ReviewDecisionBatch.toJson()` produces valid schema output
- Empty/loading/error states render without crashing
- Missing artifact sections in Pipeline Panel render empty state, not error
- `slugToCanonicalName` round-trips correctly for names with spaces, hyphens, salts (e.g. "Testosterone cypionate", "GHK-Cu")

---

## 10. Out of Scope (v1)

- Public-facing compound pages
- Writing review decisions to the database
- Real-time pipeline run triggering from the UI
- Diff view between research runs
- Reviewer role / permissions system (admin token is sufficient for v1)
- Inline claim editing
