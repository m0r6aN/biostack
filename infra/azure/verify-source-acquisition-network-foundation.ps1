[CmdletBinding()]
param(
    [string]$SubscriptionId = '909e0322-c3c0-4bce-ae53-b3d2ed735bd4',
    [string]$ResourceGroup = 'biostack-rg',
    [string]$BicepFile = (Join-Path $PSScriptRoot 'source-acquisition-network-foundation.bicep'),
    [string]$ParametersFile = (Join-Path $PSScriptRoot 'source-acquisition-network-foundation.parameters.example.json'),
    [switch]$RunWhatIf
)

$ErrorActionPreference = 'Stop'
$expectedSubscriptionId = '909e0322-c3c0-4bce-ae53-b3d2ed735bd4'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-Ipv4CidrRange {
    param([string]$Cidr)

    $parts = $Cidr.Split('/')
    Assert-True ($parts.Count -eq 2) "Invalid IPv4 CIDR: $Cidr"
    $address = [System.Net.IPAddress]::Parse($parts[0])
    $bytes = $address.GetAddressBytes()
    Assert-True ($bytes.Count -eq 4) "Only IPv4 CIDRs are supported: $Cidr"
    $prefixLength = [int]$parts[1]
    Assert-True ($prefixLength -ge 0 -and $prefixLength -le 32) "Invalid IPv4 prefix length: $Cidr"

    $addressValue = ([uint64]$bytes[0] * 16777216) +
        ([uint64]$bytes[1] * 65536) +
        ([uint64]$bytes[2] * 256) +
        [uint64]$bytes[3]
    $rangeSize = [uint64][math]::Pow(2, 32 - $prefixLength)
    $start = [uint64]([math]::Floor($addressValue / $rangeSize) * $rangeSize)

    [pscustomobject]@{
        Start = $start
        End = $start + $rangeSize - 1
    }
}

function Test-Ipv4CidrOverlap {
    param(
        [string]$First,
        [string]$Second
    )

    $firstRange = Get-Ipv4CidrRange $First
    $secondRange = Get-Ipv4CidrRange $Second
    return $firstRange.Start -le $secondRange.End -and $secondRange.Start -le $firstRange.End
}

Assert-True ($SubscriptionId -eq $expectedSubscriptionId) "Subscription must be exactly $expectedSubscriptionId."
Assert-True (Test-Path -LiteralPath $BicepFile) "Missing Bicep file: $BicepFile"
Assert-True (Test-Path -LiteralPath $ParametersFile) "Missing parameters file: $ParametersFile"

$source = Get-Content -LiteralPath $BicepFile -Raw
$acrTransitionSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-acquisition-acr-transition.bicep') -Raw
$parameters = Get-Content -LiteralPath $ParametersFile -Raw | ConvertFrom-Json

Assert-True ($parameters.parameters.expectedSubscriptionId.value -eq $expectedSubscriptionId) 'Example parameters use the wrong subscription.'
Assert-True ($parameters.parameters.expectedResourceGroupName.value -eq 'biostack-rg') 'Example parameters use the wrong resource group.'
Assert-True ($parameters.parameters.acrName.value -eq 'biostackmissionctrlacr') 'Example parameters target the wrong ACR.'
Assert-True ($parameters.parameters.infrastructureSubnetAddressPrefix.value -ne $parameters.parameters.privateEndpointSubnetAddressPrefix.value) 'Infrastructure and private-endpoint subnets must differ.'
Assert-True ($source.Contains("name: 'privatelink.azurecr.io'")) 'Exact ACR private DNS zone is missing.'
Assert-True ($source.Contains("'registry'")) 'ACR private endpoint registry group is missing.'
Assert-True ($source.Contains("publicNetworkAccess: 'Enabled'")) 'ACR public access preservation is missing.'
Assert-True ($acrTransitionSource.Contains('adminUserEnabled: true')) 'ACR admin-user preservation is missing.'
Assert-True (-not $source.Contains("publicNetworkAccess: 'Disabled'")) 'This transition must not disable ACR public access.'
Assert-True (-not $acrTransitionSource.Contains('adminUserEnabled: false')) 'This transition must not disable the ACR admin user.'
Assert-True (-not $source.Contains('Microsoft.App/containerApps@')) 'Current Container Apps must not be declared.'
Assert-True (-not $source.Contains('Microsoft.App/jobs@')) 'Current Container App Jobs must not be declared.'
Assert-True (-not $source.Contains('Microsoft.Authorization/roleAssignments@')) 'Role assignments are outside this parcel.'

