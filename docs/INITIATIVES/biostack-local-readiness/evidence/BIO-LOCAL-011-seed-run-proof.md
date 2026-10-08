# BIO-LOCAL-011 — Seed-Run and Serving Proof (Corpus Finale)

Builder: `bio_local_011_builder`. Parcel: BIO-LOCAL-011. Branch:
`proof/bio-local-011-seed-run-proof`. Worktree:
`/home/cmorgan76/Repos/biostack-wt/BIO-LOCAL-011`.

**Dispatch supersedes the spec's stale "150" target.** The approved spec
(`docs/specs/active/BIO-LOCAL-011-seed-run-and-serving-proof.md`) was written against a
150-record charter target. Gate 2 dispatch (`GATE2-BIO-LOCAL-011.md`) and coordinator
decision D-C (`COORDINATOR-DECISIONS-2026-10-07.md`) both pin the real target at **100**
(57 pre-existing + 43 batched from A/B/C). This record follows the dispatch/decision
numbers; the spec's Verification checklist is otherwise followed exactly, substituting
100 for 150 wherever the spec says 150.

## Exact revision and environment

- Tested commit (worktree base, unchanged by this parcel's own commit until push):
  `a632cde986158d4222ae6a8b80b8c9d3cab090f2`
- UTC start: `2026-10-08T11:09:50Z` (worktree creation) — proof steps below ran
  `2026-10-08T11:1x–11:3xZ` same session.
- Tool versions: `dotnet 10.0.401`, `node v26.8.2`, `docker 29.7.2` (build a7dcaa6fdb).
- OS: Linux (devbox).

## 1. Corpus count verification (file)

```
$ python3 -c "import json; print(len(json.load(open('backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json'))))"
100
```

`backend/src/BioStack.KnowledgeWorker/Seeds/substances-seed.json` holds exactly **100**
records — matches the dispatch/decision D-C target (57 existing + 43 batched A/B/C).
Not 150; the 50-record gap is addressed in §6 below.

## 2. Builder observed output (`CorpusIdentityInventoryBuilder` / `StructuralEvaluationReportBuilder`)

Observed by instantiating both builders directly (scratch console project outside the
repo, referencing the built `BioStack.KnowledgeWorker` project, rooted at this worktree;
no repository file was modified to produce this output):

```
SeedRecordCount=100
CandidateRecordCount=16
EvidencePacketCount=78
SourceRegistryRecordCount=30
SeedCandidateOverlapCount=16
SeedOnlyCanonicalIds.Count=84
CandidateOnlyCanonicalIds.Count=0
CandidatesMissingEvidenceCanonicalIds.Count=0
EvidenceWithoutCandidateCanonicalIds.Count=62
ApprovedRightsSourceCount=7
ActiveOperationsSourceCount=7
AcquisitionEnabledSourceCount=7
RegistryAuthorizedEvidencePacketCount=2
IdentityTokenCollisions.Count=3
  collision key=chorionic-gonadotropin owners=[seed:chorionic-gonadotropin,seed:human-chorionic-gonadotropin]
  collision key=creatine owners=[candidate:creatine,seed:creatine,seed:creatine-monohydrate]
  collision key=creatine-monohydrate owners=[candidate:creatine,seed:creatine-monohydrate]
ExternalIdentifierCollisions.Count=0
ModelInvoked=False
NetworkAccessed=False
SnapshotVersion=1.0.0
Scope=repository-identity-and-provenance-metadata-only
```

StructuralEvaluationReportBuilder's `CorpusInventory.*` fields reproduce the same
values (SeedRecordCount=100, SeedCandidateOverlapCount=16, SeedOnlyCanonicalIds.Count=84,
CandidateOnlyCanonicalIds.Count=0, IdentityTokenCollisions.Count=3; all other fields
unchanged from the 57-record baseline). All non-count fields (ReportVersion, ReportKind,
Payload.Scope/EvaluationStatus/PolicyStatus/OverallVerdict, Comparison.*, ModelInvoked,
NetworkAccessed, observed/unavailable metric sets) are unchanged from the prior (57-record)
observed baseline.

### Observed-vs-007 cross-check

007's projection (`evidence/BIO-LOCAL-007-seed-gap-inventory.md`, `REACHABLE-100`
verdict: 57 existing + 43 harvestable) matches the file-count observation in §1 exactly
(100). No mismatch to reconcile.

