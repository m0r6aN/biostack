targetScope = 'subscription'

param roleName string
param assignableScopeId string

var roleDefinitionGuid = guid(subscription().id, roleName)

resource objectWriterRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: roleDefinitionGuid
  properties: {
    roleName: roleName
    description: 'Blob object read/list/write/delete and lease access. Container creation and deletion are excluded.'
    type: 'CustomRole'
    assignableScopes: [
      assignableScopeId
    ]
    permissions: [
      {
        actions: [
          'Microsoft.Storage/storageAccounts/blobServices/containers/read'
        ]
        notActions: [
          'Microsoft.Storage/storageAccounts/blobServices/containers/delete'
          'Microsoft.Storage/storageAccounts/blobServices/containers/write'
        ]
        dataActions: [
          'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read'
          'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/write'
          'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/add/action'
          'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/delete'
          'Microsoft.Storage/storageAccounts/blobServices/containers/blobs/move/action'
        ]
        notDataActions: []
      }
    ]
  }
}

output roleDefinitionId string = objectWriterRole.id
