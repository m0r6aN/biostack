targetScope = 'resourceGroup'

// Scheduled data tasks for the knowledge corpus (owner ruling 7, 2026-10-24).
// All times are UTC cron. Defaults (America/New_York in parentheses, EDT/EST):
//   plan-updates     daily  06:00 UTC (02:00 / 01:00)  update checks for every existing entry past the stale window
//   plan-discovery   weekly Mon 06:30 UTC              new-compound research from the discovery watchlist
//   research         daily  07:00 UTC                  compile evidence packets into draft records + review queue
//   refresh-dryrun   daily  09:00 UTC                  Refresh with DryRun=true (logs the would-be diff, no writes)
//   refresh-apply    daily  09:30 UTC                  Refresh apply (idempotent; unchanged rows are no-ops)
// Source acquisition stays a Manual job (source-acquisition-jobs.bicep): each cycle needs its
// approved request, source-decision and registry inputs.
//   council          daily  08:00 UTC                  two-reviewer AI council (consensus passes; disagreement -> owner)

@minLength(3)
@maxLength(18)
param baseName string
param location string = resourceGroup().location

@description('Existing Container Apps managed environment resource ID.')
param containerAppsEnvironmentId string

@description('Immutable worker image: <acr login server>/<repo>@sha256:<64 hex>. Build with backend/Dockerfile.knowledgeworker.')
param workerImage string

@description('ACR login server, e.g. myacr.azurecr.io.')
param acrLoginServer string

@description('User-assigned managed identity with AcrPull on the registry and Key Vault Secrets User on the vault.')
param identityResourceId string

@description('Container Apps environment storage (Azure Files) name mounted at /app/research-runtime.')
param researchStorageName string

@description('Key Vault secret URI (versionless) holding the Postgres connection string. Used by Refresh only.')
param dbConnectionStringSecretUri string

param planUpdatesCron string = '0 6 * * *'
param planDiscoveryCron string = '30 6 * * 1'
param researchCron string = '0 7 * * *'
param councilCron string = '0 8 * * *'
param refreshDryRunCron string = '0 9 * * *'
param refreshApplyCron string = '30 9 * * *'

@description('Set false to keep only the dry-run Refresh scheduled.')
param enableRefreshApply bool = true

@description('Council reviewer 0 provider: openai-compatible or anthropic.')
@allowed([ 'openai-compatible', 'anthropic' ])
param councilReviewer0Provider string
@description('Council reviewer 0 model name.')
param councilReviewer0Model string
@description('Council reviewer 0 base URL (empty = provider default).')
param councilReviewer0Endpoint string = ''
@description('Council reviewer 0 auth header for openai-compatible: bearer or api-key (Azure OpenAI).')
param councilReviewer0AuthHeader string = 'bearer'
@description('Key Vault secret URI (versionless) for reviewer 0 API key.')
param councilReviewer0ApiKeySecretUri string

@description('Council reviewer 1 provider: openai-compatible or anthropic.')
@allowed([ 'openai-compatible', 'anthropic' ])
param councilReviewer1Provider string
param councilReviewer1Model string
param councilReviewer1Endpoint string = ''
param councilReviewer1AuthHeader string = 'bearer'
@description('Key Vault secret URI (versionless) for reviewer 1 API key.')
param councilReviewer1ApiKeySecretUri string

@description('Max compounds the council reviews per run.')
param councilMaxItemsPerRun int = 20

@description('Entries reviewed longer ago than this are due for an update check.')
param updateCheckStaleDays int = 30

@description('Max requests per planning pass (Worker:MaxBatchSize).')
param maxBatchSize int = 50

var digestParts = split(workerImage, '@sha256:')
var imageIsPinned = length(digestParts) == 2 && length(last(digestParts)) == 64 && startsWith(workerImage, '${acrLoginServer}/')
var image = imageIsPinned ? workerImage : fail('workerImage must be ${acrLoginServer}/<repository>@sha256:<64 hex characters>.')

var runtime = '/app/research-runtime'
var common = [
  { name: 'DOTNET_ENVIRONMENT', value: 'Production' }
  { name: 'Worker__MaxBatchSize', value: string(maxBatchSize) }
]
var dbSecrets = [
  { name: 'db-conn-string', keyVaultUrl: dbConnectionStringSecretUri }
]
var dbEnv = [
  { name: 'ConnectionStrings__DefaultConnection', secretRef: 'db-conn-string' }
  { name: 'Worker__RunMode', value: 'Refresh' }
  { name: 'Worker__ReviewDecisionDirectory', value: '${runtime}/review-decisions' }
]
var planningEnv = [
  { name: 'Worker__RunMode', value: 'ResearchPlanning' }
  { name: 'Worker__ResearchPlanningOutputDirectory', value: '${runtime}/research-requests' }
  { name: 'Worker__ResearchUpdateCheckStaleDays', value: string(updateCheckStaleDays) }
]

module planUpdates './modules/knowledge-schedule-job.bicep' = {
  name: '${deployment().name}-plan-updates'
  params: {
    name: '${baseName}-kw-plan-updates'
    location: location
    containerAppsEnvironmentId: containerAppsEnvironmentId
    image: image
    acrLoginServer: acrLoginServer
    identityResourceId: identityResourceId
    storageName: researchStorageName
    cronExpression: planUpdatesCron
    env: concat(common, planningEnv, [
      { name: 'Worker__ResearchPlanningIncludeUpdates', value: 'true' }
      { name: 'Worker__ResearchPlanningIncludeDiscovery', value: 'false' }
    ])
  }
}

