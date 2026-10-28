# Everything I'm Taking — implementation handoff

Status: **partial, not release-ready**. Source spec: `C:\Users\clint\Downloads\everything-im-taking-reminders-spec.md` (v5). Starting commit: `022aaa8fea193a9e47dd2994e11e3aedac76f0a4` (includes #604/#605). Worktree: `D:\Repos\BioStack\.worktrees\everything-taking`; branch: `feat/everything-taking`. See Git history and status for the current commit and any subsequent work. The original checkout at `D:\Repos\BioStack` was at `3a3c2b2e` and retains its unrelated untracked files.

## Implemented so far

- Removed the static Supplementation baseline, its endpoint and aggregate section, contracts, mock and UI. Added a **basic**, not yet spec-complete, Everything I'm Taking view using the existing profile-scoped compound API and dose-log form. Old entries are unlinked and therefore displayed as Unverified; add/edit still uses the existing `/compounds` page.
- Schedule now unions active compound records added after a saved protocol with the protocol's items, without duplicating IDs. Paused items are excluded.
- Added `capsule`, `tablet`, `softgel`, `scoop`, `drop`, `serving`, and `gummy` to user-entered item/log units on both sides. Dose inputs remain blank until the user enters or explicitly copies their own saved value.
- A successful dose-log POST followed by a refresh failure now reports that it was recorded, preventing a misleading retry.

## Not implemented — do not enable or release as the completed feature

- **PR1 remains incomplete:** catalog selection/typeahead and persistent catalog links; server-derived verification/category provenance; provider-summary and research opt-out flags; `GET /taking`; Observer cap D6 (existing cap still counts all active items); filters, item-level edit/pause controls and reminders. The basic view cannot distinguish new catalog links yet.
- **PR2 incomplete:** provider-summary inclusion/exclusion and other-items section.
- **PR3–9b not started:** lookup lifecycle, inbox/notes, external probes, provisional items, brand/products and ingredient snapshots, anonymous demand, discovery integration and fulfillment.
- **PR10–12 not started:** reminder scheduler, in-app/push/actions, email digest and unsubscribe.
- Do not infer consent or production-release approval. Spec requires the consent decision before enabling PR3–12, flags default off, plus security/privacy review, migrations/rollback proof, and staging/egress/operational checks. No external services or production resources have been changed.

## Validation performed

- `dotnet test backend/tests/BioStack.Domain.Tests/BioStack.Domain.Tests.csproj --filter FullyQualifiedName~UserDoseTests`: 41 passed.
- `dotnet test backend/tests/BioStack.Api.Tests/BioStack.Api.Tests.csproj --filter 'FullyQualifiedName~ProtocolPortalEndpointsIntegrationTests|FullyQualifiedName~DoseLogAndFrequencyIntegrationTests|FullyQualifiedName~UserEnteredDoseIntegrationTests'`: 92 passed. After the final extra assertions, the narrower filter for portal endpoints, schedule union and new-unit round trips passed 21 tests.
- Frontend focused Vitest: 68 passed in 6 files; focused ESLint for the new tab, tab bar, unit helper and tab tests passed. These used the existing main-checkout `frontend/node_modules` (Vitest 4.1.10, Node 24.7.0), not the spec base's package-lock versions. Validate again with the lockfile and supported Node >=26.8.2.
- Full `tsc --noEmit` did not pass: numerous pre-existing test-global and other test typing errors. There were no reported errors in the new tab or page in a follow-up run with `--types vitest/globals,node`; this is not a passing full typecheck.
- The first parallel backend test invocation collided on the Domain build output (CS2012); the serialized rerun passed. Do not run builds sharing the same `obj` paths concurrently.

## Next safe action

Implement the remaining PR1 contract and migration with server-validated catalog identities and authoritative categories, then D6 cap checks (including updates/reactivation and forged-category tests); wire the catalog picker and verification labels. Keep legacy/unverified names counted and never trust client category or verification fields for exemptions. Follow the source spec's ordering, privacy boundaries and tests. Preserve any subsequent uncommitted changes and the original checkout.
