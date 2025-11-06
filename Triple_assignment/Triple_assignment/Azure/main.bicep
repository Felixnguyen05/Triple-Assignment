@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the storage account')
param storageAccountName string = 'tripleassignmentsa'

@description('Name of the function app')
param functionAppName string = 'tripleassignment-func'

/* -----------------------
   Storage Account + Queues
------------------------- */
resource storage 'Microsoft.Storage/storageAccounts@2023-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
  }
}

resource imageQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  name: '${storage.name}/default/image-job-queue'
}

resource startQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-09-01' = {
  name: '${storage.name}/default/start-job-queue'
}

/* -----------------------
   App Service Plan
------------------------- */
resource plan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'      // Consumption plan
    tier: 'Dynamic'
  }
}

/* -----------------------
   Function App
------------------------- */
resource func 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storage.listKeys().keys[0].value
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'IMAGE_QUEUE'
          value: 'image-job-queue'
        }
        {
          name: 'START_QUEUE'
          value: 'start-job-queue'
        }
      ]
    }
  }
}

/* -----------------------
   Outputs
------------------------- */
output storageConnectionString string = listKeys(storage.id, '2023-06-01').keys[0].value
output functionAppUrl string = func.properties.defaultHostName
