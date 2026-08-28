targetScope = 'resourceGroup'

@description('Short lowercase deployment stem used for globally unique resource names.')
@minLength(3)
@maxLength(16)
param baseName string

@description('Existing API Container App with a system-assigned managed identity already enabled.')
param apiAppName string

param location string = resourceGroup().location

@description('Private Blob container that holds only the ASP.NET Core Data Protection key ring.')
param containerName string = 'data-protection'

@description('Single Blob object used by the ASP.NET Core Data Protection key-ring repository.')
param keyRingBlobName string = 'biostack-session-key-ring.xml'

@description('Versionless Key Vault key name used to wrap Data Protection keys.')
param keyName string = 'biostack-session-cookie'

var storageName = toLower(take('bs${uniqueString(resourceGroup().id, baseName)}dp', 24))
var vaultName = toLower(take('${baseName}-${uniqueString(resourceGroup().id, baseName)}-dp', 24))
var blobDataContributorRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var keyVaultCryptoUserRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '12338af0-0e69-4776-bea7-57ae8d297424')

resource apiApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: apiAppName
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  properties: {
    enablePurgeProtection: true
    enableRbacAuthorization: true
    enableSoftDelete: true
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
  }
}

resource wrappingKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: vault
  name: keyName
  properties: {
    attributes: {
      enabled: true
    }
    keyOps: [
      'wrapKey'
      'unwrapKey'
    ]
    kty: 'RSA'
    keySize: 2048
  }
}

resource storageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(container.id, apiApp.id, blobDataContributorRoleId)
  scope: container
  properties: {
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: blobDataContributorRoleId
  }
}

resource keyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, apiApp.id, keyVaultCryptoUserRoleId)
  scope: vault
  properties: {
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultCryptoUserRoleId
  }
}

output applicationName string = 'BioStack.Api.SessionCookie.v1'
output blobUri string = '${storage.properties.primaryEndpoints.blob}${containerName}/${keyRingBlobName}'
output keyVaultKeyIdentifier string = '${vault.properties.vaultUri}keys/${keyName}'
output apiManagedIdentityPrincipalId string = apiApp.identity.principalId
