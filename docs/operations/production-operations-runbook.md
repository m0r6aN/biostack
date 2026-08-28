# Production Operations Runbook — backup/restore, monitoring, rollback

**Scope:** biostack.cc on Azure Container Apps (API + web, ACR images, OIDC deploys via `.github/workflows/deploy.yml`). Authored 2026-08-28 against `infra/azure/deploy-container-apps.ps1` and the deploy workflow. Dry-run status per section. Secrets never appear in this file; commands reference `$RG` (resource group) and app names as provisioned.

## 0. FIRST: verify durable production state — release-gating

`deploy-container-apps.ps1` now defaults to and requires PostgreSQL for production unless an explicit throwaway-only SQLite override is set. `deploy.yml` only replaces revision images, so the currently deployed configuration remains authoritative and must be inspected.

> **If production was provisioned with the SQLite default, user data lives on ephemeral revision storage and every image deploy or revision restart can destroy it.** This must be answered before any backup claim is honest.

Check (read-only):
```
az containerapp show -n biostackmissionctrl-api -g $RG --query "properties.template.containers[0].env[?name=='Database__Provider']"
az containerapp show -n biostackmissionctrl-api -g $RG --query "properties.template.volumes"
```
- `Database__Provider=postgresql` → follow §1-A. SQLite + a volume → §1-B. SQLite + no volume → **stop; migrate to PostgreSQL Flexible Server or attach an Azure Files mount before launch.**

Session cookies have a separate durability boundary. Verify the API has a managed identity and all three non-secret Data Protection settings; never print cookie values, tokens, storage credentials, or secret references:

```powershell
az containerapp show -n <api-app> -g $RG --query identity
az containerapp show -n <api-app> -g $RG --query "properties.template.containers[0].env[?starts_with(name, 'DataProtection__')].{name:name,value:value}" -o table
```

Expected application name: `BioStack.Api.SessionCookie.v1`. The Blob URI must identify one object without a query/SAS. The Key Vault URI must be versionless (`.../keys/<name>`). Missing values or unusable identity permissions intentionally prevent the new API revision from becoming ready.

## 0-A. Session continuity across restart and scale-to-zero

Operator prerequisites:

1. Use an operator principal with `Contributor` plus `User Access Administrator` (or `Owner`, or an approved equivalent custom role) at the target resource-group scope so it can create the storage/vault/key resources and role assignments.
2. Enable a system-assigned identity on the API app (or attach the approved user-assigned identity).
3. Pre-create the dedicated Blob container and an enabled RSA Key Vault key with `wrapKey`/`unwrapKey`, or deploy `infra/azure/session-data-protection.bicep` after enabling the system identity.
4. Assign `Storage Blob Data Contributor` to the API identity at the dedicated container scope and `Key Vault Crypto User` at the wrapping-key vault/key scope. Wait for RBAC propagation.
5. Configure the exact three settings above. Configure `DataProtection__ManagedIdentityClientId` only for a user-assigned identity.
6. Preserve all old wrapping-key versions for the 30-day cookie lifetime plus rollback window. Never change the application name during a routine deploy.

An already-unreadable cookie whose container-local key was destroyed cannot be recovered. The acceptance session must be minted once after the durable key-ring cutover; that new cookie is then tested across restart and scale-to-zero.

Verification uses a dedicated test identity and a normal browser. Record only timestamps, revision names, replica counts, response status, and final route—never the link token or cookie value.

1. Sign in, accept the current consent version if required, open `/profiles`, and confirm `/api/v1/auth/session` reports `authenticated: true` in the browser session.
2. Record the active revision, then restart only that revision:

   ```powershell
   $REV = az containerapp revision list -n <api-app> -g $RG --query '[?properties.trafficWeight > `0`].name | [0]' -o tsv
   az containerapp revision restart -n <api-app> -g $RG --revision $REV
   ```

3. After readiness returns, refresh `/profiles` in the same browser. Pass: no sign-in redirect and the session endpoint remains authenticated.
4. If the configured API scale rule has `minReplicas=0`, leave the app idle until this read-only command returns `[]` for the active revision:

   ```powershell
   az containerapp replica list -n <api-app> -g $RG --revision $REV -o json
   ```

5. Refresh `/profiles` again. The first request may include cold-start latency. Pass: the same browser session remains authenticated after a new replica starts. Fail: `session-expired`, a cookie decryption warning, a locally generated `/home/app/.aspnet/DataProtection-Keys` warning, or a new sign-in requirement.
6. Sign out. Pass: the server session is revoked and the browser receives an expired `biostack_session` cookie. Retain no cookie material in evidence.

Rollback: move traffic back only to a revision using the same application name, Blob URI, Key Vault key identifier, and identity access. A code rollback cannot recover cookies after deleting the Blob key ring or disabling a Key Vault version.

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
