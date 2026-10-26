# Azure Container Apps deployment

BioStack deploys a Next.js frontend and a .NET API to Azure Container Apps. Production requires PostgreSQL for user data and a remote ASP.NET Core Data Protection key ring for durable cookie sessions. No account key, SAS token, client secret, or Key Vault secret belongs in application configuration.

## Production session-key prerequisites

The API revision will fail startup unless all four non-secret values are present:

- `DataProtection__ApplicationName=BioStack.Api.SessionCookie.v1` — immutable across revisions, replicas, and environments that must share cookies;
- `DataProtection__BlobUri=https://<account>.blob.core.windows.net/<container>/<blob>.xml` — one Blob object URI, with no SAS/query string;
- `DataProtection__KeyVaultKeyIdentifier=https://<vault>.vault.azure.net/keys/<key-name>` — versionless so future Key Vault rotations remain usable;
- `DataProtection__ManagedIdentityClientId=<client-id>` only for a user-assigned identity. Omit it for the API Container App's system-assigned identity.

The identity needs exactly these data-plane roles:

- `Storage Blob Data Contributor` (`ba92f5b4-2d11-453d-a403-e96b0029c9fe`) scoped to the dedicated Blob container;
- `Key Vault Crypto User` (`12338af0-0e69-4776-bea7-57ae8d297424`) scoped to the wrapping-key vault (or the individual key where the operator's deployment policy supports that scope).

The provisioning operator also needs control-plane permission to create the storage account/container, vault/key, and role assignments. In standard Azure roles this means `Contributor` plus `User Access Administrator`, or `Owner`, at the target resource-group scope; an approved custom role is acceptable only if it includes the equivalent resource creates and `Microsoft.Authorization/roleAssignments/write`.

The Blob container must already exist. The Key Vault key must be enabled for `wrapKey` and `unwrapKey`. Keep old Key Vault key versions enabled for at least the longest cookie lifetime plus deployment rollback window; deleting an old version makes key-ring entries protected by it unreadable.

`session-data-protection.bicep` creates a dedicated credential-free StorageV2 account/container, RBAC Key Vault/key, and the two role assignments for an existing API Container App whose system identity is already enabled. It outputs the three exact environment values:

```powershell
az containerapp identity assign --name <api-app> --resource-group <resource-group> --system-assigned --output none

az deployment group create `
  --resource-group <resource-group> `
  --template-file ./infra/azure/session-data-protection.bicep `
  --parameters baseName=<short-stem> apiAppName=<api-app>
```

Role assignments can take several minutes to propagate. Do not deploy the fail-closed API revision until the identity can create/read the configured Blob and wrap/unwrap with the Key Vault key.

Cookies minted before this durable key ring exists cannot be recovered after their original container key is lost. Plan one explicit post-cutover sign-in; cookies minted after the cutover are the ones that must survive restart and scale-to-zero.

For an approved user-assigned identity, attach it to the API app, assign the same two roles to that identity, and pass both `-DataProtectionManagedIdentityClientId` and `-DataProtectionManagedIdentityResourceId` to `deploy-container-apps.ps1`. Never substitute an application secret or storage connection string.

## Deploy script inputs

`deploy-container-apps.ps1` requires PostgreSQL plus the two remote key-store URIs. Secret values are passed only through the script's existing Container Apps secret references:

```powershell
pwsh ./infra/azure/deploy-container-apps.ps1 `
  -ResourceGroup <resource-group> `
  -Location <region> `
  -BaseName <base-name> `
  -JwtSecret '<secret supplied out of band>' `
  -PostgresConnectionString '<secret supplied out of band>' `
  -DataProtectionBlobUri 'https://<account>.blob.core.windows.net/<container>/<blob>.xml' `
  -DataProtectionKeyVaultKeyIdentifier 'https://<vault>.vault.azure.net/keys/<key-name>'
```

The script enables the API system identity by default and injects only non-secret Data Protection identifiers. Production rejects SQLite unless the explicitly unsafe throwaway override is set; no persistent SQLite mount is provisioned here.

### `Smtp__MagicLinkSubject` / `AzureCommunicationEmail__MagicLinkSubject`

Leave these unset in normal operation. The API falls back to `MagicLinkSubjects.Default`
(`"BioStack Quick Login"`, `backend/src/BioStack.Api/Auth/MagicLinkDelivery.cs`) whenever
the corresponding configuration key is absent, so the code default only takes effect when
no environment value is set for it. If either environment variable *is* set — including by
this deploy script's `-SmtpMagicLinkSubject` parameter, which defaults to the same string but
can be overridden — that value overrides the code default and is used verbatim as the email
subject line. Confirm the deployed value (`az containerapp show` / the Container Apps
environment variables blade) before assuming the code default applies in production.

## Network boundary

The included template leaves the Blob and Key Vault data-plane endpoints reachable over public Azure endpoints while disabling anonymous Blob access and shared-key authorization. Authentication is managed identity only. If the target Container Apps environment has approved private-endpoint routing, add Blob and Key Vault private endpoints/DNS under that environment's network design before disabling public network access; do not copy the unrelated source-acquisition network topology without confirming routes and DNS.

## Acceptance

Repository builds prove configuration and wiring only. Before release, execute the session continuity procedure in `docs/operations/production-operations-runbook.md`: authenticate once, restart the serving API revision, verify the same cookie, allow the API to reach zero replicas, and verify the same cookie again after cold start.

## Knowledge schedules (2026-10-24)

`knowledge-schedules.bicep` deploys the scheduled KnowledgeWorker jobs: daily update planning, weekly discovery planning, daily Research compile, and a daily Refresh dry-run followed by an apply. The database connection string is a Key Vault reference resolved by a user-assigned identity; no secret is a parameter value. See `docs/operations/knowledge-schedules.md`.

## Structured logs and alerts (ledger G5)

Outside Development, the API and KnowledgeWorker write one JSON object per log line to stdout (`RedactingJsonConsoleFormatter`), with the fields `time`, `level`, `category`, `message`, `props` and `exception`. Before a line is written, emails, IPv4/IPv6 addresses, JWTs, bearer tokens, Stripe keys and `password=`/`token=`/`code=`/`sig=`/`AccountKey=`/`salt=` values are replaced. Scopes are not written. Set `Logging__PlainConsole=true` to fall back to plain text.

Container Apps ships stdout to the environment's Log Analytics workspace. `monitoring-alerts.bicep` emails the owner on:

| Rule | Signal |
|---|---|
| `biostack-api-5xx` | API `Requests` metric with `statusCodeCategory=5xx` above the threshold (default 10) in 5 minutes |
| `biostack-probe-failures` | Probe failures for the API or web app in the system logs |
| `biostack-job-failures` | Executions of jobs whose names start with a configured prefix (default `biostack`) ending in `BackoffLimitExceeded` or `DeadlineExceeded` |
| `biostack-api-error-logs` | More than 20 `Error`/`Critical` JSON log lines from the API in 10 minutes |

```bash
cid=$(az containerapp env show -n <env> -g <rg> --query properties.appLogsConfiguration.logAnalyticsConfiguration.customerId -o tsv)
ws=$(az monitor log-analytics workspace list --query "[?customerId=='$cid'].id" -o tsv)
az deployment group create -g <rg> -f infra/azure/monitoring-alerts.bicep \
  -p logAnalyticsWorkspaceId="$ws" apiAppName=<api-app> webAppName=<web-app> alertEmail=<owner email>
```
## Health probes and replica floor (ledger G3)

The API exposes two health endpoints:

- `/health` is **liveness**. It has no dependency checks, so a database outage never restarts replicas.
- `/health/ready` is **readiness**. It reports the database as reachable or not, within 5 s, and never includes connection details.

The probes are defined in `container-app-probes.json`:

| App | Startup | Liveness | Readiness |
|---|---|---|---|
| API | `/health`, allows up to about 3 min for migrations | `/health` | `/health/ready` |
| Web | TCP | TCP | `/robots.txt` |

The API and web apps are created by `deploy-container-apps.ps1` through the CLI, not Bicep. A Bicep resource for them would replace their whole configuration, so the probes are merged into the live spec instead:

```bash
infra/azure/apply-container-app-probes.sh <rg> <api-app> <web-app> 1 --dry-run   # show the merge
infra/azure/apply-container-app-probes.sh <rg> <api-app> <web-app> 1             # apply
```

The fourth argument is `minReplicas`. With `1`, one replica per app is always on: there are no cold starts, and that replica is billed continuously. With `0`, apps scale to zero, which is cheaper, but the first request after an idle period waits for a cold start. A 46.8 s first request was observed on 2026-10-25. Image updates in `deploy.yml` keep the probes.
## Claim-submission salt (ledger G2)

In Production the API refuses to start unless `ClaimSubmissions__Salt` is set to a private value of at least 32 characters. The public development default is rejected. Set it up once:

1. `az keyvault secret set --vault-name <vault> --name claim-submissions-salt --value "$(openssl rand -hex 32)"`
   - Create it once and never rotate it casually. Rotating it splits each submitter's history.
2. `az deployment group create -g <rg> -f infra/azure/claim-submissions-salt.bicep -p keyVaultName=<vault> apiAppName=<api-app>`
   - This grants the API's system identity *Key Vault Secrets User* on that one secret.
3. Point the API at the secret. Either:
   - pass `-ClaimSubmissionsSaltSecretUri https://<vault>.vault.azure.net/secrets/claim-submissions-salt` to `deploy-container-apps.ps1`, or
   - on an existing app, run:
     - `az containerapp secret set -n <api-app> -g <rg> --secrets "claim-submissions-salt=keyvaultref:<uri>,identityref:system"`
     - `az containerapp update -n <api-app> -g <rg> --set-env-vars "ClaimSubmissions__Salt=secretref:claim-submissions-salt"`
