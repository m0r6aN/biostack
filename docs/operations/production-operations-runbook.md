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

**ANSWERED 2026-08-28 (operator-verified against production):** `Database__Provider=postgres`; Container App volumes `[]`; no container volume mounts — consistent with an external managed PostgreSQL service, not in-container storage. §1-A applies. Remaining §1-A actions: confirm PITR settings on the server and stand up the weekly logical backup + quarterly restore drill.

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

**Production backup policy (verified 2026-10-26):** `biostackmcpg20260407e2` retains automated backups for **35 days**; geo-redundant backup remains **Disabled**. Retention was raised from 7 days after the restore drill; see the [retention-change receipt](g4-postgres-retention-update-2026-10-26.json). Azure then reported the earliest restore point as `2026-10-16T19:56:29.805559Z`. Check `backup.earliestRestoreDate` before choosing a restore point: the configured 35-day retention is not a claim that 35 days of history is already available. The drill receipt preserves the 7-day setting observed during that drill.

**Restore compute selection:** restore the temporary copy onto **`Standard_D4ds_v5`**, tier **`GeneralPurpose`**, in **East US 2, availability zone 2**. The 2026-10-26 drill received `SkuNotAvailable` for both `Standard_B1ms` and `Standard_B2s`; the General Purpose copy succeeded in **10 minutes 8 seconds** (18 minutes 46 seconds including failed attempts). The basic `az postgres flexible-server restore` command inherits the source compute size and has no SKU argument. For this restore, use an ARM create request with `properties.createMode: PointInTimeRestore`, the source server resource ID and UTC restore point, plus the explicit SKU and availability zone. This selects compute for the temporary copy; production stays on `Standard_B1ms`.

The [2026-10-26 restore-drill receipt](g4-postgres-restore-drill-2026-10-26.json) records the native PITR operation, failed attempts, matching knowledge/source counts and content fingerprint, and verified deletion of every temporary server. Sources are embedded in `KnowledgeEntries.Details.Sources`, rather than a separate source table.

### 1-B SQLite (only acceptable with an Azure Files mount)
- Nightly snapshot: Container Apps job mounting the same share: `sqlite3 /app/data/biostack.db ".backup /app/data/backups/biostack-$(date +%F).db"`, then copy to blob storage. Never file-copy a hot SQLite db without `.backup` — WAL corruption.
- Restore: stop revision traffic, replace the file, restart, run spine verification, re-enable.

**Execution status (2026-10-26):** the native PostgreSQL PITR and read-only SQL comparison were executed successfully; see the dated receipt above for the exact drill scope and evidence.

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

Check revision mode before choosing the rollback operation:
```bash
az containerapp show -n <api-app> -g "$RG" --query properties.configuration.activeRevisionsMode -o tsv
```

Production uses **Single** revision mode. Roll back by deploying the last-good revision's
immutable image, preserving the current environment variables, secrets and probes. Do not
change revision mode for a drill. Record the current image first so it can be restored:
```bash
CURRENT_IMAGE=$(az containerapp show -n <api-app> -g "$RG" --query 'properties.template.containers[0].image' -o tsv)
ROLLBACK_IMAGE=$(az containerapp revision show -n <api-app> -g "$RG" --revision <last-good-revision> --query 'properties.template.containers[0].image' -o tsv)
az containerapp update -n <api-app> -g "$RG" --image "$ROLLBACK_IMAGE" --output none
node scripts/verify-containerapp-deployment.mjs --name <api-app> --resource-group "$RG" --expected-image "$ROLLBACK_IMAGE" --health-path /health/ready
node scripts/smoke-prod.mjs
az containerapp update -n <api-app> -g "$RG" --image "$CURRENT_IMAGE" --output none
node scripts/verify-containerapp-deployment.mjs --name <api-app> --resource-group "$RG" --expected-image "$CURRENT_IMAGE" --health-path /health/ready
node scripts/smoke-prod.mjs
```
Time each update through readiness and smoke completion. On a failed rollback smoke,
restore the captured current image immediately and verify it before continuing. Two revision
names can reference the same image: record that explicitly; redeploying them does not prove
compatibility between different application versions. Record the image/digest, created revision,
elapsed time and smoke result in a receipt under `docs/operations/`.

