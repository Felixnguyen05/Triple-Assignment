# -------------------------
# Deployment Configuration
# -------------------------
$resourceGroup = "Hieu"
$location = "westeurope"
$bicepFile = "./azure/main.bicep"

# API credentials
$apiUsername = "admin_hieu"
$apiPassword = "neko-chann"

# -------------------------
# Login to Azure
# -------------------------
Write-Host "Logging into Azure..."
az login

# -------------------------
# Deploy Bicep Template
# -------------------------
Write-Host "Deploying infrastructure using Bicep..."
$deployment = az deployment group create `
    --resource-group $resourceGroup `
    --template-file $bicepFile `
    --query "properties.outputs" -o json | ConvertFrom-Json

$storageConnectionString = $deployment.storageConnectionString
$imageQueue = $deployment.imageQueueName
$startQueue = $deployment.startQueueName
$storageSasUrl = $deployment.storageSasToken

# -------------------------
# Show deployment outputs
# -------------------------
Write-Host "Storage connection string: $storageConnectionString"
Write-Host "Image queue name: $imageQueue"
Write-Host "Start queue name: $startQueue"
Write-Host "Storage SAS URL: $storageSasUrl"

# -------------------------
# Set environment variables for local development
# -------------------------
$env:AZURE_STORAGE_CONNECTION_STRING = $storageConnectionString
$env:IMAGE_QUEUE = $imageQueue
$env:START_QUEUE = $startQueue
$env:AZURE_STORAGE_SAS_URL = $storageSasUrl
$env:API_USERNAME = $apiUsername
$env:API_PASSWORD = $apiPassword

Write-Host "Deployment complete!"
Write-Host "Storage, queues, SAS URL, and API credentials have been configured."
