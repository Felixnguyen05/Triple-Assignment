@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the storage account')
param storageAccountName string = 'tripleassignmentsa'

/* Storage Account + Queues */
resource storage 'Microsoft.Storage/storageAccounts@2025-06-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: { minimumTlsVersion: 'TLS1_2', allowBlobPublicAccess: false }
}

resource imageQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  name: '${storage.name}/default/image-job-queue'
}

resource startQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  name: '${storage.name}/default/start-job-queue'
}

/* SAS Token - use string values */
param sasExpiryTime string = '2025-11-07T23:47:00Z'

output storageSasToken string = listAccountSas(storage.name, '2025-06-01', {
  services: 'b'
  resourceTypes: 'o'
  permissions: 'r'
  protocols: 'https'
  sharedAccessExpiryTime: sasExpiryTime
}).accountSasToken

output imageQueueName string = 'image-job-queue'
output startQueueName string = 'start-job-queue'