$compiledJson = rtk proxy az bicep build --file $BicepFile --stdout
if ($LASTEXITCODE -ne 0) {
    throw 'Bicep compilation failed.'
}

$compiled = $compiledJson | ConvertFrom-Json -Depth 100
$rootTypes = @($compiled.resources | ForEach-Object { $_.type })
$requiredRootTypes = @(
    'Microsoft.Resources/deployments',
    'Microsoft.Network/virtualNetworks',
    'Microsoft.Network/virtualNetworks/subnets',
    'Microsoft.App/managedEnvironments',
    'Microsoft.Network/privateEndpoints',
    'Microsoft.Network/privateDnsZones',
    'Microsoft.Network/privateEndpoints/privateDnsZoneGroups',
    'Microsoft.Network/privateDnsZones/virtualNetworkLinks'
)

foreach ($requiredType in $requiredRootTypes) {
    Assert-True ($rootTypes -contains $requiredType) "Compiled template is missing $requiredType."
}

$forbiddenTypes = @(
    'Microsoft.App/containerApps',
    'Microsoft.App/jobs',
    'Microsoft.Authorization/roleAssignments',
    'Microsoft.ContainerRegistry/registries/webhooks',
    'Microsoft.ContainerRegistry/registries/replications'
)

foreach ($forbiddenType in $forbiddenTypes) {
    Assert-True (-not ($rootTypes -contains $forbiddenType)) "Compiled template unexpectedly contains $forbiddenType."
}

$requiredOutputs = @(
    'containerAppsEnvironmentId',
    'containerAppsInfrastructureSubnetId',
    'privateEndpointSubnetId',
    'privateDnsVnetId',
    'acrPrivateEndpointResourceId',
    'acrPrivateDnsZoneResourceId',
    'acrPrivateDnsVnetLinkResourceId'
)

foreach ($requiredOutput in $requiredOutputs) {
    Assert-True ($null -ne $compiled.outputs.$requiredOutput) "Compiled template is missing output $requiredOutput."
}

$acrDeployment = @($compiled.resources | Where-Object { $_.type -eq 'Microsoft.Resources/deployments' })[0]
$compiledAcr = @($acrDeployment.properties.template.resources | Where-Object { $_.type -eq 'Microsoft.ContainerRegistry/registries' })[0]
Assert-True ($compiledAcr.sku.name -eq 'Premium') 'Compiled ACR transition does not select Premium.'
Assert-True ($compiledAcr.properties.publicNetworkAccess -eq "[parameters('publicNetworkAccess')]") 'Compiled ACR transition does not preserve public access from its Enabled-only parameter.'
Assert-True ($compiledAcr.properties.adminUserEnabled -eq $true) 'Compiled ACR transition does not preserve the admin user as true.'
Assert-True ($compiledAcr.properties.anonymousPullEnabled -eq $false) 'Compiled ACR transition does not preserve anonymous pull state.'
Assert-True ($compiledAcr.properties.encryption.status -eq 'disabled') 'Compiled ACR transition does not preserve encryption state.'
Assert-True ($compiledAcr.properties.policies.azureADAuthenticationAsArmPolicy.status -eq 'enabled') 'Compiled ACR transition does not preserve ARM authentication policy.'

Write-Output 'PASS: deterministic network-foundation preservation and shape checks'
Write-Output "PASS: Bicep compiled with $($rootTypes.Count) root resources and all source-acquisition-jobs.bicep network outputs"

if (-not $RunWhatIf) {
    Write-Output 'SKIP: Azure queries and what-if (use -RunWhatIf explicitly)'
    exit 0
}

$account = rtk proxy az account show --subscription $SubscriptionId --query '{id:id,state:state}' --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to query the requested Azure subscription.'
}