## 3. Count-asserting test before/after table

Both frozen test files asserted the 57-record baseline. Running the full
`BioStack.KnowledgeWorker.Tests` lane against the merged 100-record corpus (before any
edit to the tests) reproduced exactly two count-assertion failures (plus two unrelated
pre-existing failures logged separately in §5):

| Field (both test files unless noted)                              | Before (asserted) | After (observed, now asserted) | Arithmetic |
|---------------------------------------------------------------------|---:|---:|---|
| `SeedRecordCount`                                                    | 57 | 100 | 57 existing + 43 batched = 100 |
| `SeedCandidateOverlapCount`                                          | 12 | 16  | observed overlap at 100-record corpus |
| `SeedOnlyCanonicalIds.Count`                                         | 45 | 84  | observed seed-only at 100-record corpus |
| `CandidateOnlyCanonicalIds.Count`                                    | 4  | 0   | observed candidate-only at 100-record corpus |
| `EvidenceWithoutCandidateCanonicalIds.Count`                         | 62 | 62  | unchanged — no edit |
| `ApprovedRightsSourceCount` / `ActiveOperationsSourceCount` / `AcquisitionEnabledSourceCount` | 7 / 7 / 7 | 7 / 7 / 7 | unchanged — no edit |
| `RegistryAuthorizedEvidencePacketCount`                              | 2  | 2   | unchanged — no edit |
| `IdentityTokenCollisions.Count`                                      | 2  | 3   | observed collision count at 100-record corpus |
| `IdentityTokenCollisions` keys (`CorpusIdentityInventoryBuilderTests` only) | `["creatine", "creatine-monohydrate"]` | `["chorionic-gonadotropin", "creatine", "creatine-monohydrate"]` | itemized overlap list directly derived from the count above |
| `IdentityTokenCollisions` owners (`CorpusIdentityInventoryBuilderTests` only) | single shared list `["candidate:creatine", "seed:creatine-monohydrate"]` asserted for every collision via `Assert.All` | three distinct per-key owner lists (see §2) asserted individually by index | the 100-record corpus no longer has all collisions sharing one owner set, so the single-list `Assert.All` shape could not be preserved verbatim; each key's owners are now itemized explicitly (still exact-value assertions, same intent: verify collision contents match observed) |
| `CandidatesMissingEvidenceCanonicalIds` / `ExternalIdentifierCollisions` | `Assert.Empty` | `Assert.Empty` | unchanged — both still empty |
| `ModelInvoked` / `NetworkAccessed`                                   | false | false | unchanged |

All new literals above were transcribed character-exact from the saved observed output in
§2 — not hand-typed from memory. The only non-literal-substitution change is the
`IdentityTokenCollisions` owners assertion shape (single shared list → three itemized
per-key lists), made necessary because the observed 100-record data no longer has a
single owners list common to every collision; this is the "directly derived overlap
list" update the spec and dispatch explicitly permit, not a change to unrelated test
logic.

Diff scope (`git diff --stat`), confirming only the two allowed test files changed and
no logic outside count literals / the overlap list above:

```
 .../CorpusIdentityInventoryBuilderTests.cs         | 29 ++++++++++++++--------
 .../StructuralEvaluationReportBuilderTests.cs      | 11 ++++----
 2 files changed, 24 insertions(+), 16 deletions(-)
```

## 4. Local SeedJob run (Postgres, parcel-local, disposable)

The KnowledgeWorker is **Npgsql-only** unconditionally (`ProductionSafetyGuard.
EnforcePostgresOnly` rejects SQLite-shaped connection strings regardless of
environment) — SeedJob cannot run against `docker-compose.dev.yml`'s SQLite dev volume.
Per the spec's "SQLite dev volume **or** parcel-local DB" option, this run used a
disposable, parcel-local Postgres container, started via `newgrp docker`:

```
$ newgrp docker <<< "docker run -d --name biostack-bio-local-011-pg \
    -e POSTGRES_DB=biostack_bio_local_011 -e POSTGRES_USER=biostack \
    -e POSTGRES_PASSWORD=<redacted-local-synthetic-password> -p 55432:5432 postgres:17-alpine"
```

Not the shared `docker-compose.yml` Postgres (which was occupied on `5432` by an
unrelated running container) and not Postgres production/staging. Local-only,
disposable, torn down in cleanup (§8).

### Command (live, non-DryRun — the proving run)

```
$ export ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=biostack_bio_local_011;Username=biostack;Password=<redacted>"
$ dotnet backend/src/BioStack.KnowledgeWorker/bin/Release/net10.0/BioStack.KnowledgeWorker.dll \
    --Worker:RunMode=Seed --Worker:DryRun=false --Worker:SeedFilePath=Seeds/substances-seed.json \
    --environment Development
```

### Log excerpt (informational lines only; full transcript saved locally at
`/tmp/seedjob_live.log` during the session, not committed — SQL parameter values are
`?`-redacted by the configured logger and contain no secrets)

```
[Startup] Verifying Postgres connectivity...
[Startup] Postgres connectivity verified.
[Startup] Ensuring database schema exists...
[Startup] Database schema ready.
[IngestionWorker] Starting one-shot — RunMode=Seed DryRun=False MaxBatchSize=50 SeedFile=Seeds/substances-seed.json ScopeHint=(none)
[SeedJob] Starting — DryRun=False MaxBatchSize=50 SeedFile=Seeds/substances-seed.json
[SeedJob] Batch 1: 50 record(s)
[SeedJob] Batch 2: 50 record(s)
[SeedJob] Run complete — Scanned=100 Created=100 Updated=0 Unchanged=0 SkippedUnpromoted=0 FlaggedForReview=76 Failed=0 DryRun=False
[IngestionWorker] One-shot complete — requesting shutdown.
```

`Scanned=100 Created=100 ... Failed=0` — all 100 schema-valid records ingested, every
one stamped `ops.lastChangeType=seed` (SeedJob's `ChangeType` is the fixed string
`"seed"`; the ingested row count matches the seed file count in §1 exactly). SeedJob is
intentionally ungated (`IngestionJobBase.EvaluatePromotionGate` base implementation
approves every record) — `SkippedUnpromoted=0` is expected, not a gate bypass bug.
`FlaggedForReview=76` means 76 of the 100 records carry `ops.needsReview=true` in the
source seed file — see §6 (draft-status honesty) for what that means on the serving
side.

### Confirmation query (direct DB count, independent of the job's own counters)

```
$ newgrp docker <<< 'docker exec biostack-bio-local-011-pg psql -U biostack -d biostack_bio_local_011 -c "SELECT COUNT(*) FROM \"KnowledgeEntries\";"'
 count
-------
   100
(1 row)
```

### Post-seed DryRun confirmation (idempotency proof)

Re-running SeedJob in DryRun mode against the now-populated database reproduces the
exact same 100-record scan with zero deltas, confirming the live write above was
complete and the DB now holds exactly the seed file's 100 records:

```
[SeedJob] DRY-RUN summary — Scanned=100 WouldCreate=0 WouldUpdate=0 Unchanged=100 SkippedUnpromoted=0 FlaggedForReview=76 Failed=0 (no writes were made)
```

(An initial DryRun attempt against the *fresh*, schema-less database failed closed with
`Scanned=100 Failed=100` — `Program.cs` intentionally skips `EnsureCreatedAsync` under
DryRun, so a DryRun cannot bootstrap schema on a brand-new database. This is documented
worker behavior (see inline comment in `Program.cs`), not a BIO-LOCAL-011 finding; the
live run in this section is what actually seeded the database, and the post-seed DryRun
above is the honest idempotency re-check.)

## 5. Full `BioStack.KnowledgeWorker.Tests` lane (counts)

### Before this parcel's test edits (against the merged 100-record corpus, tests still
asserting 57-record-baseline literals)

