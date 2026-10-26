targetScope = 'resourceGroup'

// Ledger gate G5: alert rules for the production API, web app and knowledge jobs, sent by
// email to the owner. Assumes the Container Apps environment sends logs to Log Analytics,
// which `az containerapp env create` sets up by default. The queries union the legacy
// (`*_CL`, Log Analytics destination) and the Azure Monitor (`ContainerAppSystemLogs` /
// `ContainerAppConsoleLogs`) table shapes, so they work with either logs destination.
//
// Find the workspace resource id:
//   cid=$(az containerapp env show -n <env> -g <rg> --query properties.appLogsConfiguration.logAnalyticsConfiguration.customerId -o tsv)
//   az monitor log-analytics workspace list --query "[?customerId=='$cid'].id" -o tsv

@description('Resource id of the Log Analytics workspace that receives the Container Apps environment logs.')
param logAnalyticsWorkspaceId string

@description('API Container App name.')
param apiAppName string

@description('Web Container App name.')
param webAppName string

@description('Container Apps job names starting with any of these prefixes are watched for failed executions.')
param jobNamePrefixes array = [
  'biostack'
]

@description('Owner email that receives every alert.')
param alertEmail string

@description('Server errors (5xx) from the API in 5 minutes that raise an alert.')
@minValue(1)
param api5xxThreshold int = 10

@description('Error/Critical log lines from the API in 10 minutes that raise an alert.')
@minValue(1)
param apiErrorLogThreshold int = 20

param location string = resourceGroup().location

@description('Set false to deploy the rules disabled (e.g. while verifying queries).')
param enabled bool = true

var watchedApps = [
  apiAppName
  webAppName
]

// Legacy and Azure Monitor system-log shapes, normalized to App/Job/Reason/Log.
var systemLogs = '''
union isfuzzy=true
  (ContainerAppSystemLogs_CL | project TimeGenerated, App = ContainerAppName_s, Job = JobName_s, Reason = Reason_s, Log = Log_s),
  (ContainerAppSystemLogs    | project TimeGenerated, App = ContainerAppName,   Job = JobName,   Reason = Reason,   Log = Log)
'''

var consoleLogs = '''
union isfuzzy=true
  (ContainerAppConsoleLogs_CL | project TimeGenerated, App = ContainerAppName_s, Log = Log_s),
  (ContainerAppConsoleLogs    | project TimeGenerated, App = ContainerAppName,   Log = Log)
'''

resource apiApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: apiAppName
}

resource owner 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'biostack-owner-alerts'
  location: 'global'
  properties: {
    groupShortName: 'biostack'
    enabled: true
    emailReceivers: [
      {
        name: 'owner'
        emailAddress: alertEmail
        useCommonAlertSchema: true
      }
    ]
  }
}

// 1. API 5xx responses (platform metric, needs no logs).
resource api5xx 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: 'biostack-api-5xx'
  location: 'global'
  properties: {
    description: 'API returned more than ${api5xxThreshold} server errors (5xx) in 5 minutes.'
    severity: 1
    enabled: enabled
    scopes: [
      apiApp.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    autoMitigate: true
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'server-errors'
          metricNamespace: 'Microsoft.App/containerApps'
          metricName: 'Requests'
          dimensions: [
            {
              name: 'statusCodeCategory'
              operator: 'Include'
              values: [
                '5xx'
              ]
            }
          ]
          operator: 'GreaterThan'
          threshold: api5xxThreshold
          timeAggregation: 'Total'
        }
      ]
    }
    actions: [
      {
        actionGroupId: owner.id
      }
    ]
  }
}

// 2. Readiness, liveness or startup probe failures on the API or web app (see container-app-probes.json).
resource probeFailures 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'biostack-probe-failures'
  location: location
  kind: 'LogAlert'
  properties: {
    displayName: 'BioStack probe failures'
    description: 'API or web replicas failed health probes (readiness/liveness/startup) in the last 10 minutes.'
    severity: 1
    enabled: enabled
    scopes: [
      logAnalyticsWorkspaceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: '${systemLogs}| where App in (${join(map(watchedApps, a => '\'${a}\''), ', ')})\n| where Log contains "probe failed" or Log contains "failed readiness probe" or Log contains "failed liveness probe" or Log contains "failed startup probe"'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 2
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        owner.id
      ]
    }
  }
}

// 3. Failed knowledge job executions (Refresh, Discovery, Research, Council, retention...).
// Jobs run with replicaRetryLimit 0, so a non-zero exit, including a blocked mass retirement
// (Refresh G1), surfaces as BackoffLimitExceeded. A timeout surfaces as DeadlineExceeded.
resource jobFailures 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'biostack-job-failures'
  location: location
  kind: 'LogAlert'
  properties: {
    displayName: 'BioStack job failures'
    description: 'A BioStack Container Apps job execution failed or timed out.'
    severity: 2
    enabled: enabled
    scopes: [
      logAnalyticsWorkspaceId
    ]
    evaluationFrequency: 'PT15M'
    windowSize: 'PT30M'
    autoMitigate: false
    criteria: {
      allOf: [
        {
          query: '${systemLogs}| where isnotempty(Job)\n| where ${join(map(jobNamePrefixes, p => 'Job startswith \'${p}\''), ' or ')}\n| where Reason in ("BackoffLimitExceeded", "DeadlineExceeded") or Log contains "backoff limit" or Log contains "active longer than specified deadline"'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        owner.id
      ]
    }
  }
}

// 4. API error logs from the structured JSON console output (RedactingJsonConsoleFormatter).
resource apiErrorLogs 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'biostack-api-error-logs'
  location: location
  kind: 'LogAlert'
  properties: {
    displayName: 'BioStack API error logs'
    description: 'The API logged more than ${apiErrorLogThreshold} Error/Critical lines in 10 minutes.'
    severity: 2
    enabled: enabled
    scopes: [
      logAnalyticsWorkspaceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    autoMitigate: true
    criteria: {
      allOf: [
        {
          query: '${consoleLogs}| where App == \'${apiAppName}\'\n| extend level = tostring(parse_json(Log).level)\n| where level in ("Error", "Critical")'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: apiErrorLogThreshold
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        owner.id
      ]
    }
  }
}

output actionGroupId string = owner.id
output alertRuleNames array = [
  api5xx.name
  probeFailures.name
  jobFailures.name
  apiErrorLogs.name
]
