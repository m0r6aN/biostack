# Production Operations Runbook — backup/restore, monitoring, rollback

**Scope:** biostack.cc on Azure Container Apps (API + web, ACR images, OIDC deploys via `.github/workflows/deploy.yml`). Authored 2026-08-28 against `infra/azure/deploy-container-apps.ps1` and the deploy workflow. Dry-run status per section. Secrets never appear in this file; commands reference `$RG` (resource group) and app names as provisioned.

## 0. FIRST: determine the live database provider — open question, release-gating

`deploy-container-apps.ps1` defaults to `DatabaseProvider=sqlite` with `ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db` and **defines no Azure Files storage mount**. `deploy.yml` only runs `az containerapp update --image ...`, which replaces revisions.

> **If production was provisioned with the SQLite default, user data lives on ephemeral revision storage and every image deploy or revision restart can destroy it.** This must be answered before any backup claim is honest.

Check (read-only):
```
az containerapp show -n biostackmissionctrl-api -g $RG --query "properties.template.containers[0].env[?name=='Database__Provider']"
az containerapp show -n biostackmissionctrl-api -g $RG --query "properties.template.volumes"
```
- `Database__Provider=postgresql` → follow §1-A. SQLite + a volume → §1-B. SQLite + no volume → **stop; migrate to PostgreSQL Flexible Server or attach an Azure Files mount before launch.**

## 1. Backup / restore

### 1-A PostgreSQL (target state)
- Enable automated backups on the Flexible Server (7–35 day PITR) and confirm: `az postgres flexible-server show -n <server> -g $RG --query backup`.
- Weekly logical backup to a storage account: `pg_dump --format=custom "$CONN" > biostack-$(date +%F).dump`, uploaded with `az storage blob upload`, 90-day lifecycle policy.
- **Restore drill (quarterly):** restore PITR to a new server, point a staging Container App revision at it, run the deterministic verifier (`BioStack.ProtocolOperationsExportBundleVerifierCli`) against a known export bundle, record the receipt in `.audit/`.
- Spine integrity: after any restore, run the hash-chain verification (tamper-evident spine, PR #ec6b759) before re-enabling traffic — a restored DB with a broken chain must fail closed.

### 1-B SQLite (only acceptable with an Azure Files mount)
- Nightly snapshot: Container Apps job mounting the same share: `sqlite3 /app/data/biostack.db ".backup /app/data/backups/biostack-$(date +%F).db"`, then copy to blob storage. Never file-copy a hot SQLite db without `.backup` — WAL corruption.
- Restore: stop revision traffic, replace the file, restart, run spine verification, re-enable.

**Dry-run status:** commands validated for syntax only; no live execution from this session (requires prod credentials — Clint-only).

## 2. Monitoring

- **Cold-start P0 symptom (observed 2026-08-27/28):** first API request after idle exceeds 30 s; `/tools` renders "Compound search is temporarily unavailable" to first-touch visitors. Fix candidates: `az containerapp update -n <api-app> -g $RG --min-replicas 1` (keeps one warm replica; small always-on cost) and a health-probe warmup.
- Log Analytics is built into the Container Apps environment. Baseline alerts (all via `az monitor scheduled-query create`):
  1. Availability: `/health` non-200 over 5 min (already exercised by the deploy smoke — make it continuous with an Availability Test against `https://biostack.cc/health` + `/api/v1/knowledge/compounds` HEAD).
  2. p95 request latency > 2 s over 15 min.
  3. Container restarts > 3 per hour (revision crash-looping).
  4. 5xx rate > 1% over 15 min.
  5. Stripe webhook failures (log signature-verification and idempotency rejects at Warning; alert on any sustained rate) — feeds the billing lane.
- Route alerts to email (morganclint76@gmail.com action group) until a paging tool exists.

**Dry-run status:** alert definitions drafted; creation requires prod subscription access (Clint-only).

## 3. Rollback

Deploys are immutable SHA-tagged images, so rollback is a revision flip — no rebuild:
```
az containerapp revision list -n <api-app> -g $RG -o table          # find last-good revision
az containerapp ingress traffic set -n <api-app> -g $RG \
  --revision-weight <last-good-revision>=100                        # instant traffic shift
```
Same for the web app. Verify with the pinned smoke used in deploy.yml (`scripts/verify-containerapp-deployment.mjs`).

**Rollback triggers (decide BEFORE deploying):** failed pinned smoke; 5xx > 5% in the 15 min after cutover; spine hash-chain verification failure; any Class-boundary regression on the public knowledge response (re-run the `.audit/prod-bpc157-*` probe).

**Database caveat:** revision rollback does NOT roll back migrations. Migrations are hand-written (repo convention) — every migration must be backward-compatible one release, so N-1 code runs against N schema. Destructive migration + rollback = restore from §1 instead.

## 4. Deploy-time checklist (append to PR template once adopted)
1. `.audit` probe for public knowledge boundary still passes on staging.
2. Migration (if any) reviewed for N-1 compatibility.
3. Last-good revision name noted in the PR description.
4. Post-deploy: pinned smoke + `/tools` first-load check (cold-start) + BPC-157 boundary probe.