```
Test Run Failed.
Total tests: 1006
     Passed: 1002
     Failed: 4
 Total time: 30.0825 Seconds
```

Four failures: the two count-asserting tests this parcel exists to fix
(`CorpusIdentityInventoryBuilderTests.Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority`,
`StructuralEvaluationReportBuilderTests.Build_CurrentArtifacts_RecordsPartialPolicyWithoutVerdict`
— both `Expected: 57 / Actual: 100`), plus two **pre-existing, out-of-scope** failures in
`ResearchJobTests` (unrelated to corpus counts; not touched — see note below).

### After this parcel's test edits (same corpus, same commit base, tests updated)

```
Test Run Failed.
Total tests: 1006
     Passed: 1004
     Failed: 2
 Total time: 8.3990 Seconds
```

Both count-asserting tests now pass. The same two `ResearchJobTests` failures remain,
untouched, as required ("any OTHER failing test: record it, do not fix it").

### Pre-existing, out-of-scope failures (recorded, not fixed, not touched)

- `BioStack.KnowledgeWorker.Tests.ResearchJobTests.PromotionImportDryRunJob_Succeeds_For_Safe_Create_Preview`
  — `Assert.Equal() Failure: Expected: 1 / Actual: 0`.
- `BioStack.KnowledgeWorker.Tests.ResearchJobTests.ResearchJob_Applies_Review_Decision_To_Promotion_Manifest`
  — `Assert.Equal() Failure: Strings differ. Expected: "create" / Actual: "update"`.

Both are in `ResearchJobTests.cs`, which is **not** an allowed surface for this parcel
(`backend/src/BioStack.KnowledgeWorker/Jobs/ResearchJob.cs` and its tests belong to the
Research/promotion pipeline, out of BIO-LOCAL-011's scope). These are not count-class
assertions and not caused by the two test-file edits in this parcel — they reproduce
identically before and after the edits, against the unmodified `ResearchJob.cs` and
unmodified `ResearchJobTests.cs`. Reported here per the mandate; left for the owning
parcel/initiative to triage.

### Focused filter runs (fixture-based tests — untouched fixture proof)

```
$ dotnet test --filter "FullyQualifiedName~IngestionPipelineTests|FullyQualifiedName~SubstanceRecordValidatorTests"
Test Run Successful.
Total tests: 7
     Passed: 7
```

`backend/tests/BioStack.KnowledgeWorker.Tests/Fixtures/substances-seed.json` (the
independent synthetic sample these tests exercise) is untouched by this parcel —
confirmed by `git diff --name-only` showing only the two allowed test files changed
(§3), not the fixture.

## 6. Served-count transcript + draft-status honesty (SG-L8, partial)

### Boot

API built in Release and run directly (not via `docker-compose.dev.yml`, which is
pinned to SQLite — see §4) against the **same** parcel-local Postgres the SeedJob wrote
to, so the served count reflects a live database read, not the fixture and not staging:

```
$ export ASPNETCORE_ENVIRONMENT=Development
$ export ASPNETCORE_URLS=http://127.0.0.1:55100
$ export ConnectionStrings__DefaultConnection="Host=localhost;Port=55432;Database=biostack_bio_local_011;Username=biostack;Password=<redacted>"
$ dotnet backend/src/BioStack.Api/bin/Release/net10.0/BioStack.Api.dll
...
Now listening on: http://127.0.0.1:55100
Hosting environment: Development
```

(`ASPNETCORE_ENVIRONMENT=Development` + a Postgres-shaped `ConnectionStrings:DefaultConnection`
causes `DatabaseProviderResolver.IsPostgres` to select the Npgsql provider even outside
Production — confirmed by the EF `CREATE TABLE`/interaction-hint bootstrap log lines
on startup, which only run on first connection against this fresh container.)

### Request + response (unauthenticated — this is the public knowledge endpoint; no
`Authorization` header sent)

```
$ curl -s -o /tmp/compounds_response.json -w "HTTP_STATUS=%{http_code}\n" \
    http://127.0.0.1:55100/api/v1/knowledge/compounds
HTTP_STATUS=200
```

```
$ python3 -c "import json; d=json.load(open('/tmp/compounds_response.json')); print(len(d))"
100
```

