# Knowledge schedules

Owner ruling 7 (2026-10-24): every data task runs on a schedule, covering both research on new compounds and updates to existing entries. Clinton Morgan is the sole owner, operator and decision maker.

## What runs

| Job | RunMode | Default cron (UTC) | New York time (EDT / EST) | Writes |
|---|---|---|---|---|
| `<base>-kw-plan-updates` | `ResearchPlanning` (updates only) | `0 6 * * *` daily | 02:00 / 01:00 | `research-requests/` on the share |
| `<base>-kw-plan-discovery` | `ResearchPlanning` (discovery only) | `30 6 * * 1` Mondays | 02:30 / 01:30 | `research-requests/` |
| `<base>-kw-research` | `Research` | `0 7 * * *` daily | 03:00 / 02:00 | `research-output/` (drafts, review queue, task queue) |
| `<base>-kw-council` | `CouncilReview` | `0 8 * * *` daily | 04:00 / 03:00 | `review-decisions/`, `owner-escalations/` |
| `<base>-kw-refresh-dryrun` | `Refresh`, `DryRun=true` | `0 9 * * *` daily | 05:00 / 04:00 | nothing (logs the diff) |
| `<base>-kw-refresh-apply` | `Refresh` | `30 9 * * *` daily | 05:30 / 04:30 | `KnowledgeEntries` (idempotent) |

`SourceAcquisition` remains a Manual job (`source-acquisition-jobs.bicep`) because each cycle needs its approved request, source-decision and registry inputs. Retention runs at `0 7 * * *` as before.

**Update checks.** Every entry whose `provenance.lastReviewedAt` is older than `Worker:ResearchUpdateCheckStaleDays` (default 30) gets a request, oldest first, up to `Worker:MaxBatchSize`. An entry is marked high priority when its overall grade is weak (Plausible Theory, Anecdotal Signal, Unsupported, Hype Exceeds Evidence, Contradicted or Not graded) or when its regulatory status is missing. With a cap of 50 and 158 entries, the whole corpus is covered every 4 days.

**Discovery.** Every compound in the watchlist (`Seeds/discovery-watchlist.json`, or the copy on the share at `/app/research-runtime/discovery/watchlist.json`) that is not already in the corpus, matched by name or alias, gets a `discover-*` request. Edit the watchlist to steer research.

## Worker loop (always-on or local)

```
Worker__RunMode=ResearchPlanning Worker__LoopEnabled=true Worker__RefreshIntervalHours=24 dotnet run --project backend/src/BioStack.KnowledgeWorker
```

`Worker:LoopIntervalMinutes` overrides the hour interval. A failed pass is logged and the loop continues. Run one loop process per RunMode. Cron is the production scheduler.

## Deploy

1. Build and push the image from the repo root:
   `docker build -f backend/Dockerfile.knowledgeworker -t <acr>/biostack-knowledgeworker:<tag> .`
   Then deploy it by digest.
2. Create an Azure Files share and register it as Container Apps environment storage (`researchStorageName`).
3. Seed the share:
   - copy `research/review-decisions/` to `review-decisions/`
   - copy `Seeds/discovery-watchlist.json` to `discovery/watchlist.json`
   - create an empty `evidence-packets/` folder
4. Create a user-assigned identity. Grant it **AcrPull** on the registry and **Key Vault Secrets User** on the vault.
5. Store the Postgres connection string in Key Vault, for example as secret `db-conn-string`, and pass its versionless URI as `dbConnectionStringSecretUri`. The Bicep never takes the value itself.
6. Store both council API keys in Key Vault and pass their versionless URIs as `councilReviewer0ApiKeySecretUri` / `councilReviewer1ApiKeySecretUri`, along with `councilReviewer{0,1}Provider` and `Model` (see `ai-council-review.md`).
7. Deploy:
   `az deployment group create -g <rg> -f infra/azure/knowledge-schedules.bicep -p baseName=… containerAppsEnvironmentId=… workerImage=… acrLoginServer=… identityResourceId=… researchStorageName=… dbConnectionStringSecretUri=…`
   Set `enableRefreshApply=false` to keep only the dry run.
