@description('Location for all resources')
param location string

@description('Prefix for generated resource names (keep short)')
param prefix string = 'triple'

@description('Blob container name for generated images')
param imagesContainerName string = 'images'

@description('Queue names')
param imageQueueName string = 'image-job-queue'
param startQueueName string = 'start-job-queue'

@description('Table name for job status tracking')
param jobStatusTableName string = 'jobstatus'

@description('API username for Basic Auth')
param apiUsername string = 'admin_hieu'

@description('API password for Basic Auth')
@secure()
param apiPassword string

var suffixFull = toLower(uniqueString(resourceGroup().id))
var suffix = substring(suffixFull, 0, 8)

var storageAccountName = toLower('${prefix}sa${suffix}')
var functionAppName = toLower('${prefix}-func-${suffixFull}')

// -------------------- Storage Account --------------------
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: '${storage.name}/default'
}

resource imagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${storage.name}/default/${imagesContainerName}'
  properties: {
    publicAccess: 'None'
  }
  dependsOn: [ blobService ]
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  name: '${storage.name}/default'
}

resource imageQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: '${storage.name}/default/${imageQueueName}'
  dependsOn: [ queueService ]
}

resource startQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  name: '${storage.name}/default/${startQueueName}'
  dependsOn: [ queueService ]
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  name: '${storage.name}/default'
}

resource jobStatusTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  name: '${storage.name}/default/${jobStatusTableName}'
  dependsOn: [ tableService ]
}

var storageKey = listKeys(storage.id, '2023-01-01').keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storageKey};EndpointSuffix=${environment().suffixes.storage}'

// -------------------- App Insights --------------------
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${functionAppName}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// -------------------- Consumption Plan (Y1) --------------------
resource plan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

// -------------------- Function App --------------------
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }

        { name: 'AzureWebJobsStorage', value: storageConnectionString }
        { name: 'STORAGE_CONNECTION_STRING', value: storageConnectionString }

        { name: 'IMAGES_CONTAINER', value: imagesContainerName }
        { name: 'IMAGE_QUEUE', value: imageQueueName }
        { name: 'START_QUEUE', value: startQueueName }
        { name: 'JOB_STATUS_TABLE', value: jobStatusTableName }

        // ? Basic Auth credentials for your API
        { name: 'API_USERNAME', value: apiUsername }
        { name: 'API_PASSWORD', value: apiPassword }

        { name: 'APPINSIGHTS_INSTRUMENTATIONKEY', value: appInsights.properties.InstrumentationKey }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
      ]
    }
  }
  dependsOn: [
    imagesContainer
    imageQueue
    startQueue
    jobStatusTable
    plan
    appInsights
  ]
}

// -------------------- Outputs --------------------
output functionAppName string = functionApp.name
output functionBaseUrl string = 'https://${functionApp.properties.defaultHostName}/api'
output storageAccount string = storage.name
output imageQueueName string = imageQueueName
output startQueueName string = startQueueName
output imagesContainer string = imagesContainerName
output jobStatusTable string = jobStatusTableName
output storageConnectionString string = storageConnectionString