**`GET /api/v1/knowledge/compounds` serves exactly 100 compounds** — matching the seed
file count (§1) and the ingested DB row count (§4) exactly. No mismatch.

### Draft-status honesty finding (SG-L8, partial — recorded, not fixed)

Per the spec's required check: "note how unreviewed records present (or are withheld) on
public vs authenticated surfaces; any published-reading of a draft record is an SG-L8
finding, not a pass."

The ingestion run in §4 reported `FlaggedForReview=76` — 76 of the 100 seed records
carry `ops.needsReview=true`. The persisted `KnowledgeEntry` entity
(`backend/src/BioStack.Domain/Entities/KnowledgeEntry.cs`) has **no review-status,
draft, or publication-state field at all** — the `ops.needsReview` flag present on the
source seed record is not carried into the serving table by the canonicalization
pipeline. `DatabaseKnowledgeSource.GetAllCompoundsAsync` reads `KnowledgeEntries`
unfiltered, and `KnowledgeService.GetAllCompoundsAsync` groups/maps every row to the
public response with no review-status filter or label field.

Observed directly in the public (unauthenticated) response above: `Semaglutide`
(one of the 76 `needsReview` records) is served with
`mechanismSummary: "Mechanism has not been reviewed for this draft record."` and
`notes: "Pilot regulatory evidence packet requires human review before publication. |
Compiled draft requires human review before publication."` — i.e. the fact that this is
an unreviewed/draft record is only present as incidental free text inside fields a
client has no structural reason to parse for status, not as a dedicated status field,
and not withheld. A scan of all 100 served records for "review"/"draft" substrings in
`notes`/`mechanismSummary` found only 61 of the 100 (not all 76 `FlaggedForReview`
records) carrying any such textual hint — so the free-text signal is also incomplete,
not just structurally absent.

**Finding: this is a published-reading of draft records — an SG-L8 finding, not a
pass.** `/api/v1/knowledge/compounds` (public, unauthenticated) presents all 100 records,
including the 76 unreviewed ones, indistinguishably from the 24 that have completed
review, with no structural draft/reviewed distinction and only an inconsistent,
incidental textual hint for some of them. There is no authenticated surface in this
codebase that serves a *different* (filtered or labeled) view of the same data to
compare against — the public endpoint is the only compound-listing surface, and it does
not vary by auth state. This finding is **recorded, not fixed**: `SeedJob`,
`DatabaseKnowledgeSource`, `KnowledgeService`, and `KnowledgeEntry` are all outside this
parcel's allowed surfaces (forbidden: "Editing any seed, schema, loader, builder,
SeedJob, fixture, API, frontend, or contract file"), and BIO-LOCAL-002/003 (serving
baseline honesty) are listed as dependencies, not surfaces, here. The remediation (either
withholding `needsReview` records from the public endpoint, or carrying a structural
status field through to `KnowledgeEntry`) belongs to a future, separately-ratified
parcel.

## 7. `git diff --check` and diff-scope proof

```
$ git diff --check
(no output — clean)

$ git status --porcelain
 M backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs
 M backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs
?? docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-011-seed-run-proof.md
```

Exactly the three allowed files: the two count-test files (count literals + the one
directly-derived overlap-list restructure in §3) and this evidence file. No seed, schema,
loader, builder, SeedJob, fixture, API, or frontend file was touched.

## 8. Redaction attestation

- No production, staging, or shared connection string, credential, or secret appears
  anywhere in this record, in the worktree, or in any committed file. The Postgres
  password used for the disposable local container
  (`biostack-bio-local-011-pg`) is a synthetic, locally-generated development value,
  never reused outside this container, and is redacted above (`<redacted>`) out of
  caution even though it has no production validity.