Assert-True ($account.id -eq $expectedSubscriptionId) 'Azure CLI selected the wrong subscription.'
Assert-True ($account.state -eq 'Enabled') 'Azure subscription is not Enabled.'

$registry = rtk proxy az acr show `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroup `
    --name biostackmissionctrlacr `
    --query '{sku:sku.name,publicNetworkAccess:publicNetworkAccess,adminUserEnabled:adminUserEnabled,tags:tags}' `
    --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to query the existing ACR.'
}

Assert-True ($registry.publicNetworkAccess -eq 'Enabled') 'Live ACR publicNetworkAccess is not Enabled; transition is fail-closed.'
Assert-True ($registry.adminUserEnabled -eq $true) 'Live ACR adminUserEnabled is not true; transition is fail-closed.'
Assert-True (@('Basic', 'Premium') -contains $registry.sku) "Unexpected live ACR SKU: $($registry.sku)"
Assert-True (@($registry.tags.PSObject.Properties).Count -eq 0) 'Direct ACR transition what-if expects the currently audited empty tag set.'

$vnets = rtk proxy az network vnet list `
    --subscription $SubscriptionId `
    --query '[].{name:name,resourceGroup:resourceGroup,addressPrefixes:addressSpace.addressPrefixes}' `
    --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to audit existing subscription VNets.'
}
$vnetCount = @($vnets).Count
$proposedVnetPrefixes = @($parameters.parameters.virtualNetworkAddressPrefixes.value)
foreach ($vnet in @($vnets)) {
    foreach ($existingPrefix in @($vnet.addressPrefixes)) {
        foreach ($proposedPrefix in $proposedVnetPrefixes) {
            Assert-True (-not (Test-Ipv4CidrOverlap $existingPrefix $proposedPrefix)) "Example CIDR $proposedPrefix overlaps existing VNet $($vnet.resourceGroup)/$($vnet.name) prefix $existingPrefix."
        }
    }
}

Assert-True (-not (Test-Ipv4CidrOverlap `
    $parameters.parameters.infrastructureSubnetAddressPrefix.value `
    $parameters.parameters.privateEndpointSubnetAddressPrefix.value)) 'Example infrastructure and private-endpoint subnets overlap.'

Write-Output "PASS: live prerequisite state ACR sku=$($registry.sku), publicNetworkAccess=Enabled, adminUserEnabled=true"
Write-Output "INFO: enabled-subscription VNet audit count=$vnetCount"

$whatIfOutput = rtk proxy az deployment group what-if `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroup `
    --name 'keo74-network-foundation-020-what-if' `
    --template-file $BicepFile `
    --parameters "@$ParametersFile" `
    --result-format ResourceIdOnly `
    --no-pretty-print
if ($LASTEXITCODE -ne 0) {
    throw 'Azure what-if failed.'
}

$whatIf = $whatIfOutput | ConvertFrom-Json -Depth 100
Assert-True ($whatIf.status -eq 'Succeeded') 'Azure network-foundation what-if did not succeed.'
$nonIgnoredFoundationChanges = @($whatIf.changes | Where-Object { $_.changeType -ne 'Ignore' })
$resourceIdPrefix = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers"
$expectedFoundationCreates = @(
    "$resourceIdPrefix/Microsoft.App/managedEnvironments/$($parameters.parameters.containerAppsEnvironmentName.value)",
    "$resourceIdPrefix/Microsoft.Network/virtualNetworks/$($parameters.parameters.virtualNetworkName.value)",
    "$resourceIdPrefix/Microsoft.Network/virtualNetworks/$($parameters.parameters.virtualNetworkName.value)/subnets/$($parameters.parameters.infrastructureSubnetName.value)",
    "$resourceIdPrefix/Microsoft.Network/virtualNetworks/$($parameters.parameters.virtualNetworkName.value)/subnets/$($parameters.parameters.privateEndpointSubnetName.value)",
    "$resourceIdPrefix/Microsoft.Network/privateEndpoints/$($parameters.parameters.acrPrivateEndpointName.value)",
    "$resourceIdPrefix/Microsoft.Network/privateEndpoints/$($parameters.parameters.acrPrivateEndpointName.value)/privateDnsZoneGroups/default",
    "$resourceIdPrefix/Microsoft.Network/privateDnsZones/privatelink.azurecr.io",
    "$resourceIdPrefix/Microsoft.Network/privateDnsZones/privatelink.azurecr.io/virtualNetworkLinks/$($parameters.parameters.acrPrivateDnsVnetLinkName.value)"
)
$expectedFoundationCreatesNormalized = @($expectedFoundationCreates | ForEach-Object { $_.ToLowerInvariant() })

Assert-True ($nonIgnoredFoundationChanges.Count -eq $expectedFoundationCreates.Count) "Network-foundation what-if must contain exactly $($expectedFoundationCreates.Count) non-ignored changes; found $($nonIgnoredFoundationChanges.Count)."
foreach ($change in $nonIgnoredFoundationChanges) {
    Assert-True ($change.changeType -eq 'Create') "Network-foundation what-if contains unsafe change type $($change.changeType) for $($change.resourceId)."
    Assert-True ($expectedFoundationCreatesNormalized -contains $change.resourceId.ToLowerInvariant()) "Network-foundation what-if contains unexpected resource $($change.resourceId)."
}
foreach ($expectedResourceId in $expectedFoundationCreates) {
    Assert-True (@($nonIgnoredFoundationChanges | Where-Object { $_.resourceId.ToLowerInvariant() -eq $expectedResourceId.ToLowerInvariant() }).Count -eq 1) "Network-foundation what-if is missing expected create $expectedResourceId."
}

$acrWhatIfOutput = rtk proxy az deployment group what-if `
    --subscription $SubscriptionId `
    --resource-group $ResourceGroup `
    --name 'keo74-acr-premium-transition-what-if' `
    --template-file (Join-Path $PSScriptRoot 'source-acquisition-acr-transition.bicep') `
    --parameters acrName=biostackmissionctrlacr location=eastus 'tags={}' publicNetworkAccess=Enabled networkRuleBypassOptions=AzureServices dataEndpointEnabled=false `
    --result-format FullResourcePayloads `
    --no-pretty-print
