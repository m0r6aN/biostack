targetScope = 'resourceGroup'

@description('Short lowercase deployment stem.')
@minLength(3)
@maxLength(18)
param baseName string

param location string = resourceGroup().location

@description('Existing Container Apps managed environment resource ID with private routing to the storage private endpoint.')
param containerAppsEnvironmentId string

@description('Existing subnet resource ID for the Blob private endpoint.')
param privateEndpointSubnetId string

@description('Existing VNet resource ID used by the Container Apps environment and private endpoint.')
param privateDnsVnetId string

@description('Immutable ACR image reference. Tags are rejected.')
param workerImage string

@description('Existing private Azure Container Registry name.')
param acrName string

@description('Existing approved ACR private endpoint resource ID.')
param acrPrivateEndpointResourceId string

@description('Existing privatelink.azurecr.io private DNS zone resource ID.')
param acrPrivateDnsZoneResourceId string

@description('Existing ACR private DNS zone VNet-link resource ID.')
param acrPrivateDnsVnetLinkResourceId string

@description('Current Container Apps managed-environment infrastructure subnet resource ID.')
param containerAppsInfrastructureSubnetId string

@description('Microsoft Entra object ID for Clint read-only artifact access.')
param clintPrincipalId string

@description('Caller-supplied acquisition cycle ID.')
param acquisitionCycleId string

@description('Public NCBI E-utilities tool identifier.')
param pubMedTool string

@description('Public operational contact used for NCBI E-utilities.')
param pubMedContactEmail string

@description('Daily UTC schedule for source-free retention enforcement.')
param retentionCron string = '0 7 * * *'

var storageName = toLower(take('bs${uniqueString(resourceGroup().id, baseName)}source', 24))
var containerName = 'source-acquisition'
var artifactPrefix = 'source-acquisition'
var customRoleName = '${baseName}-source-acquisition-blob-object-writer'
var customRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  guid(subscription().id, customRoleName))
var blobDataReaderRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
var acrPullRoleId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

var containerAppsEnvironmentIdParts = split(containerAppsEnvironmentId, '/')
var acrPrivateEndpointIdParts = split(acrPrivateEndpointResourceId, '/')
var acrPrivateDnsZoneIdParts = split(acrPrivateDnsZoneResourceId, '/')
var acrPrivateDnsVnetLinkIdParts = split(acrPrivateDnsVnetLinkResourceId, '/')
var containerAppsVnetId = join(take(split(toLower(containerAppsInfrastructureSubnetId), '/'), 9), '/')
var acrPrivateEndpointVnetId = join(take(split(toLower(acrPrivateEndpoint.properties.subnet.id), '/'), 9), '/')

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  scope: resourceGroup(containerAppsEnvironmentIdParts[2], containerAppsEnvironmentIdParts[4])
  name: containerAppsEnvironmentIdParts[8]
}

resource acrPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-01-01' existing = {
  scope: resourceGroup(acrPrivateEndpointIdParts[2], acrPrivateEndpointIdParts[4])
  name: acrPrivateEndpointIdParts[8]
}

resource acrPrivateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' existing = {
  scope: resourceGroup(acrPrivateDnsZoneIdParts[2], acrPrivateDnsZoneIdParts[4])
  name: acrPrivateDnsZoneIdParts[8]
}

resource acrPrivateDnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' existing = {
  parent: acrPrivateDnsZone
  name: acrPrivateDnsVnetLinkIdParts[10]
}

var workerImageParts = split(workerImage, '@sha256:')
var workerImageDigest = length(workerImageParts) == 2 ? last(workerImageParts) : ''
var workerImageDigestRemainder = replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(replace(toLower(workerImageDigest), '0', ''), '1', ''), '2', ''), '3', ''), '4', ''), '5', ''), '6', ''), '7', ''), '8', ''), '9', ''), 'a', ''), 'b', ''), 'c', ''), 'd', ''), 'e', ''), 'f', '')
var acrIsPrivateAndCredentialFree = acr.properties.publicNetworkAccess == 'Disabled' && acr.properties.adminUserEnabled == false
var acrPrivateConnections = filter(acrPrivateEndpoint.properties.privateLinkServiceConnections, connection => toLower(connection.properties.privateLinkServiceId) == toLower(acr.id) && contains(connection.properties.groupIds, 'registry') && connection.properties.privateLinkServiceConnectionState.status == 'Approved')
var acrNetworkEvidenceIsValid = toLower(acrPrivateEndpoint.id) == toLower(acrPrivateEndpointResourceId) && toLower(acrPrivateDnsZone.id) == toLower(acrPrivateDnsZoneResourceId) && toLower(acrPrivateDnsVnetLink.id) == toLower(acrPrivateDnsVnetLinkResourceId) && toLower(acrPrivateDnsZone.name) == 'privatelink.azurecr.io' && toLower(containerAppsEnvironment.properties.vnetConfiguration.infrastructureSubnetId) == toLower(containerAppsInfrastructureSubnetId) && toLower(privateDnsVnetId) == containerAppsVnetId && acrPrivateEndpointVnetId == containerAppsVnetId && toLower(acrPrivateDnsVnetLink.properties.virtualNetwork.id) == containerAppsVnetId && acrPrivateDnsVnetLink.properties.registrationEnabled == false && length(acrPrivateConnections) == 1
var workerImageIsValid = acrIsPrivateAndCredentialFree && acrNetworkEvidenceIsValid && length(workerImageParts) == 2 && startsWith(workerImage, '${acr.properties.loginServer}/') && length(first(workerImageParts)) > length(acr.properties.loginServer) + 1 && length(workerImageDigest) == 64 && length(workerImageDigestRemainder) == 0
var validatedWorkerImage = workerImageIsValid ? workerImage : fail('ACR must be private and credential-free; its approved registry private endpoint and privatelink.azurecr.io VNet link must match the current Container Apps infrastructure subnet VNet; workerImage must be its login server plus /repository@sha256:<64 hex characters>.')

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_ZRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      bypass: 'None'
      defaultAction: 'Deny'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: false
    }
    containerDeleteRetentionPolicy: {
      enabled: false
    }
    isVersioningEnabled: false
  }
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: containerName
  properties: {
    publicAccess: 'None'
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2024-01-01' = {
  name: '${baseName}-source-blob-pe'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'blob'
        properties: {
          privateLinkServiceId: storage.id
          groupIds: [
            'blob'
          ]
        }
      }
    ]
  }
}

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.blob.${environment().suffixes.storage}'
  location: 'global'
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-01-01' = {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'blob'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

