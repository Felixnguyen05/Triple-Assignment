$resourceGroup = "Hieu"   
$location = "westeurope"             
$bicepFile = "./azure/main.bicep"

# Ensure Azure login
Write-Host "Logging into Azure..."
az login

# Deploy Bicep Template to existing resource group
Write-Host "Deploying infrastructure using Bicep..."
$deployment = az deployment group create `
    --resource-group $resourceGroup `
    --template-file $bicepFile `
    --query "properties.outputs" -o json | ConvertFrom-Json

$storageConnectionString = $deployment.storageConnectionString
$imageQueue = $deployment.imageQueueName
$startQueue = $deployment.startQueueName

Write-Host "Storage connection string: $storageConnectionString"
Write-Host "Image queue name: $imageQueue"
Write-Host "Start queue name: $startQueue"

# Set environment variables (for local scripts)
$env:AZURE_STORAGE_CONNECTION_STRING = $storageConnectionString
$env:IMAGE_QUEUE = $imageQueue
$env:START_QUEUE = $startQueue

Write-Host "Deployment complete! Only storage and queues were created in existing resource group."
