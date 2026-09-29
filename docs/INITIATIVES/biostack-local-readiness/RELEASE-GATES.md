# Release Gates — biostack-local-readiness (A-6, LOCAL-GO only)

| Gate | Description | Evidence required | Status | Blocking |
|---|---|---|---|---|
| `contracts-verified` | C1+C2 frozen shapes hold; mirrors `--check` green; no silent drift | 006 + 005 runs in `VERIFICATION.md` | unverified | yes |
| `scenarios-verified` | LS1–LS10 pass in LOCAL at pinned SHA | per-parcel run records in `VERIFICATION.md` | unverified | yes |
| `corpus-seeded` | Exactly 150 schema-valid draft/needsReview/inactive seed records; local SeedJob ingests 150; local API serves 150; frozen count tests assert new values | 007 allocation + 008–010 batch records + 011 run log + updated test output | unverified | yes |
| `security-clearance` | SG-L1..L7 pass or formally waived | `SECURITY-GATES.md` + probe reports | unverified | yes |
| `evidence-complete` | every public/local claim maps to a pinned artifact; `EVIDENCE.md` (parcel-built) indexed | artifact index | unverified | yes |
| `open-decisions-resolved` | OQ1–OQ3 carried OPEN at Gate 1 (no ruling); parcels proceed on spec-stated recommended defaults, stop on conflict; no in-parcel product decision outstanding | owner OQ ruling/waiver in `DECISIONS.md` | unverified (OQ answers proposed, not ratified) | yes |
| `rollback-documented` | LOCAL reset = `docker compose -f docker-compose.dev.yml down -v` + volume behavior proven; no prod rollback implied | 001 reset note | unverified | yes (local scope) |
| `deployment-config-reviewed` | dev compose + `.env.example` reviewed; prod-compose gap (`KeonRuntime__*` not passed through) RECORDED as known limitation, not fixed here | 001 config note | unverified | yes (local scope) |
| `tenant-separation-verified` | owner-scoping + SQLite posture holds locally; NO production-tenant claim | 003+004 runs | unverified | yes |

## Verdict rule

LOCAL-GO requires every row `passing` (or `waived` with owner+rationale+expiry). Any `unverified`/`failing` blocking gate → HOLD. Staging/production gates are not evaluated here and must never be inferred from local passes.