resource privateDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: privateDnsZone
  name: '${baseName}-container-apps'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: privateDnsVnetId
    }
  }
}

module objectWriterRole './source-acquisition-role.bicep' = {
  name: '${deployment().name}-object-writer-role'
  scope: subscription()
  params: {
    roleName: customRoleName
    assignableScopeId: resourceGroup().id
  }
}

resource acquisitionJob 'Microsoft.App/jobs@2024-03-01' = {
  name: '${baseName}-source-acquisition'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 7200
      replicaRetryLimit: 0
      registries: [
        {
          server: acr.properties.loginServer
          identity: 'system'
        }
      ]
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: validatedWorkerImage
          env: acquisitionEnvironment
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
        }
      ]
    }
  }
}

resource retentionJob 'Microsoft.App/jobs@2024-03-01' = {
  name: '${baseName}-source-retention'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      triggerType: 'Schedule'
      replicaTimeout: 1800
      replicaRetryLimit: 0
      registries: [
        {
          server: acr.properties.loginServer
          identity: 'system'
        }
      ]
      scheduleTriggerConfig: {
        cronExpression: retentionCron
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'retention'
          image: validatedWorkerImage
          env: retentionEnvironment
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

var commonEnvironment = [
  {
    name: 'DOTNET_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'Worker__SourceAcquisitionStorageProvider'
    value: 'AzureBlob'
  }
  {
    name: 'Worker__SourceAcquisitionBlobServiceUri'
    value: storage.properties.primaryEndpoints.blob
  }
  {
    name: 'Worker__SourceAcquisitionBlobContainerName'
    value: containerName
  }
  {
    name: 'Worker__SourceAcquisitionBlobPrefix'
    value: artifactPrefix
  }
  {
    name: 'Worker__SourceAcquisitionCandidateRetentionDays'
    value: '30'
  }
  {
    name: 'Worker__SourceAcquisitionReceiptRetentionDays'
    value: '30'
  }
]

var acquisitionEnvironment = concat(commonEnvironment, [
  {
    name: 'Worker__RunMode'
    value: 'SourceAcquisition'
  }
  {
    name: 'Worker__SourceAcquisitionCycleId'
    value: acquisitionCycleId
  }
  {
    name: 'Worker__SourceAcquisitionResearchRequestPath'
    value: '/app/inputs/research-request.json'
  }
  {
    name: 'Worker__SourceAcquisitionDecisionPath'
    value: '/app/inputs/source-decisions.json'
  }
  {
    name: 'Worker__SourceAcquisitionRegistryPath'
    value: '/app/inputs/source-registry.json'
  }
  {
    name: 'Worker__SourceAcquisitionPubMedTool'
    value: pubMedTool
  }
  {
    name: 'Worker__SourceAcquisitionPubMedContactEmail'
    value: pubMedContactEmail
  }
])

var retentionEnvironment = concat(commonEnvironment, [
  {
    name: 'Worker__RunMode'
    value: 'SourceAcquisitionRetention'
  }
])

resource acquisitionWrite 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: container
  name: guid(container.id, acquisitionJob.id, customRoleDefinitionId)
  properties: {
    principalId: acquisitionJob.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: objectWriterRole.outputs.roleDefinitionId
  }
}

resource retentionWrite 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: container
  name: guid(container.id, retentionJob.id, customRoleDefinitionId)
  properties: {
    principalId: retentionJob.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: objectWriterRole.outputs.roleDefinitionId
  }
}

resource acquisitionAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: acr
  name: guid(acr.id, acquisitionJob.id, acrPullRoleId)
  properties: {
    principalId: acquisitionJob.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource retentionAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: acr
  name: guid(acr.id, retentionJob.id, acrPullRoleId)
  properties: {
    principalId: retentionJob.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource clintRead 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: container
  name: guid(container.id, clintPrincipalId, blobDataReaderRoleId)
  properties: {
    principalId: clintPrincipalId
    principalType: 'User'
    roleDefinitionId: blobDataReaderRoleId
  }
}

output storageAccountName string = storage.name
output containerResourceId string = container.id
output acquisitionJobName string = acquisitionJob.name
output retentionJobName string = retentionJob.name