- The seed-run log excerpt above is informational-line-only; the full transcript (which
  includes EF Core's parameterized SQL logging) was reviewed before excerpting — all bind
  parameter values are logged as `?` by the configured logger, so no compound data,
  connection strings, or secrets were exposed in the retained excerpt.
- No model was invoked and no network access occurred during the builder/report runs
  (`ModelInvoked=False`, `NetworkAccessed=False` — confirmed directly from the observed
  snapshot in §2, not merely asserted).
- The seed run, DB, and API boot were entirely local (`localhost`/`127.0.0.1` only); no
  external network calls were made by the worker or API during this proof beyond the
  loopback Postgres connection.

## 9. Corpus gap (50 records below the original 150 charter target)

Per decision D-C (`docs/INITIATIVES/COORDINATOR-DECISIONS-2026-10-07.md`): BIO-LOCAL-007
returned `VERDICT: REACHABLE-100` (57 existing + 43 harvestable from the evidence-packet
inventory; a further 50 records to reach the original 150-record charter target would
require **new source acquisition**, which was out of scope for 007–010). The owner
selected option (a): seed the reachable 100 now (this parcel proves exactly that), and
hold the 50-record gap pending future sourcing.

- **Gap size:** 50 records (150 charter target − 100 reached = 50).
- **Status:** HELD. Not authorized for sourcing in this parcel or any parcel to date.
- **Path to close:** new source acquisition under the KEO-73/74 gates path. Per D-C: "New
  sourcing for the gap remains unauthorized until KEO-73/74 gates clear." This parcel
  does not open, advance, or pre-authorize that path — it only records the gap for the
  corpus-count record, per D10's in-parcel rule ("The 50-record gap gets an explicit gap
  record in BIO-LOCAL-011's evidence").
- **Unknown-honest doctrine:** unaffected by this gap record — draft/needsReview/inactive
  dispositions on the 100 seeded records are untouched by this parcel (see §6); no claim
  about the un-sourced 50 records is made or implied here.

## Reviewer replay notes

- §2's builder output was produced by a standalone console project outside the repository
  tree (`/tmp/bio011-scratch`, not committed), referencing
  `backend/src/BioStack.KnowledgeWorker/BioStack.KnowledgeWorker.csproj` as a
  `ProjectReference` and constructing `CorpusIdentityInventoryBuilder(repoRoot)` /
  `StructuralEvaluationReportBuilder(repoRoot)` directly with this worktree's absolute
  path — equivalent to what the xUnit tests do internally (`LocateRepositoryRoot()`),
  just without stopping at the first `Assert.Equal` failure. No repository file was
  edited to produce it.
- §4/§6's Postgres container, SeedJob run, and API boot can be reproduced exactly by
  repeating the commands shown, against a fresh disposable Postgres container on any free
  local port.
- The `IdentityTokenCollisions` owners restructure in §3 is the one place this record
  asks a reviewer to look closely: confirm the three keys/owner-lists in the new test
  match §2's observed output character-for-character, and that no other assertion
  shape changed.

## Session Handoff

- Starting commit: `a632cde986158d4222ae6a8b80b8c9d3cab090f2` (origin/main at worktree
  creation).
- Ending commit: this parcel's commit on `proof/bio-local-011-seed-run-proof` (see PR).
- Files changed: `backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs`,
  `backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs`,
  `docs/INITIATIVES/biostack-local-readiness/evidence/BIO-LOCAL-011-seed-run-proof.md`.
- Commands run: see §1–§7 above (file count, builder scratch run, SeedJob DryRun+Live+
  post-seed DryRun, full test lane before/after, focused fixture filter, API boot, curl).
- Tests passed: 1004/1006 (full `BioStack.KnowledgeWorker.Tests` lane, after edits); 7/7
  (focused `IngestionPipelineTests` + `SubstanceRecordValidatorTests` filter).
- Tests failed: 2 — both pre-existing, out-of-scope `ResearchJobTests` failures (§5),
  recorded not fixed.
- Decisions needed: none from this builder; the SG-L8 draft-honesty finding in §6
  requires a future, separately-ratified parcel to remediate (owner/coordinator to scope).
- Blockers: none.
- Next safe action: coordinator/reviewer (`bio_local_011_review_1`) replay per the
  Verification Plan in the spec; Gate 3 merge decision is the owner's.
- Do not touch: any file outside the three allowed surfaces; the 50-record gap remains
  HELD (no new sourcing) pending KEO-73/74.
