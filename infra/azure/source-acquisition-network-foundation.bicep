targetScope = 'resourceGroup'

@allowed([
  '909e0322-c3c0-4bce-ae53-b3d2ed735bd4'
])
@description('Fail-closed subscription boundary for this transition parcel.')
param expectedSubscriptionId string = '909e0322-c3c0-4bce-ae53-b3d2ed735bd4'

@allowed([
  'biostack-rg'
])
@description('Fail-closed resource-group boundary for the existing BioStack registry.')
param expectedResourceGroupName string = 'biostack-rg'

@description('Azure region for the parallel network foundation. Must match the existing registry.')
param location string = resourceGroup().location

@description('Existing Azure Container Registry to upgrade to Premium without changing either current access path.')
@allowed([
  'biostackmissionctrlacr'
])
param acrName string = 'biostackmissionctrlacr'

@description('Name for the new VNet used only by the parallel source-acquisition environment.')
@allowed([
  'biostack-source-acquisition-vnet'
])
param virtualNetworkName string

@description('Address prefixes for the new VNet.')
param virtualNetworkAddressPrefixes array

@description('Name for the delegated Container Apps infrastructure subnet.')
@allowed([
  'container-apps-infrastructure'
])
param infrastructureSubnetName string

@description('CIDR for the delegated Container Apps infrastructure subnet.')
param infrastructureSubnetAddressPrefix string

@description('Name for the separate private-endpoint subnet.')
@allowed([
  'private-endpoints'
])
param privateEndpointSubnetName string

@description('CIDR for the separate private-endpoint subnet.')
param privateEndpointSubnetAddressPrefix string

@description('Name for the new parallel workload-profile Container Apps environment.')
@allowed([
  'biostack-source-acquisition-env'
])
param containerAppsEnvironmentName string

@description('Name for the ACR registry private endpoint.')
@allowed([
  'biostackmissionctrlacr-registry-pe'
])
param acrPrivateEndpointName string

@description('Name for the privatelink.azurecr.io VNet link.')
@allowed([
  'biostack-source-acquisition-vnet-link'
])
param acrPrivateDnsVnetLinkName string

var subscriptionGuard = toLower(subscription().subscriptionId) == toLower(expectedSubscriptionId)
  ? expectedSubscriptionId
  : fail('Deployment is allowed only in subscription 909e0322-c3c0-4bce-ae53-b3d2ed735bd4.')
var resourceGroupGuard = toLower(resourceGroup().name) == toLower(expectedResourceGroupName)
  ? expectedResourceGroupName
  : fail('Deployment is allowed only in resource group biostack-rg.')
var guardedEnvironmentName = !contains([
  'biostackmissionctrl-env'
  'biostack-sandbox-env'
], toLower(containerAppsEnvironmentName))
  ? containerAppsEnvironmentName
  : fail('Transition aborted: the parallel environment name must not target a current BioStack environment.')

resource acrCurrent 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

var acrIsTransitionallyReachable = acrCurrent.properties.publicNetworkAccess == 'Enabled' && acrCurrent.properties.adminUserEnabled == true
var acrCurrentUntypedProperties = any(acrCurrent.properties)
var acrAncillaryStateIsExpected = !contains([
  acrCurrentUntypedProperties.anonymousPullEnabled == false
  acrCurrent.properties.dataEndpointEnabled == false
  acrCurrent.properties.encryption.status == 'disabled'
  acrCurrentUntypedProperties.policies.azureADAuthenticationAsArmPolicy.status == 'enabled'
  acrCurrent.properties.policies.exportPolicy.status == 'enabled'
  acrCurrent.properties.policies.quarantinePolicy.status == 'disabled'
  acrCurrent.properties.policies.retentionPolicy.days == 7
  acrCurrent.properties.policies.retentionPolicy.status == 'disabled'
  acrCurrent.properties.policies.trustPolicy.status == 'disabled'
  acrCurrent.properties.policies.trustPolicy.type == 'Notary'
  acrCurrent.properties.zoneRedundancy == 'Disabled'
], false)
var guardedAcrName = acrIsTransitionallyReachable && acrAncillaryStateIsExpected
  ? acrCurrent.name
  : fail('Transition aborted: the existing ACR access, policy, encryption, and redundancy state differs from the audited preservation contract.')