In **Multiple** revision mode, a last-good active revision can instead receive traffic via
`az containerapp ingress traffic set -n <api-app> -g "$RG" --revision-weight <last-good-revision>=100`.
Same mode-specific procedure applies to the web app. The full production smoke checks both
API and web; its expected active-compound count must track additions and retirements.

### Ingress trust for per-IP controls

`ClientIpResolver` serves both rate-limit partitions and claim-submitter hashing. Configure
only measured ingress networks using `RateLimit__TrustedProxyNetworks__0=<CIDR>` (additional
entries use consecutive indices), or `deploy-container-apps.ps1 -TrustedProxyNetworks @('<CIDR>')`
under PowerShell 7.4 or newer. Existing `RateLimit:TrustedProxies` exact addresses still work.
There is no implicit trust of private/CGNAT ranges. The resolver checks each network with
`System.Net.IPNetwork.Contains`, normalizes IPv4-mapped socket addresses, and walks the
forwarded chain from the right to the first untrusted address. Invalid CIDRs grant no trust;
the deployment script rejects them before making changes.

On 2026-10-26 the managed environment exposed no VNet/CIDR in its ARM settings. Read-only
`/proc/net/tcp6` snapshots showed API port 5000 peers `100.100.0.36` and `100.100.0.174`, both in
`100.100.0.0/24`; that narrow observed ingress range is the production setting. This is an
observed platform allocation, not a guaranteed Azure subnet contract: remeasure after an
environment/network change and repeat the 11 bogus-token auth-verification test. Do not
broaden trust to all `100.64.0.0/10` or all private addresses. Azure appends the verified
visitor address at the right of X-Forwarded-For; never select a caller-supplied leftmost value.

**Rollback triggers (decide BEFORE deploying):** failed pinned smoke; 5xx > 5% in the 15 min after cutover; spine hash-chain verification failure; any Class-boundary regression on the public knowledge response (re-run the `.audit/prod-bpc157-*` probe).

**Database caveat:** revision rollback does NOT roll back migrations. Migrations are hand-written (repo convention) — every migration must be backward-compatible one release, so N-1 code runs against N schema. Destructive migration + rollback = restore from §1 instead.

## 3.5 Required production configuration (startup fails closed without these)

The API validates configuration at startup in Production and crash-loops the new revision if any is missing — the deploy then fails at "Verify API revision readiness" with the revision stuck Activating (observed 2026-08-28, revision 0000112). Before merging a change that adds a required config key, provision the value in prod FIRST.

Current required set (beyond DB/JWT/Stripe):
- `DataProtection__ApplicationName` = `BioStack.Api.SessionCookie.v1` (exact)
- `DataProtection__BlobUri` — HTTPS blob URI for the key ring, no SAS/query (blob container + *Storage Blob Data Contributor* for the app's managed identity)
- `DataProtection__KeyVaultKeyIdentifier` — versionless `https://<vault>.vault.azure.net/keys/<key>` (identity needs *Key Vault Crypto User*)
- optional `DataProtection__ManagedIdentityClientId` (GUID, user-assigned identity only)
- If `Auth__Passkeys__Enabled=true`: `RpId`, `ServerName`, `Origins` (must include `FrontendUrl` exactly)

Rule: any PR introducing a `Validate(...isProduction)` startup check lists its new keys in the PR description, and the deploy is scheduled after the values exist.

## 4. Deploy-time checklist (append to PR template once adopted)
1. `.audit` probe for public knowledge boundary still passes on staging.
2. Migration (if any) reviewed for N-1 compatibility.
3. Last-good revision name noted in the PR description.
4. Post-deploy: pinned smoke + `/tools` first-load check (cold-start) + BPC-157 boundary probe.
