# Scenarios — biostack-local-readiness (A-3/A-4, local only)

Environment for every row: **local** (`docker-compose.dev.yml`, SQLite, stubbed Keon, in-memory inbox) at `e5b75e0` unless a parcel re-pins the SHA. No staging/production inference.

| ID | Surface | Scenario | Type | Status | Evidence required (parcel) |
|---|---|---|---|---|---|
| LS1 | L1 | `docker compose -f docker-compose.dev.yml up --build` boots; `/health` 200; `/health/keon` reports stubbed-dev posture; frontend `:3043` reachable; `down -v` resets | positive/failure | unverified | 001: compose log + curl transcripts + volume/reset note |
| LS2 | L2 | Anonymous `GET /knowledge`, dossier `/knowledge/[slug]`, `/knowledge/methodology`, `/tools/*` read 200 without sign-in; `/onboarding`+`/map` redirect to canonicals | positive | unverified | 002: HTTP transcripts + route-alias assertions |
| LS3 | L2/L8 | Anonymous + Observer overlap/interaction-check return ONLY pairs/ids + `severity: null`; no Description/Reason/Confidence/mechanism/direction; entitled Operator gets full shape | negative (leak-proof) | unverified | 002+005: request/response fixtures (PII-free) + projection unit proof |
| LS4 | L3 | Magic-link start→verify via in-memory inbox returns to intended protected route; unauthenticated protected route denies/redirects; tampered/expired link fails closed | positive/negative/failure | unverified | 003: inbox transcript + browser/curl trace + denial fixtures |
| LS5 | L3/L4 | Authenticated user A cannot read/write user B's profile/protocol/check-in (direct-ID + list-scope); provider intake does not enumerate existence | negative | unverified | 003: isolation test output + non-enumeration proof |
| LS6 | L3 | Authenticated write without current consent version is rejected; consent acceptance persists server-selected version evidence | negative/positive | unverified | 003: consent-version test output |
| LS7 | L4/L5 | Spine write→read round-trips identically on SQLite; checkpoint signs + verifies; receipt write/read holds tenant scoping; `/governance/receipts` denies anonymous | positive/negative | unverified | 004: chain/round-trip test output + probe transcript |
| LS8 | L5 | Production-boot check refuses stubbed runtime without explicit `AllowStubInProduction` (unit/config proof — NOT a prod boot) | negative | unverified | 004: fail-closed unit/config test output |
| LS9 | L8 | Guidance backstop suite green (`GuidanceContentContract`, `DoctrineSanitizer`, `EvidenceContextComparison`); Class D probe denied; calculator output is math-only | positive/negative | unverified | 005: `dotnet test --filter ...` output + probe fixtures |
| LS10 | L7 | `sync-product-contract.mjs --check` green; monthly-only/grace-zero/alias/health-path/entitlement assertions hold in `FeatureGate` + TierGate rendering | positive | unverified | 006: `--check` output + entitlement test output |
| LS11 | L1–L8 | `git diff --check` clean on every parcel close; no secrets/PII/health payload in any artifact | hygiene | passing (assessment-level: ran 2026-09-18 clean) | per-parcel `git diff --check` + redaction attestation |
| LS12 | T3 corpus | Seed-gap inventory proves 150 reachable from existing repo inputs (or proves unreachable → owner ruling); batch A/B/C allocation recorded | inventory | unverified | 007: allocation record + reachability verdict |
| LS13 | T3/API | `Seeds/substances-seed.json` holds exactly 150 schema-valid draft/needsReview/inactive records; local SeedJob run ingests 150; local knowledge API serves 150 compounds; frozen count tests assert new values | positive | unverified | 008–010: batch records + validator output; 011: seed-run log + served-count transcript + updated test output |

`unverified` is blocking by default (D7). Only LS11 passes at assessment level; everything else requires its parcel's pinned run. LS13 spans four parcels (008–011); it passes only when all four close green.
