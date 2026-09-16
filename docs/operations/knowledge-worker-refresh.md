# KnowledgeWorker Refresh Runbook

`RunMode=Refresh` re-ingests `Seeds/substances-seed.json` and upserts each record into
`KnowledgeEntries`. As of this runbook, Refresh is **gated**: a record is upserted only
when its review decision resolves to `approve-for-promotion` with
`clearsSoftPromotionBlockers: true` (`ReviewDecisionIndex.HasPromotionApproval`,
`backend/src/BioStack.KnowledgeWorker/Pipeline/ReviewDecisionIndex.cs:75-79`). Every other
record is skipped and reported by canonical name and reason. This is a Clint-only,
production-database operation — nobody else runs this against `biostack.cc`'s database.

This procedure is written for PowerShell, run from `D:\Repos\BioStack` on Windows, against
the worker's own build output. It does not depend on a deployed Container App Job existing
(none is currently defined in `infra/` — see the A1 promotion-state audit, §4) — it assumes
you run the worker binary directly against the production connection string.

## 0. Before you start

- **Never paste the connection string into chat.** Set it as an environment variable from
  the `db-conn-string` secret, in your own PowerShell session, immediately before you need
  it, and clear it (or close the session) afterward.
- Take a Postgres snapshot/backup before the live (non-DryRun) run. Refresh has no
  rollback mechanism of its own (§6 below) — a backup is the only way back.
- Build the worker first if you haven't:
  ```powershell
  cd D:\Repos\BioStack
  dotnet build backend\src\BioStack.KnowledgeWorker\BioStack.KnowledgeWorker.csproj -c Release
  ```
- Check for stale environment variables that would silently override your flags — the
  worker's config chain applies environment variables **after** command-line args
  (`Program.cs`), so a leftover `Worker__DryRun` or `Worker__RunMode` wins over what you
  type:
  ```powershell
  Get-ChildItem Env: | Where-Object { $_.Name -like 'Worker__*' -or $_.Name -like 'ConnectionStrings__*' }
  ```
  Clear anything unexpected before continuing.

## 1. Set the connection string

```powershell
$env:ConnectionStrings__DefaultConnection = '<value from the db-conn-string secret>'
```

## 2. DryRun — read the plan before writing anything

```powershell
cd D:\Repos\BioStack
dotnet backend\src\BioStack.KnowledgeWorker\bin\Release\net10.0\BioStack.KnowledgeWorker.dll `
  --Worker:RunMode=Refresh --Worker:DryRun=true --Worker:SeedFilePath=Seeds\substances-seed.json
```

Run this **from the repository root** — the promotion gate reads
`research/review-decisions/review-decision-batch-*.json` relative to the current
directory (`Worker:ReviewDecisionDirectory`, default `research/review-decisions`), not
relative to the worker's `bin` folder.

DryRun now makes **zero writes of any kind**, including the interaction-hints bootstrap
that used to run unconditionally. The console output ends with a summary line and a
per-record table, for example:

```
[RefreshJob] DRY-RUN summary — Scanned=57 WouldCreate=5 WouldUpdate=1 Unchanged=12 SkippedUnpromoted=39 FlaggedForReview=0 Failed=0 (no writes were made)
[RefreshJob] DRY-RUN per-record plan:
Name                        Action           Reason
--------------------------  ---------------  ------
Semaglutide                 unchanged
Toremifene                  would-insert
Raloxifene                  would-update
Creatine monohydrate        skip-unpromoted  latest decision is request-changes
Vitamin D3                  skip-unpromoted  latest decision is request-changes
Tamoxifen                   skip-unpromoted  latest decision is request-changes
...
```

**Read the table, not just the summary line.** The summary counters are now honest
(computed by actually comparing each record against the database, read-only — see
`DatabaseKnowledgeSource.PreviewUpsertAsync`), but the table is what tells you *which*
compounds fall into each bucket.

### What to expect against the current promoted set (18 compounds)

Per the A1 promotion-state audit (`a1-promotion-audit/promotion-state-audit.md`, §2.1 and
§5), on the seed file as of this writing:

- **`WouldCreate`/`WouldUpdate` should cover exactly the 18 promoted compounds** that
  aren't already live with identical content — in particular the five stranded wave-005
  creates (`Toremifene`, `Lasofoxifene`, `LL-37`, `AC-262536`, `LGD-3303`, all currently
  404 on `biostack.cc`) and the Raloxifene **update** (its existing live row is a stale
  placeholder from before wave 005 — expect `would-update`, not `unchanged`, for it; if the
  table instead shows `unchanged` for Raloxifene, stop and investigate before proceeding —
  it means the placeholder content already matches the seed, which contradicts the audit).
- **`SkippedUnpromoted` should cover the other 39 seed records**, including Creatine
  monohydrate, Vitamin D3, and Tamoxifen — all three are `request-changes` as of the
  newest wave-006 re-review and must **not** go live from this run.
- If the table shows anything unpromoted under `would-insert`/`would-update` instead of
  `skip-unpromoted`, or shows a promoted compound as `skip-unpromoted`, **stop**. That
  means the review-decision index the gate loaded doesn't match what you expect — check
  which files landed under `research/review-decisions` and re-run DryRun before going
  further.

### If DryRun aborts immediately (before any table)

That's the fail-closed promotion gate: it refused to run because it could not load a
usable review-decision index (missing `research/review-decisions` directory, no matching
`review-decision-batch-*.json` files from the current directory, a file that isn't valid
JSON, or a batch that fails schema validation). The error names which check failed. Fix the
underlying problem (usually: you weren't in the repository root) — Refresh will not touch
the database with a gate it can't establish.

## 3. Live Refresh

Only after the DryRun table looks exactly as expected:

```powershell
# Re-check environment precedence (step 0) once more — do this immediately before running,
# not just once at the start of the session.
dotnet backend\src\BioStack.KnowledgeWorker\bin\Release\net10.0\BioStack.KnowledgeWorker.dll `
  --Worker:RunMode=Refresh --Worker:DryRun=false --Worker:SeedFilePath=Seeds\substances-seed.json
```

