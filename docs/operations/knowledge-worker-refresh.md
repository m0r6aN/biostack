# KnowledgeWorker Refresh Runbook

`RunMode=Refresh` re-ingests `Seeds/substances-seed.json` and upserts each record into
`KnowledgeEntries`. As of this runbook, Refresh is **gated**: a record is upserted only
when the Refresh-specific promotion gate accepts its applicable review disposition as
`approve-for-promotion` with `clearsSoftPromotionBlockers: true`
(`backend/src/BioStack.KnowledgeWorker/Pipeline/PromotionGate.cs`). Every other
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
  rollback mechanism of its own (§6 below) — retain a backup or an export of pre-Refresh values for recovery.
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
  Get-ChildItem Env: |
    Where-Object { $_.Name -like 'Worker__*' -or $_.Name -like 'ConnectionStrings__*' } |
    Select-Object Name, @{Name='Present'; Expression={ $true }}
  ```
  This shows presence only, never values. Clear unexpected Worker overrides before
  continuing; confirm the intended connection string privately without printing it.

## 1. Set the connection string

```powershell
$env:ConnectionStrings__DefaultConnection = '<value from the db-conn-string secret>'
```

## 2. DryRun — read the plan before writing anything

```powershell
cd D:\Repos\BioStack
dotnet backend\src\BioStack.KnowledgeWorker\bin\Release\net10.0\BioStack.KnowledgeWorker.dll `
  --Worker:RunMode=Refresh --Worker:DryRun=true --Worker:SeedFilePath=Seeds\substances-seed.json
if ($LASTEXITCODE -ne 0) { throw "DryRun failed; do not run Live Refresh." }
```

Run this **from the repository root** — the promotion gate reads
`research/review-decisions/review-decision-batch-*.json` relative to the current
directory (`Worker:ReviewDecisionDirectory`, default `research/review-decisions`), not
relative to the worker's `bin` folder.

With effective `DryRun=true`, startup skips schema creation and interaction-hint seeding,
and the Refresh preview reads detached database rows without saving them. This is a
database-read-only preview, not a rehearsal of live startup writes. The preview tests
and job-level integration tests do not execute the full Program startup path; they are
not proof of a production run or a full-host database write trace. The console output
ends with a summary line and a per-record table, for example:

```
[RefreshJob] DRY-RUN summary — Scanned=57 WouldCreate=5 WouldUpdate=1 Unchanged=8 SkippedUnpromoted=43 FlaggedForReview=0 Failed=0 (no writes were made)
[RefreshJob] DRY-RUN per-record plan:
Name                        Action           Reason
--------------------------  ---------------  ------
Liraglutide                  unchanged
Toremifene                  would-insert
Raloxifene                  would-update
Creatine monohydrate        skip-unpromoted  no review decision on file
Vitamin D3                  skip-unpromoted  latest decision is request-changes
Tamoxifen                   skip-unpromoted  latest decision is request-changes
...
```

**Read the table, not just the summary line.** The summary counters are now honest
(computed by actually comparing each record against the database, read-only — see
`DatabaseKnowledgeSource.PreviewUpsertAsync`), but the table is what tells you *which*
compounds fall into each bucket.

### Check the revision's expected plan

For the corrected Refresh gate, an offline projection of the seed and review corpus
at original PR revision `2ef5e853e0bbe99c3f217efcce1ddc693ece8681` selects **14 of
57 records and skips 43**. The old historical-approval predicate selected 18/39;
that is superseded for this corrected gate. The projection is not a database preview,
scientific approval, or permission to publish. Reconcile the actual gate, review files
and seed revision before a run rather than treating these counts as permanent.

- The corrected gate uses the latest applicable disposition, excluding
  `resolve-review-items`. A later claim-only decision does not carry an older promotion
  forward. At the latest timestamp every applicable decision must promote with blockers
  cleared; a tied non-promotion decision denies. These are conservative Refresh rules,
  not changes to the shared research index or issued review decisions.
- With that corpus and no validation or preview failures,
  `WouldCreate + WouldUpdate + Unchanged = 14` and `SkippedUnpromoted = 43`.
  Fourteen eligible records does not mean fourteen writes; the sample above is illustrative.
- The fourteen eligible names are Liraglutide, Dulaglutide, Exenatide, Tesamorelin,
  Sermorelin, Ipamorelin, MOTS-c, Raloxifene, Thymosin alpha-1, AC-262536,
  Lasofoxifene, LGD-3303, LL-37 and Toremifene. Semaglutide, Enclomiphene,
  Spermidine and Urolithin A no longer qualify under the corrected ordering.
