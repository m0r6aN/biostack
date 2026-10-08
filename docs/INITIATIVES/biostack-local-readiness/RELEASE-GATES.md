# Release Gates — biostack-local-readiness (A-6, LOCAL-GO only)

**2026-10-08 coordinator update:** all rows resolved; see `FINAL-HANDOFF.md` (LOCAL-GO) for the
pinned artifacts and the owner-ruling amendments. Historical statuses preserved in git.

| Gate | Description | Evidence required | Status | Blocking |
|---|---|---|---|---|
| `contracts-verified` | C1+C2 frozen shapes hold; mirrors `--check` green; no silent drift | 006 + 005 runs in `VERIFICATION.md` | passing (006+005 evidence, reviews PASS) | no |
| `scenarios-verified` | LS1–LS10 pass in LOCAL at pinned SHA | per-parcel run records in `VERIFICATION.md` | passing (001–005 evidence, reviews PASS) | no |
| `corpus-seeded` | Exactly 150 schema-valid draft/needsReview/inactive seed records; local SeedJob ingests 150; local API serves 150; frozen count tests assert new values | 007 allocation + 008–010 batch records + 011 run log + updated test output | **waived (owner)** — target amended by rulings D-C (REACHABLE-100; gap held) + D-D (consolidation → 99); 011 proved run/serving/count-tests at 100; owner: Clint Morgan; rationale: reachability-only sourcing doctrine; expiry: until KEO-73/74 sourcing decision | no (owner waiver) |
| `security-clearance` | SG-L1..L7 pass or formally waived | `SECURITY-GATES.md` + probe reports | passing (003/004/005 hostile retros; M1+R1 remediated via 012/013) | no |
| `evidence-complete` | every public/local claim maps to a pinned artifact; `EVIDENCE.md` (parcel-built) indexed | artifact index | passing (`VERIFICATION.md` sealed table + closure records) | no |
| `open-decisions-resolved` | OQ1–OQ3 carried OPEN at Gate 1 (no ruling); parcels proceed on spec-stated recommended defaults, stop on conflict; no in-parcel product decision outstanding | owner OQ ruling/waiver in `DECISIONS.md` | passing (defaults held with zero conflicts; D-C/D-D owner-ruled in COORDINATOR-DECISIONS) | no |
| `rollback-documented` | LOCAL reset = `docker compose -f docker-compose.dev.yml down -v` + volume behavior proven; no prod rollback implied | 001 reset note | passing | no (local scope) |
| `deployment-config-reviewed` | dev compose + `.env.example` reviewed; prod-compose gap (`KeonRuntime__*` not passed through) RECORDED as known limitation, not fixed here | 001 config note | passing | no (local scope) |
| `tenant-separation-verified` | owner-scoping + SQLite posture holds locally; NO production-tenant claim | 003+004 runs | passing (003 isolation matrix dual-reviewed; 004 spine/receipt reviewed) | no |

## Verdict rule

LOCAL-GO requires every row `passing` (or `waived` with owner+rationale+expiry). Any `unverified`/`failing` blocking gate → HOLD. Staging/production gates are not evaluated here and must never be inferred from local passes.