module planDiscovery './modules/knowledge-schedule-job.bicep' = {
  name: '${deployment().name}-plan-discovery'
  params: {
    name: '${baseName}-kw-plan-discovery'
    location: location
    containerAppsEnvironmentId: containerAppsEnvironmentId
    image: image
    acrLoginServer: acrLoginServer
    identityResourceId: identityResourceId
    storageName: researchStorageName
    cronExpression: planDiscoveryCron
    env: concat(common, planningEnv, [
      { name: 'Worker__ResearchPlanningIncludeUpdates', value: 'false' }
      { name: 'Worker__ResearchPlanningIncludeDiscovery', value: 'true' }
      { name: 'Worker__ResearchDiscoveryWatchlistPath', value: '${runtime}/discovery/watchlist.json' }
    ])
  }
}

module research './modules/knowledge-schedule-job.bicep' = {
  name: '${deployment().name}-research'
  params: {
    name: '${baseName}-kw-research'
    location: location
    containerAppsEnvironmentId: containerAppsEnvironmentId
    image: image
    acrLoginServer: acrLoginServer
    identityResourceId: identityResourceId
    storageName: researchStorageName
    cronExpression: researchCron
    cpu: '1.0'
    memory: '2Gi'
    env: concat(common, [
      { name: 'Worker__RunMode', value: 'Research' }
      { name: 'Worker__ResearchEvidencePacketDirectory', value: '${runtime}/evidence-packets' }
      { name: 'Worker__ResearchRequestDirectory', value: '${runtime}/research-requests' }
      { name: 'Worker__ResearchReviewDecisionDirectory', value: '${runtime}/review-decisions' }
      { name: 'Worker__ResearchOutputDirectory', value: '${runtime}/research-output' }
    ])
  }
}

module council './modules/knowledge-schedule-job.bicep' = {
  name: '${deployment().name}-council'
  params: {
    name: '${baseName}-kw-council'
    location: location
    containerAppsEnvironmentId: containerAppsEnvironmentId
    image: image
    acrLoginServer: acrLoginServer
    identityResourceId: identityResourceId
    storageName: researchStorageName
    cronExpression: councilCron
    replicaTimeoutSeconds: 7200
    keyVaultSecrets: [
      { name: 'council-reviewer-0-api-key', keyVaultUrl: councilReviewer0ApiKeySecretUri }
      { name: 'council-reviewer-1-api-key', keyVaultUrl: councilReviewer1ApiKeySecretUri }
    ]
    env: concat(common, [
      { name: 'Worker__RunMode', value: 'CouncilReview' }
      { name: 'Council__MaxItemsPerRun', value: string(councilMaxItemsPerRun) }
      { name: 'Council__DecisionDirectory', value: '${runtime}/review-decisions' }
      { name: 'Council__EscalationDirectory', value: '${runtime}/owner-escalations' }
      { name: 'Council__KnownRiskDirectory', value: '${runtime}/known-risks' }
      { name: 'Council__Reviewers__0__Id', value: 'reviewer-0' }
      { name: 'Council__Reviewers__0__Provider', value: councilReviewer0Provider }
      { name: 'Council__Reviewers__0__Model', value: councilReviewer0Model }
      { name: 'Council__Reviewers__0__Endpoint', value: councilReviewer0Endpoint }
      { name: 'Council__Reviewers__0__AuthHeader', value: councilReviewer0AuthHeader }
      { name: 'Council__Reviewers__0__ApiKey', secretRef: 'council-reviewer-0-api-key' }
      { name: 'Council__Reviewers__1__Id', value: 'reviewer-1' }
      { name: 'Council__Reviewers__1__Provider', value: councilReviewer1Provider }
      { name: 'Council__Reviewers__1__Model', value: councilReviewer1Model }
      { name: 'Council__Reviewers__1__Endpoint', value: councilReviewer1Endpoint }
      { name: 'Council__Reviewers__1__AuthHeader', value: councilReviewer1AuthHeader }
      { name: 'Council__Reviewers__1__ApiKey', secretRef: 'council-reviewer-1-api-key' }
    ])
  }
}

module refreshDryRun './modules/knowledge-schedule-job.bicep' = {
  name: '${deployment().name}-refresh-dryrun'
  params: {
    name: '${baseName}-kw-refresh-dryrun'
    location: location
    containerAppsEnvironmentId: containerAppsEnvironmentId
    image: image
    acrLoginServer: acrLoginServer
    identityResourceId: identityResourceId
    storageName: researchStorageName
    cronExpression: refreshDryRunCron
    keyVaultSecrets: dbSecrets
    env: concat(common, dbEnv, [
      { name: 'Worker__DryRun', value: 'true' }
    ])
  }
}

module refreshApply './modules/knowledge-schedule-job.bicep' = if (enableRefreshApply) {
  name: '${deployment().name}-refresh-apply'
  params: {
    name: '${baseName}-kw-refresh-apply'
    location: location
    containerAppsEnvironmentId: containerAppsEnvironmentId
    image: image
    acrLoginServer: acrLoginServer
    identityResourceId: identityResourceId
    storageName: researchStorageName
    cronExpression: refreshApplyCron
    keyVaultSecrets: dbSecrets
    env: concat(common, dbEnv, [
      { name: 'Worker__DryRun', value: 'false' }
    ])
  }
}

output scheduledJobs array = concat([
  planUpdates.outputs.jobName
  planDiscovery.outputs.jobName
  research.outputs.jobName
  council.outputs.jobName
  refreshDryRun.outputs.jobName
], enableRefreshApply ? [ '${baseName}-kw-refresh-apply' ] : [])
