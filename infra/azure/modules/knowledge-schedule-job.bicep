// One scheduled KnowledgeWorker Container App Job (owner ruling 7, 2026-10-24).
// Secrets come only from Key Vault references resolved by the user-assigned identity;
// nothing secret is a parameter value or literal in this file.

param name string
param location string
param containerAppsEnvironmentId string
param image string
param acrLoginServer string
param identityResourceId string
param cronExpression string
param env array
@description('Container Apps secrets: [{ name, keyVaultUrl }]. Values are never inline.')
param keyVaultSecrets array = []
param storageName string
param replicaTimeoutSeconds int = 3600
param cpu string = '0.5'
param memory string = '1Gi'

resource job 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityResourceId}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironmentId
    configuration: {
      triggerType: 'Schedule'
      replicaTimeout: replicaTimeoutSeconds
      replicaRetryLimit: 0
      registries: [
        {
          server: acrLoginServer
          identity: identityResourceId
        }
      ]
      secrets: [for s in keyVaultSecrets: {
        name: s.name
        keyVaultUrl: s.keyVaultUrl
        identity: identityResourceId
      }]
      scheduleTriggerConfig: {
        cronExpression: cronExpression
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: image
          env: env
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          volumeMounts: [
            {
              volumeName: 'research-runtime'
              mountPath: '/app/research-runtime'
            }
          ]
        }
      ]
      volumes: [
        {
          name: 'research-runtime'
          storageType: 'AzureFile'
          storageName: storageName
        }
      ]
    }
  }
}

output jobName string = job.name
