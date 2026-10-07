# Builder Handoff — Governance Hardening Session

| Field | Value |
|---|---|
| Date | 2026-08-03 |
| Baseline | `ec6b759` feat(governance): tamper-evident spine with hash chain |
| Verification | `dotnet build` clean; **1975 tests, 0 failed, 5 skipped** across all five test projects |
| Companion | `GOVERNANCE-ENFORCEMENT-FINDINGS.md` (full findings + rationale) |

---

## 1. Do this first — uncommitted work

**Done.** See `docs/guidance/OPS-SPINE-AND-SIDECAR.md` for:

- Applying F3 / F3+ migrations on real databases
- Spine checkpoint signing key + export
- Research sidecar path + `uv sync` after rename

---

## 2. Traps — read before touching these areas

These cost real time this session. Each is a place where the obvious action is the wrong one.

### Do NOT run `dotnet ef migrations add`

`BioStack.Api/ProductionMigrationBaselineConfiguration.cs` states the convention: *"Migrations in this repository are intentionally hand-written and the central snapshot is intentionally minimal."* The committed `BioStackDbContextModelSnapshot.cs` is **126 lines describing one entity**.

Run the scaffolder and EF compares the full model against that deliberately-incomplete snapshot, concludes 25 tables don't exist, and emits `CreateTable` for `AppUsers`, `KnowledgeEntries`, `SpineEntries` and the rest — while rewriting the snapshot to ~1900 lines. Applying it to a live database would try to create tables that already exist.

Hand-write migrations. Copy the shape of `20260626000000_AddReceiptClassToSpine.cs`: hand-picked timestamp with trailing zeros, `type: "TEXT"` for strings, and a partial `.Designer.cs` carrying the `[Migration]` attribute. Leave the central snapshot alone.

### Timestamps in the Spine hash chain

`SpineChain.Stamp()` normalises to UTC at **microsecond** precision. Do not "simplify" it back to `ToString("O")`.

Two failures hide there, and both were caught only by running the tests against SQLite:
- `DateTimeKind` does not survive the SQLite round-trip. A value written as `Utc` reads back `Unspecified`, and `"O"` renders those differently (`...Z` vs. no `Z`), so **every entry rehashes to a different digest than it was written with** — the chain never verifies.
- .NET ticks are 100ns; PostgreSQL timestamps are microsecond-resolution. Sub-microsecond precision is truncated on the way back, so the same bug reappears on the other provider.

### `PreviousEntryHash` is deliberately NOT unique

`AddColumn` backfills every pre-existing row with the same default, so a unique constraint there fails on the second legacy row — the migration cannot apply to any populated database. Uniqueness on `SequenceNumber` is what enforces chain linearity (two concurrent appends claim the same slot; one loses at the DB). `EntryHash` uniqueness is safe because it derives from the already-unique `ReceiptUri`.

### Doctrine patterns live in exactly one place

`DoctrineRuleset` is the single source for banned-phrase patterns. `DoctrineSanitizer` and `PolicyGate` both delegate to it. They previously kept separate copies hand-synced by a comment and had already drifted (16 patterns vs. 9), with the copy-guard suite exercising only one of them. `DoctrineRulesetParityTests` now fails CI on divergence — do not reintroduce a local pattern array.

### Two doctrine tiers — do not collapse them

- `PersonalizedDirection` — prohibited **unconditionally**. A citation never redeems an imperative.
- `AttributionSensitiveClaim` (`is safe`, `cures`, `proven to`, `will treat`) — prohibited **only when unattributed**. As BioStack's own assertion these are Class D; as a report of a cited finding they are Class A.

`OutputAttribution` defaults to `Unattributed` — callers must *prove* attribution. Exactly one production call site uses the permissive tier: `EvidenceGate` Check 8, which is safe **only because Check 6 already rejected anything without citations**. If you reorder those checks, that safety property is gone.

### Container validation is on in every environment

`Program.cs` sets `ValidateOnBuild` and `ValidateScopes` globally (the default is Development-only). A missing or captively-scoped registration now fails at startup instead of as a 500 on first request. If startup breaks after adding a service, that is the check working. `GovernanceDependencyInjectionSmokeTests` resolves the governed-output path — **add new gates to its list**.

### The sidecar is Python

`backend/research-sidecar` is a FastAPI/uv project, not .NET. It is not in `BioStack.sln`. Its tests run under `pytest`, not `dotnet test`. The former path `backend/src/BioStack.Research` predates the language decision and has been renamed.

---

## 3. Pending manual steps

**Done:** sidecar moved to `backend/research-sidecar` (own commit). After pull, recreate the venv if an editable install still points at the old path:

```bash
rm -rf backend/research-sidecar/.venv && cd backend/research-sidecar && uv sync
```

Leave the `biostack-research-sidecar` strings in `ScientificResearchSidecarClient.cs` and `ScientificResearchCandidateStagingService.cs` alone — those are the provider identity in the artifact contract, not paths.

---

## 4. Open findings, in priority order

| ID | Finding | Notes |
|---|---|---|
| **S2** | Sidecar privacy boundary checks key **names**, not values | `privacy.py` walks dict keys against a 20-name denylist. Health data in `subject_name` (256 chars of free text) passes cleanly. Constrain `subject_name` to a compound-identifier shape and whitelist `known_identifiers` keys. |
| **S5** | `202 Accepted` blocks the event loop | `app.py:197` runs the job inline in the request handler. One long job freezes the sidecar including `/health`. `max_concurrent_research_jobs`, `job_ttl_seconds`, and both execution-timeout settings are define-only. |
| **F3+** | Spine is tamper-**evident**, not tamper-**proof** | **CLOSED (foundation)** — signed checkpoints + cadence; configure `SpineCheckpoint:SigningKey` and export manifests off-box for real external anchor. |
| **S3** | Hosted-fallback clause has four flags and no choke point | No inference path exists yet, so the contract clause is vacuously satisfied. Add `assert_hosted_inference_allowed()` and its test *now*, while it costs nothing. |
| **S4** | Sidecar output is structurally non-promotable | `evidence_class="unknown"` hardcoded, `source_locations` never populated. Correct as posture, but the ratification record presents it as policy when it is currently also a structural impossibility. |
| **S6** | Every terminal status is `PARTIAL` | Full success, total tool failure, and no-steps-executed are indistinguishable by status. `COMPLETED` and `PENDING_REVIEW` are unreachable. |

---

## 5. Governance decisions still owed to humans

Neither is a code question. Both belong in the ratification package.

**Legacy Spine rows cannot be retro-chained.** Their hashes were never computed, and inventing them is exactly the forgery the chain detects. The migration assigns unique placeholders so constraints can be created; `VerifyChainAsync` will report the first such row as a mismatch. That is intended — the migration point is the chain's effective genesis. The alternative is export-and-reseed.

**`AllowUnanchoredSafetyReceipts` defaults to `true`.** When Keon cannot anchor a non-effecting safety receipt, a clearly-labelled local row is written (`biostack://unanchored-receipt/...`, policy hash `unanchored-local`). Set `false` to degrade to "warning surfaced, nothing recorded". Effect-bearing receipts always fail closed regardless.

Public Class B/C UX is unblocked under the ratified contract when outputs obey copy-guards and review gates (see `RATIFICATION.md`). Nothing in the F3+/sidecar sessions enabled a *new* public surface beyond that ratification.
