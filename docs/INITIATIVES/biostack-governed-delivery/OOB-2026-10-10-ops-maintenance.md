# Out-of-Band Change Record — Node 26.8.2 Toolchain Alignment & Security Hardening (2026-10-10)

**Record type:** owner-authorized maintenance change, made outside parcel dispatch.
**Authorization:** direct written instruction from the owner (Clint Morgan) in the 2026-10-10 working session: align all Node configuration to the installed v26.8.2, verify no secrets are headed to the remote, fix the failing tests, fail-closed `DB_PASSWORD`, harden `secret-scan`, and record the change here. "You are authorized to fix."
**Classification:** repository maintenance / CI hardening / compliance-boundary repair. No product capability, contract, or doctrine change is intended or made.
**Why not a parcel:** the governing lifecycle dispatches product-delivery work; this record exists so the registry has an audit trail for maintenance changes that touched governed surfaces. Builders did not approve or close any parcel here.

## Change A — Node toolchain alignment (PR #540, merged `bb1d180a`)

- `deploy.yml`: `setup-node` pinned to `26.8.2` and moved above every `node` step; the sync/verify scripts had been running on the ubuntu runner's default Node (v20).
- `source-acquisition-worker.yml`: added the missing `setup-node` pin (same v20 fallback).
- `research-sidecar-ci.yml`: `22.23.1` → `26.8.2`, verifier assertion updated. The digest-pinned `node:22@sha256:…` P01 repro record in `PARCELS.md` was deliberately left intact as executed-repro evidence.
- `frontend/package.json`: `engines.node >=26.8.2`, `@types/node` `^26`; `frontend/Dockerfile` and `docker-compose.dev.yml` → `node:26.8.2-alpine`; `.nvmrc` added.
- Required companion fix: `vitest ^5.0.3`, `@vitest/coverage-v8 ^5.0.3`, `jsdom ^30.1.2`. Node 26's experimental global `localStorage` getter (undefined without `--localstorage-file`) shadows jsdom's `localStorage` in vitest 4's jsdom environment (`window === globalThis`); 256 frontend tests failed without this.
- `.gitleaks.toml`: allowlisted three confirmed false positives (evidence-doc prose, `biostack-*:<git-sha>` receipt image tags, the public CPython release-signing key ID).

Evidence: PR #540 description and CI run; `node --test scripts/verify-research-sidecar-container.test.mjs` 282/282; `npm test` 1322/1325; `npm run build` clean under Node 26.8.2; `gitleaks dir` clean.

## Change B — follow-up repair and hardening (this PR)

1. **Public content boundary restored** in `frontend/src/components/knowledge/CompoundIntelligenceCard.tsx`: removed the "Known synergies" block that rendered raw `PairsWellWith`. Commit `553dd788` introduced it without updating the guard tests, contrary to [P0-PUBLIC-CONTENT-BOUNDARY-REPAIR-025](../biostack-production-readiness/parcels/P0-PUBLIC-CONTENT-BOUNDARY-REPAIR-025.md), which withholds `PairsWellWith`, compatible blends, and vial compatibility from the public projection and card. The display also matches no approved spec: [BIO-PAIRWISE-006](../../specs/active/BIO-PAIRWISE-006-studied-combinations-surface.md) is `review-candidate` (implementation blocked) and governs *sourced negative relationships* via a future `StudiedCombinations.tsx` with per-pair source and evidence tier — not unsourced positive pairing names. BIO-PAIRWISE-006 remains the governed path to any combinations surface.
2. **Sidebar test aligned to the ContactPage feature** (`Sidebar.collapse.test.tsx`): `553dd788` intentionally replaced the `mailto:support@biostack.cc` link with "Contact Us" → `/contact` (new ContactPage + backend `ContactEndpoints`). The test's accessibility intent (named, operable compact account action) is preserved; only the expected name/href were stale.
3. **`docker-compose.yml` fail-closed on `DB_PASSWORD`**: `${DB_PASSWORD:?…}` replaces the silent `biostack_dev_password` fallback in all three uses (postgres, api, knowledge-worker), matching the file's existing `${Jwt__Secret:?…}` / `${Auth__CallbackSecret:?…}` convention. Local dev remains on `docker-compose.dev.yml`. Also repaired a pre-existing YAML parse bug in the `AUTH_SECRET` fail-closed message (`Generate: openssl…` contains colon-space, which YAML parses as a mapping and made `docker compose config` reject the `biostack-ui` environment list outright).
4. **`secret-scan.yml` hardened**: gitleaks 8.24.3 is now downloaded to `RUNNER_TEMP` and verified against the pinned `sha256:9991e0b2…` before extraction (same pattern and checksum as `research-sidecar-ci.yml`), instead of a piped, unverified `curl | tar` into `/usr/local/bin`.

## Security review status (2026-10-10)

- Working tree and pending diffs: no credentials (gitleaks 8.24.3, repo config).
- Git history: dev-era `Jwt:Secret` / `Auth:CallbackSecret` values from commits `312257f0` / `b13dbc00` were already on `origin/main` and absent from the tree; the owner confirmed rotation on 2026-10-10. Remaining history hits are placeholders, test fixtures, and documented dev defaults.

## Residual follow-ups

- None blocking. If a public combinations/relationships surface is wanted, dispatch BIO-PAIRWISE-006 through Gate 2 (it remains `review-candidate`).
