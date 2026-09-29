# Discovery — biostack-local-readiness (A-1)

Date (UTC): 2026-09-18. Base: `main@e5b75e0` (merge PR #375). Working tree: clean except untracked `Claude outputs/delivery-receipt*.md` and untracked `docs/specs/` (pairwise-lane drafts). No deployments attempted or authorized in this assessment.

## Repository and branches

| Item | Value |
|---|---|
| Repo | single repo, `D:\Repos\BioStack`, tracks: frontend, backend API, knowledge worker, research sidecar, contracts, docs |
| Base branch | `main`, in sync with `origin/main` (`## main...origin/main`, no ahead/behind) |
| Current commit | `e5b75e0` Merge PR #375 `codex/goal-biostack-sidecar-deployment` |
| Recent merges | #373 compound-panel followup, #372 public overlap-check gating, #371 PR369 compile recovery, #370 public interaction-check gating |
| Worktrees | ~100 entries under `.worktrees/` + Codex worktrees; parcel branches are per-fix (`fix/*`, `sonnet/*`, `research/*`); no shared-state violation in `main` itself, but worktree hygiene is a dispatch constraint |
| Untracked | `Claude outputs/` receipts (5 files), `docs/specs/` (6 pairwise files + 1 shaping-result) — neither is part of the launch surface |

## Build and test commands (from README + repo files)

| Track | Command | Notes |
|---|---|---|
| Backend (.NET 10 SDK) | `cd backend && dotnet test` | five test projects under `backend/tests/` + shared fixtures |
| Frontend (Node 22) | `cd frontend && npm ci --include=dev && npm run lint && npm run test` | vitest; production build is the local proof gate |
| Sidecar (uv, out-of-solution) | `cd backend/research-sidecar && uv sync --all-extras && uv run pytest` | FastAPI, admin-only, disabled by default |
| Contract mirrors | `scripts/sync-product-contract.mjs` (+ `--check`) | `contracts/product-contract.v1.json` v1.0.0 is source of truth; frontend/backend mirrors must not drift |
| Guidance backstop | `dotnet test ... --filter "FullyQualifiedName~GuidanceContentContract\|...DoctrineSanitizer\|...EvidenceContextComparison"` | per `docs/guidance/RATIFICATION.md` |
| Hygiene | `git diff --check` | ran 2026-09-18: clean (exit true) |

## Local deployment state

- Dev composition `docker-compose.dev.yml`: API on .NET 10 SDK image (`dotnet watch run`, `ASPNETCORE_ENVIRONMENT=Development`, SQLite `Data Source=/app/data/biostack.db` in named volume `biostack-dev-data`), frontend on Node 22 (`npm install && npm run dev`), ports 5000/5001 + 3043, healthchecks on `/health` and `:3043`.
- Prod-shaped composition `docker-compose.yml`: explicitly NOT RUN in this assessment (requires secrets in `.env`, live Keon runtime or explicit `AllowStubInProduction`, and `KeonRuntime__*` wiring the compose file does not currently pass through — README-noted gap, left untouched per no-deployments scope).
- Keon runtime posture: stubbed in Development (convenience, not a governance claim); Production boot refuses stub unless `KeonRuntime:AllowStubInProduction` is explicit; `StubAllowAll` rejected in Production. Dependency probe at `/health/keon` (contract `health.keonDependencyPath`, liveness `health.livenessPath=/health`).
- Persistence: `Database:Provider` selects PostgreSQL vs SQLite; hashed/serialized values must round-trip both (`DateTimeKind`, PG microsecond truncation affect Spine hash chain). Migrations are hand-written; `dotnet ef migrations add` is forbidden (`ProductionMigrationBaselineConfiguration.cs`).

## Service boundaries and entry points

- Public no-account: `/knowledge`, `/knowledge/[slug]`, `/knowledge/methodology`, `/knowledge/insights/...`, `/tools/*` (reconstitution, volume, unit-converter, analyzer), `/start` (canonical onboarding; `/onboarding`, `/map` are redirect aliases), `/pricing`, `/how-it-works`, `/safety`, `/faq`, `/providers`. Verified in contract `routes.publicPrefixes` + frontend `middleware.public-routes` test + `robots.ts`/`sitemap.ts`.
- Authenticated: protocols/compounds/check-ins/timeline/profiles, `/protocol-console` (post-sign-in default), `/governance/receipts`, `/receipts/[uri]` (authenticated, not public). Auth: magic-link email + passkeys (WebAuthn) + optional OAuth; in-memory inbox in Development.
- Admin/ops: `/admin/research/*` (`adminOnly: true`), research pipeline + taxonomy + curation; KnowledgeWorker (seed/refresh/offline eval) + Python sidecar via `IScientificResearchProvider` (disabled by default: `ScientificResearchSidecar:Enabled=false`).
- Billing: Stripe checkout/webhook/portal exist; credentials blank in checked-in settings — capability demo, not live-billing evidence. Out of scope for local go-live beyond contract-shape + TierGate enforcement.

## Auth / tenant / isolation rules (as coded)

- Owner-scoped access on profiles/protocols; atomic magic-link consumption; server-selected consent evidence (versioned observational consent precedes authenticated writes); provider intake non-enumerating; analyzer egress controls; receipt authorization; per-pair interaction/overlap reasoning gated to `reviewed_relationship_graph` (Operator) with explicit `severity: null` = unavailable in reduced shapes (B3/B4/B5 + correction, `docs/guidance/RATIFICATION.md`).

## CI / evidence artifacts present

- `.github/workflows/`: `deploy.yml`, `secret-scan.yml`, `sonarcloud.yml`, `source-acquisition-worker.yml`, `protocol-operations-offline-verification-kit.yml`, `structural-evaluation-report.yml`.
- Prior initiative state: `docs/INITIATIVES/biostack-production-readiness/` (24 md files, HOLD verdict) — reused as constraint input only, not as passing evidence.
- Frontend readiness audit `BIOSTACK_FRONTEND_READINESS_AUDIT.md` — 11/12 quick wins done at `a37726a`; open items (`/compounds` gating intent, homepage proof panel stub, auth-loop live test) carry forward as local scenarios, not as assumed-fixed.

## Open discovery items (become parcels, not assumptions)

1. Local dev-stack boot + `/health` + `/health/keon` (stubbed) + frontend reachability has no recorded run at `e5b75e0` in this workspace.
2. No recorded local run of contract-mirror `--check`, backend `dotnet test`, frontend lint/test/build, or sidecar pytest at `e5b75e0`.
3. No recorded local proof that `/knowledge` public-read, TierGate enforcement, reduced interaction/overlap shapes, consent gating, or Spine/receipt write-read hold end-to-end locally.
