# Tracks — biostack-local-readiness (A-2)

Single repo, five tracks (PDD multi-project discipline, repo-local execution).

| ID | Track | Locations | Role | Status (assessment) |
|---|---|---|---|---|
| T1 | Frontend experience | `frontend/src/**`, `frontend/middleware.ts`, `docker-compose.dev.yml:biostack-ui-dev` | Public knowledge/tools read, TierGate rendering, reduced-shape honesty, analyzer/calculator UX | unverified (prior audit signal at `a37726a`, not proof at `e5b75e0`) |
| T2 | API and data | `backend/src/BioStack.Api/**`, `Application/**`, `Domain/**`, `Infrastructure/**`, `docker-compose.dev.yml:biostack-api-dev` | Auth (magic-link/passkey), ownership, consent, feature gates, knowledge endpoints, health probes, SQLite local | unverified |
| T3 | Knowledge pipeline | `backend/src/BioStack.KnowledgeWorker/**`, `backend/research-sidecar/**`, `research/**` | Source-first ingest → classify → review → promote; sidecar disabled-by-default; offline eval; **seed corpus 57 → 150 (draft-only expansion + local SeedJob run)** | unverified (enabled-path NOT exercised locally in this goal; seed count at 57, target 150 per D10) |
| T4 | Governance and cognition | `BioStack.Application/Governance/**`, `BioStack.Domain/Governance/**`, `BioStack.Infrastructure/Governance/**`, `BioStack.Infrastructure/Keon/**`, `BioStack.Cognition*/**`, `BioStack.Api/Governance/**` | Doctrine/policy gates, Spine chain + checkpoints, Keon receipts, fail-closed boot, cognition envelope (not user text) | unverified |
| T5 | Contracts and evidence | `contracts/product-contract.v1.json`, `scripts/sync-product-contract.mjs`, `docs/guidance/**`, `docs/canon/**`, `docs/INITIATIVES/biostack-local-readiness/**`, `docs/specs/active/BIO-LOCAL-*.md` | Frozen contract truth, mirror integrity, assessment state, parcel specs, verification log | in-progress (this assessment) |

No track is complete because code merged. Each track clears only via its parcel's green chain + verification record.