The live run applies the same gate: skipped records are logged the same way DryRun showed
them, and the run-complete summary line reports `SkippedUnpromoted` alongside
`Created`/`Updated`/`Unchanged`.

## 4. The `AllowUnpromoted` override (dev/local only)

`--Worker:AllowUnpromoted=true` disables the promotion gate entirely — every schema-valid
seed record is upserted, promoted or not. It is refused unless the connection string host
is `localhost` or `127.0.0.1`:

```powershell
# Refused — the worker will throw and exit before connecting, because the host isn't local:
dotnet ... --Worker:RunMode=Refresh --Worker:AllowUnpromoted=true
# (against ConnectionStrings__DefaultConnection=Host=biostack-prod.postgres...)
```

To force it anyway against a non-local database, also pass
`--Worker:AcknowledgeUnpromotedProduction=true`. Every use of the override is logged as a
loud startup warning. **Do not use this against the production connection string.** It
exists for local development against a disposable Postgres instance, not for bypassing
review on `biostack.cc`.

## 5. Verify afterward

```powershell
# 1. Row count and names via the public API.
(Invoke-RestMethod https://biostack.cc/api/v1/knowledge/compounds).Count
$names = 'Raloxifene','Toremifene','Lasofoxifene','LL-37','AC-262536','LGD-3303'
foreach ($n in $names) {
  try { Invoke-RestMethod ("https://biostack.cc/api/v1/knowledge/compounds/" + [Uri]::EscapeDataString($n)) | Out-Null; "OK: $n" }
  catch { "STILL MISSING: $n" }
}

# 2. Sitemap dossier count (frontend/src/app/sitemap.ts appends one /knowledge/<name> path per compound).
(Invoke-WebRequest https://biostack.cc/sitemap.xml).Content | Select-String -Pattern '/knowledge/' -AllMatches |
  Select-Object -ExpandProperty Matches | Measure-Object | Select-Object Count

# 3. Census files — these are the corpus census, not a promotion census (they compare the
#    seed against a legacy pilot-candidate list); confirm they still read 57/45/12 and have
#    not silently drifted. See promotion-state-audit.md §2.3.
#    backend/tests/BioStack.KnowledgeWorker.Tests/CorpusIdentityInventoryBuilderTests.cs
#    backend/tests/BioStack.KnowledgeWorker.Tests/StructuralEvaluationReportBuilderTests.cs
```

An HTTP 200 on a compound endpoint is necessary but not sufficient — spot-check the
content (mechanism summary, source references) against the approved seed record,
especially for Raloxifene, whose pre-existing live record was a stale placeholder before
this Refresh should have replaced it.

## 6. Rollback notes — what Refresh can and cannot undo

- **Idempotent for unchanged records.** Re-running Refresh against an unchanged seed file
  is a no-op.
- **Additive for new compounds**, and it **never deletes rows** — a compound removed from
  the seed file is simply left alone in the database, stale but not retracted.
- **Not a safe merge for existing compounds.** `DatabaseKnowledgeSource.ApplyChanges`
  overwrites every mapped field (including list fields like `Aliases`, `Benefits`,
  `AvoidWith`, `DrugInteractions`) with the seed's value whenever they differ — it does not
  preserve a hand-edit made through the compounds UI if the seed's value differs. If
  someone has edited a promoted compound live since the last Refresh, expect that edit to
  be overwritten by this run for that compound.
- **No built-in undo.** There is no versioning or audit table for this upsert path.
  Recovery after an unwanted write means restoring from the Postgres snapshot taken in
  step 0, or manually re-applying the pre-Refresh field values from a database export.
- The promotion gate reduces blast radius going forward (it can no longer push all 57 seed
  records unconditionally), but it does not change any of the above for the 18 records it
  does allow through.