if ($LASTEXITCODE -ne 0) {
    throw 'Direct ACR transition what-if failed.'
}

$acrWhatIf = $acrWhatIfOutput | ConvertFrom-Json -Depth 100
$acrResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.ContainerRegistry/registries/biostackmissionctrlacr"
$acrChange = @($acrWhatIf.changes | Where-Object { $_.resourceId -eq $acrResourceId })[0]
Assert-True ($null -ne $acrChange) 'Direct ACR transition what-if did not report the registry.'
Assert-True (@('Modify', 'NoChange') -contains $acrChange.changeType) "Unexpected ACR change type: $($acrChange.changeType)"
Assert-True ($acrChange.after.sku.name -eq 'Premium') 'ACR what-if does not result in Premium.'
Assert-True ($acrChange.after.properties.publicNetworkAccess -eq 'Enabled') 'ACR what-if would change public access.'
Assert-True ($acrChange.after.properties.adminUserEnabled -eq $true) 'ACR what-if would change admin-user access.'
$acrDeltaPaths = @($acrChange.delta | ForEach-Object { $_.path })
Assert-True (@($acrDeltaPaths | Where-Object { $_ -ne 'sku.name' }).Count -eq 0) "ACR what-if contains non-SKU deltas: $($acrDeltaPaths -join ', ')"

Write-Output "PASS: Azure network-foundation what-if completed with $($nonIgnoredFoundationChanges.Count) non-ignored changes; no deployment was executed"
Write-Output 'PASS: direct ACR what-if changes only sku.name and preserves publicNetworkAccess Enabled plus adminUserEnabled true'
[pscustomobject]@{
    status = $whatIf.status
    nonIgnoredChanges = @($nonIgnoredFoundationChanges | ForEach-Object {
        [pscustomobject]@{
            changeType = $_.changeType
            resourceId = $_.resourceId
        }
    })
    acrDeltaPaths = $acrDeltaPaths
} | ConvertTo-Json -Depth 10