var guardedLocation = toLower(acrCurrent.location) == toLower(location)
  ? location
  : fail('Transition aborted: the network-foundation location must match the existing ACR location.')
var guardedNetworkRuleBypassOptions = contains([
  'AzureServices'
  'None'
], acrCurrent.properties.networkRuleBypassOptions)
  ? (acrCurrent.properties.networkRuleBypassOptions == 'None' ? 'None' : 'AzureServices')
  : fail('Transition aborted: the existing ACR trusted-service bypass setting is unsupported.')

module acrTransition './source-acquisition-acr-transition.bicep' = {
  name: '${deployment().name}-acr-premium-transition'
  params: {
    acrName: guardedAcrName
    location: guardedLocation
    tags: acrCurrent.tags
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: guardedNetworkRuleBypassOptions
    dataEndpointEnabled: acrCurrent.properties.dataEndpointEnabled
  }
}

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: virtualNetworkName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: virtualNetworkAddressPrefixes
    }
  }
}

resource infrastructureSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' = {
  parent: virtualNetwork
  name: infrastructureSubnetName
  properties: {
    addressPrefix: infrastructureSubnetAddressPrefix
    delegations: [
      {
        name: 'Microsoft.App.environments'
        properties: {
          serviceName: 'Microsoft.App/environments'
        }
      }
    ]
    privateEndpointNetworkPolicies: 'Enabled'
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' = {
  parent: virtualNetwork
  name: privateEndpointSubnetName
  properties: {
    addressPrefix: privateEndpointSubnetAddressPrefix
    delegations: []
    privateEndpointNetworkPolicies: 'Disabled'
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: guardedEnvironmentName
  location: location
  properties: {
    vnetConfiguration: {
      infrastructureSubnetId: infrastructureSubnet.id
      internal: false
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

resource acrPrivateEndpoint 'Microsoft.Network/privateEndpoints@2024-01-01' = {
  name: acrPrivateEndpointName
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnet.id
    }
    privateLinkServiceConnections: [
      {
        name: 'registry'
        properties: {
          privateLinkServiceId: acrCurrent.id
          groupIds: [
            'registry'
          ]
        }
      }
    ]
  }
  dependsOn: [
    acrTransition
  ]
}

resource acrPrivateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.azurecr.io'
  location: 'global'
}

resource acrPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-01-01' = {
  parent: acrPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'registry'
        properties: {
          privateDnsZoneId: acrPrivateDnsZone.id
        }
      }
    ]
  }
}

resource acrPrivateDnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: acrPrivateDnsZone
  name: acrPrivateDnsVnetLinkName
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetwork.id
    }
  }
}

output expectedSubscriptionId string = subscriptionGuard
output expectedResourceGroupName string = resourceGroupGuard
output acrName string = acrTransition.outputs.registryName
output acrResourceId string = acrTransition.outputs.registryId
output acrSku string = acrTransition.outputs.registrySku
output acrPublicNetworkAccess string = acrTransition.outputs.publicNetworkAccess
output acrAdminUserEnabled bool = acrTransition.outputs.adminUserEnabled
output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsInfrastructureSubnetId string = infrastructureSubnet.id
output privateEndpointSubnetId string = privateEndpointSubnet.id
output privateDnsVnetId string = virtualNetwork.id
output acrPrivateEndpointResourceId string = acrPrivateEndpoint.id
output acrPrivateDnsZoneResourceId string = acrPrivateDnsZone.id
output acrPrivateDnsVnetLinkResourceId string = acrPrivateDnsVnetLink.id
