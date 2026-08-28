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

## Network boundary

The included template leaves the Blob and Key Vault data-plane endpoints reachable over public Azure endpoints while disabling anonymous Blob access and shared-key authorization. Authentication is managed identity only. If the target Container Apps environment has approved private-endpoint routing, add Blob and Key Vault private endpoints/DNS under that environment's network design before disabling public network access; do not copy the unrelated source-acquisition network topology without confirming routes and DNS.

## Acceptance

Repository builds prove configuration and wiring only. Before release, execute the session continuity procedure in `docs/operations/production-operations-runbook.md`: authenticate once, restart the serving API revision, verify the same cookie, allow the API to reach zero replicas, and verify the same cookie again after cold start.
