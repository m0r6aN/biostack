# Everything I'm Taking — implementation handoff

Status: **phases 1-2 of spec v5 implemented on this branch (PR #606), not merged, not release-ready**. Phases 3+ (lookup, research requests, products, reminders) are intentionally **not started**: they wait on the tabled consent decision (`bio-observational-v1`). Source spec: `C:\Users\clint\Downloads\everything-im-taking-reminders-spec.md` (v5). Base: `022aaa8fea193a9e47dd2994e11e3aedac76f0a4` (includes #604/#605). Branch: `feat/everything-taking`. See Git history for the current commit.

## Implemented

**From the first pass (Codex):** static Supplementation guide, endpoint, aggregate section, contracts, mock and UI retired; the Everything I'm Taking tab replaces it (`?tab=taking`); schedule unions active records added after a saved protocol; `capsule/tablet/softgel/scoop/drop/serving/gummy` units; a dose-log POST followed by a refresh failure reports "recorded".

**Phase 1 (catalog link + cap), second pass:**
- Migration `20261028190000_AddCompoundCatalogLinkAndVerification` (hand-written, additive, applied on API start; model snapshot untouched like its siblings): `CatalogSlug`, `VerificationState`, `CategorySource` (nullable, no backfill) and `IncludeInProviderSummary` (NOT NULL DEFAULT TRUE, so the previous release keeps inserting valid rows). Test: `CompoundCatalogLinkMigrationTests`.
- Linking is a user pick only. `CreateCompoundRequest`/`UpdateCompoundRequest` gain `CatalogSlug` and `IncludeInProviderSummary`. There is deliberately **no** client field for verification state or category source. The server validates the slug against the live public catalog (`ICatalogItemResolver`, backed by `IKnowledgeService`), copies the **catalog category**, and derives `VerificationState` (`catalog`/`unverified`) and `CategorySource` (`catalog`/`user`). An unknown slug is a 400 on `catalogSlug`. On update: slug `null` keeps the link (old tabs and stale clients cannot unlink by accident), `""` removes it; `includeInProviderSummary` `null` keeps the stored value. Nothing is ever linked automatically and linking never copies a dose, unit, frequency or time.
- **Cap rule (D4/D6)**, `ObserverCapRules` + `CompoundService.EnsureActiveCompoundLimitAsync`: an active item is outside the Observer cap only if it is catalog-linked, `CategorySource = catalog`, and its catalog category is in `{Supplement, Vitamin, Mineral, AminoAcid, Nutraceutical}`. Everything else counts: research and other catalog categories, **Coenzyme**, every unverified custom item whatever category the client sent, and legacy rows (NULL state, no slug). The category of a linked row is read from the live catalog record; if the record can no longer be resolved (for example retired) the server-set category stored at link time is used. The check runs on create, on edit and on reactivation, with the state the server just derived. Message: "Observer includes up to N active research or unconfirmed items. Confirmed supplements, vitamins, minerals and amino acids don't count. …" (code `observer_active_compound_limit` unchanged; the limit itself stays 8 in `product-contract.v1.json`). Copy updated in `app/billing/page.tsx` (two places, plus the Operator card's "active item limit") and `lib/marketing.ts`, with their tests.
- `GET /api/v1/profiles/{id}/taking` (ownership-checked): the user's items with server-derived state, `countsTowardCap`, read-time **"Same as" offers** (exact normalized name or alias match against the catalog; offers only), and `cap {limit, counted}`.
- Add flow (`CompoundForm`): a client-side typeahead over the library (names and aliases, with category); picking a row, the compound dropdown or a dossier deep link links the record and shows "In BioStack: linked to X" with Unlink; typing alone never links; editing the name away from the record drops the link. Free text stays allowed and is labelled Unverified. The submit payload carries only `catalogSlug`; dose fields stay blank. **Deviation from the spec's single-screen flow:** the existing 4-step form is kept and extended rather than rewritten, to avoid breaking its many tests; the lookup panel is phase 3.
- Everything I'm Taking tab: reads `GET /taking`; real badges ("In BioStack" links to `/knowledge/{slug}`, otherwise "Unverified"); filter chips (All, Peptides & research compounds, SARMs/SERMs/hormones, Supplements, Vitamins & minerals, Unverified); a cap line; "Same as X? Link it" buttons; a per-item "Include in provider summary" toggle. A "Pending review" chip and badge arrive with provisional items (phase 5).

**Phase 2 (provider summary):** `ProviderObservationalSummary` takes optional `currentItems` (the live records, loaded on the protocol page). When given: items switched off are left out of Active substances and Logged amounts, protocol items and new items get "In BioStack — user-entered" / "Unverified — user-entered" labels (on screen and in the copied text), and active items outside the saved protocol appear under **Other items you take**, verbatim from the user's entries, with "No BioStack data for this item." for unverified ones. Without `currentItems` the summary renders exactly as before.

**Tooling fix:** `frontend/src/__tests__/vitest-env.d.ts` (type-only) gives the full `tsc -p tsconfig.json` the vitest globals and jest-dom matchers that `vitest.config.ts`/`setup.ts` already provide at runtime. It took the full-tsc error count on main from 2,548 to 94 and is excluded from the production build (`tsconfig.build.json`). Also removed an invalid `exact: true` ByRole option (4 call sites in 2 test files, no runtime effect).

## Not implemented (unchanged)

- **Phase 3+ (do not start before the consent decision):** `CatalogLookups`/notices/bell, matcher panel and "Please wait" flow, external probes, provisional items and the exempt "provisional" cap path, `SendNameToResearch`, `ItemKind`, `CatalogLookupId`, products and ingredients, anonymous demand, discovery integration, reminders, push and email digest. The columns for these are intentionally not added yet (additive later).
- The spec lists `ItemKind` and `SendNameToResearch` columns in PR1; they have no consumer until phases 3 and 6, so they are left out here.
- Do not infer consent or production-release approval. No external services or production resources were changed.

## Known risks and notes

- Removing the supplements endpoint and the aggregate `supplements` key returns 404 / a missing key to a stale open tab until it reloads. No other consumer exists.
- The summary's per-item toggle is read at generation time from the live record, so it applies to older saved versions too.
- Protocol-snapshot compounds (`ProtocolService.MapCompound`) carry no catalog state; the summary labels them from the live record by id.
- After merge to main the Deploy workflow applies the additive migration on API start. The PR run of that workflow only builds and tests (every deploy step is skipped for `pull_request`).

## Validation performed (box, Node 26.12.1, dotnet 10.0.402, lockfile install)

- Backend: Domain 118, Application 1,050 passed / 5 skipped (all prior skips; includes 28 new cap/link/guardrail tests), Api 673 passed (includes 5 new HTTP tests and 5 migration tests; `BillingTierIntegrationTests` still green).
- Frontend: `vitest run --pool=threads --maxWorkers=2 --no-file-parallelism`: 186 files, 1,690 tests passed (includes 18 new tests); `npm run build` passes; `tsc -p tsconfig.build.json` 0 errors.
- Full `tsc --noEmit -p tsconfig.json`: 94 errors remain, **identical on main** (test-only typing, mostly `vi.fn()` and mock-shape overloads in KnowledgePage, CompoundRelationshipsSection, CompoundsPage, ProtocolDetailPage, research route and start page tests). None introduced by this branch. CI does not run it.
- `scripts/sync-*.mjs --check` pass.

## Next safe action

Wait for the consent decision. Then phase 3 per spec section 10 (internal lookup + notices), with flags default off. Preserve the migration timestamp ordering (`20261028190000` is the latest). Run builds sequentially; they share `obj`.
