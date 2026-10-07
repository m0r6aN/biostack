targetScope = 'resourceGroup'

@description('Existing Azure Container Registry name.')
@allowed([
  'biostackmissionctrlacr'
])
param acrName string

@description('Existing registry location.')
param location string

@description('Existing registry tags, preserved during the Premium transition.')
param tags object

@allowed([
  'Enabled'
])
@description('Transitional public access state. This parcel intentionally permits only Enabled.')
param publicNetworkAccess string

@allowed([
  'AzureServices'
  'None'
])
@description('Existing trusted-service bypass setting, preserved during the Premium transition.')
param networkRuleBypassOptions string

@description('Existing dedicated data endpoint setting, preserved during the Premium transition.')
param dataEndpointEnabled bool

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Premium'
  }
  properties: any({
    adminUserEnabled: true
    anonymousPullEnabled: false
    dataEndpointEnabled: dataEndpointEnabled
    encryption: {
      status: 'disabled'
    }
    networkRuleBypassOptions: networkRuleBypassOptions
    policies: {
      azureADAuthenticationAsArmPolicy: {
        status: 'enabled'
      }
      exportPolicy: {
        status: 'enabled'
      }
      quarantinePolicy: {
        status: 'disabled'
      }
      retentionPolicy: {
        days: 7
        status: 'disabled'
      }
      trustPolicy: {
        status: 'disabled'
        type: 'Notary'
      }
    }
    publicNetworkAccess: publicNetworkAccess
    zoneRedundancy: 'Disabled'
  })
}

output registryId string = acr.id
output registryName string = acr.name
output registrySku string = acr.sku.name
output publicNetworkAccess string = acr.properties.publicNetworkAccess
output adminUserEnabled bool = acr.properties.adminUserEnabled
