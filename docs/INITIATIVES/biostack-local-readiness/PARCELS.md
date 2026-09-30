# Parcel Index — biostack-local-readiness

Dispatch order: `BIO-LOCAL-006 + BIO-LOCAL-001` (independent, first) → `002` → `003` → `004` → `005` → `007` (once 001 proves boot) → `008` → `009` → `010` (strictly serial) → `011`. 003/004/005/008/009/010 are serialized (shared auth/Spine/guidance/seed surfaces). Status authority: spec frontmatter + folder (`active/` = dispatchable after Gate 1 + coordinator lint); this table is projection only. Gate 1 GRANTED 2026-09-18 (D1–D10 ratified, OQ1–OQ3 open).

| Parcel | Track | Wave | Status | Worktree | Branch | PR | Collision risk |
|---|---|---|---|---|---|---|---|
| BIO-LOCAL-001 | T1/T2 platform-local | proof | dispatched 2026-09-19 (Gate 2 granted; Step 0 pending) | `D:\Repos\BioStack.BIO-LOCAL-001` | `proof/bio-local-001-local-dev-boot` | — | Medium: `docker-compose.dev.yml`, `.env.example` (sequenced) |
| BIO-LOCAL-002 | T1/T2 public-read | proof | spec-active (Gate 1 granted; awaiting lint + Gate 2) | `D:\Repos\BioStack.BIO-LOCAL-002` | `proof/bio-local-002-public-read` | — | Medium: knowledge endpoints, `middleware.ts`, overlap UI |
| BIO-LOCAL-003 | T2/T5 auth-isolation | proof | spec-active (Gate 1 granted; awaiting lint + Gate 2) | `D:\Repos\BioStack.BIO-LOCAL-003` | `proof/bio-local-003-auth-isolation` | — | High: auth/consent/provider endpoints (serialized) |
| BIO-LOCAL-004 | T4 governance-spine | proof | spec-active (Gate 1 granted; awaiting lint + Gate 2) | `D:\Repos\BioStack.BIO-LOCAL-004` | `proof/bio-local-004-spine-receipts` | — | High: Spine/Keon/cognition assemblies (serialized) |
| BIO-LOCAL-005 | T4/T1 guidance | proof | spec-active (Gate 1 granted; awaiting lint + Gate 2) | `D:\Repos\BioStack.BIO-LOCAL-005` | `proof/bio-local-005-guidance-enforcement` | — | High: doctrine/policy gates, projection (serialized, after 002–004) |
| BIO-LOCAL-006 | T5 contracts | proof | dispatched 2026-09-19 (Gate 2 granted; Step 0 pending) | `D:\Repos\BioStack.BIO-LOCAL-006` | `proof/bio-local-006-contract-mirrors` | — | High: `contracts/*`, `scripts/sync-product-contract.mjs`, `FeatureGate` (sequenced) |
| BIO-LOCAL-007 | T3 seed-inventory | proof | spec-active (awaiting lint + Gate 2) | `D:\Repos\BioStack.BIO-LOCAL-007` | `proof/bio-local-007-seed-gap-inventory` | — | Low (read-only); determines 008–011 viability |
| BIO-LOCAL-008 | T3 seed-batch-A | proof | spec-active (blocked on 007) | `D:\Repos\BioStack.BIO-LOCAL-008` | `proof/bio-local-008-seed-batch-a` | — | High: `Seeds/substances-seed.json` (serialized first writer) |
| BIO-LOCAL-009 | T3 seed-batch-B | proof | spec-active (blocked on 007+008) | `D:\Repos\BioStack.BIO-LOCAL-009` | `proof/bio-local-009-seed-batch-b` | — | High: seed file (rebases onto 008) |
| BIO-LOCAL-010 | T3 seed-batch-C | proof | spec-active (blocked on 007–009) | `D:\Repos\BioStack.BIO-LOCAL-010` | `proof/bio-local-010-seed-batch-c` | — | High: seed file (rebases onto 009; lands exactly 150) |
| BIO-LOCAL-011 | T3/T2 seed-run-proof | proof | spec-active (blocked on 007–010) | `D:\Repos\BioStack.BIO-LOCAL-011` | `proof/bio-local-011-seed-run-proof` | — | High: count-test expectations + local DB state (coordinator-observed) |

## Dependency graph

`006 + 001` (independent) → `002` (needs 001 boot + 006 shape) → `003` (needs 002 honesty baseline) → `004` (needs 003 isolation) → `005` (needs 002–004 surfaces). `007` (needs 001 boot only; may run alongside 002–005 once 001 is green) → `008` → `009` → `010` (strictly serial seed-file order: 009 rebases onto 008's merge, 010 onto 009's; all other parcels fork `origin/main@e5b75e0` or named successor) → `011` (needs 010 merge + 002/003 serving baseline).

## Specs

- `docs/specs/active/BIO-LOCAL-001-local-dev-boot-proof.md`
- `docs/specs/active/BIO-LOCAL-002-knowledge-public-read-proof.md`
- `docs/specs/active/BIO-LOCAL-003-auth-tenancy-isolation-proof.md`
- `docs/specs/active/BIO-LOCAL-004-governance-spine-receipt-proof.md`
- `docs/specs/active/BIO-LOCAL-005-guidance-contract-enforcement-proof.md`
- `docs/specs/active/BIO-LOCAL-006-product-contract-mirror-proof.md`
- `docs/specs/active/BIO-LOCAL-007-seed-gap-inventory.md`
- `docs/specs/active/BIO-LOCAL-008-seed-expansion-batch-a.md`
- `docs/specs/active/BIO-LOCAL-009-seed-expansion-batch-b.md`
- `docs/specs/active/BIO-LOCAL-010-seed-expansion-batch-c.md`
- `docs/specs/active/BIO-LOCAL-011-seed-run-and-serving-proof.md`

## Decisions / risks (seeded; parcel-built detail follows)

- `DECISIONS.md`: Gate 1 GRANTED 2026-09-18 (D1–D10 ratified; OQ1–OQ3 open). No in-parcel decisions yet.
- `RISKS.md`: worktree hygiene, frontend-suite OOM, prod-compose gap (recorded), pairwise-draft scope bleed, seed-claim risk (R5), reachability risk (R6). See FINAL-HANDOFF.