- Creatine monohydrate has no exact-name review decision in that corpus. Vitamin D3
  and Tamoxifen have requested changes; neither an older approval nor a claim-only
  review makes them eligible under the corrected gate.
- Earlier audit observations described missing Toremifene, Lasofoxifene, LL-37,
  AC-262536 and LGD-3303 rows and a Raloxifene placeholder. Those are historical
  observations, not current endpoint checks. An `unchanged` Raloxifene can reflect a
  subsequent successful refresh; inspect the actual approved seed and existing row.
- Stop on a nonzero native exit, any `Failed` count, or an unexpected name/action/reason.
  Read failure rows and diagnostics as well as the totals; do not proceed on a partial
  plan. Resolve the discrepancy before obtaining a new successful preview.

### If DryRun aborts immediately (before any table)

Inspect the actual error and native exit. Gate loading can fail on missing, empty,
invalid or schema-invalid configured review inputs. Database connectivity or seed
loading/validation can also fail; an abort is not automatically a gate-path problem.
Resolve the reported cause without relaxing the gate, then obtain a successful preview.

## 3. Live Refresh

Only after DryRun exits successfully, reports `Failed=0`, and the complete plan matches
the expected approved inventory:

```powershell
# Re-check environment precedence (step 0) once more — do this immediately before running,
# not just once at the start of the session.
dotnet backend\src\BioStack.KnowledgeWorker\bin\Release\net10.0\BioStack.KnowledgeWorker.dll `
  --Worker:RunMode=Refresh --Worker:DryRun=false --Worker:SeedFilePath=Seeds\substances-seed.json
```

The live run applies the same gate: skipped records are logged the same way DryRun showed
them, and the run-complete summary line reports `SkippedUnpromoted` alongside
`Created`/`Updated`/`Unchanged`.

**Live startup also performs work outside that table:** `Program.cs` calls
`EnsureCreatedAsync`, interaction-hint schema bootstrap and default hint seeding before
the Refresh job. The plan covers KnowledgeEntries reconciliation only; it does not
predict those schema/hint writes. Even a plan with no eligible records does not suppress
live startup bootstrap. Account for this broader write scope in backup and verification.

## 4. Development-only override

`Worker:AllowUnpromoted` is for a disposable local development database, not production.
It bypasses review selection, so leave it disabled for this procedure. The remediated
startup guard requires both the Development environment and a loopback database host;
non-local use is rejected. There is no supported remote promotion-bypass procedure.

## 5. Verify afterward

```powershell
# 1. Row count and names via the public API.
(Invoke-RestMethod https://biostack.cc/api/v1/knowledge/compounds).Count
$names = 'Raloxifene','Toremifene','Lasofoxifene','LL-37','AC-262536','LGD-3303'
foreach ($n in $names) {
  try { Invoke-RestMethod ("https://biostack.cc/api/v1/knowledge/compounds/" + [Uri]::EscapeDataString($n)) | Out-Null; "OK: $n" }
  catch { "CHECK FAILED: $n (inspect HTTP status or transport failure; do not assume 404)" }
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
including any row previously reported as a placeholder. Do not infer content correctness
from a successful request alone.

## 6. Rollback notes — what Refresh can and cannot undo

- **Idempotent for unchanged records.** Unchanged KnowledgeEntries are not saved again.
  This does not exempt live startup bootstrap from the separate scope described above.
- **Additive for new compounds**, and it **never deletes rows** — a compound removed from
  the seed file is simply left alone in the database, stale but not retracted.
- **Not a safe merge for existing compounds.** `DatabaseKnowledgeSource.ApplyChanges`
  overwrites every mapped field (including list fields like `Aliases`, `Benefits`,
  `AvoidWith`, `DrugInteractions`) with the seed's value whenever they differ — it does not
  preserve direct edits to those KnowledgeEntries fields if the seed differs. Personal
  compounds edited through the compounds UI are separate CompoundRecords, handled by
  CompoundService/ICompoundRecordRepository; this upsert does not overwrite them.
- **No built-in undo.** There is no versioning or audit table for this upsert path.
  Recovery after an unwanted write means restoring from the Postgres snapshot taken in
  step 0, or manually re-applying the pre-Refresh field values from a database export.
- The promotion gate reduces blast radius going forward (it can no longer push all 57 seed
  records unconditionally), but it does not change the overwrite/recovery behavior for records it
  allows through, or the separate startup bootstrap scope.
