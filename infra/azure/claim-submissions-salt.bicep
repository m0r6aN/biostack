targetScope = 'resourceGroup'

// Ledger gate G2: lets the API's managed identity read the claim-submission salt from Key Vault
// so the Container App secret can be a Key Vault reference (keyvaultref:) instead of a value.
// The secret itself is created once, outside Bicep, so it never rotates on redeploy (rotating it
// would split every submitter's history and break the k-threshold counts):
//   az keyvault secret set --vault-name <vault> --name claim-submissions-salt --value "$(openssl rand -hex 32)"

@description('Existing Key Vault that holds the claim-submissions-salt secret (RBAC authorization).')
param keyVaultName string

@description('Existing API Container App whose system-assigned identity reads the secret.')
param apiAppName string

@description('Secret name inside the vault.')
param secretName string = 'claim-submissions-salt'

var keyVaultSecretsUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6')

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' existing = {
  parent: vault
  name: secretName
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: apiAppName
}

// Scoped to the one secret, not the whole vault.
resource readSalt 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(secret.id, apiApp.id, keyVaultSecretsUserRoleId)
  scope: secret
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

@description('Versionless secret URI to pass as -ClaimSubmissionsSaltSecretUri.')
output secretUri string = '${vault.properties.vaultUri}secrets/${secretName}'
