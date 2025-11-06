$resourceGroup = "triple-resource"
$location = "westeurope"
$functionAppName = "tripleassignment-func"
$bicepFile = "./azure/main.bicep"
$projectPath = "./Triple_assignment.csproj"

# Ensure Azure login
Write-Host "Logging into Azure..."
az login

# Create Resource Group
Write-Host "Creating resource group..."
az group create --name $resourceGroup --location $location

# Deploy Bicep template first
$deployment = az deployment group create `
  --resource-group $resourceGroup `
  --template-file $bicepFile `
  --parameters functionAppName=$functionAppName `
  --query "properties.outputs" -o json | ConvertFrom-Json

$storageConnectionString = $deployment.storageConnectionString
$functionAppUrl = $deployment.functionAppUrl

Write-Host "Storage connection string: $storageConnectionString"
Write-Host "Function URL: $functionAppUrl"

# Wait a few seconds for the function app to be fully provisioned
Start-Sleep -Seconds 15

# Then publish
func azure functionapp publish $functionAppName --csharp

# Set environment variables
$env:AZURE_STORAGE_CONNECTION_STRING = $storageConnectionString
$env:IMAGE_QUEUE = "image-job-queue"
$env:START_QUEUE = "start-job-queue"

# Build and publish function
Write-Host "Building function project..."
dotnet build $projectPath --configuration Release

Write-Host "Publishing function app to Azure..."
func azure functionapp publish $functionAppName --csharp

Write-Host "Deployment complete!"
Write-Host "Your function is available at: $functionAppUrl"
$resourceGroup = "triple-resource"
$location = "westeurope"
$functionAppName = "tripleassignment-func"
$bicepFile = "./azure/main.bicep"
$projectPath = "./src/Triple_assignment.csproj"

# Ensure Azure login
Write-Host "Logging into Azure..."
az login

# Create Resource Group
Write-Host "Creating resource group..."
az group create --name $resourceGroup --location $location

# Deploy Bicep Template
Write-Host "Deploying infrastructure using Bicep..."
$deployment = az deployment group create `
    --resource-group $resourceGroup `
    --template-file $bicepFile `
    --parameters functionAppName=$functionAppName `
    --query "properties.outputs" -o json | ConvertFrom-Json

$storageConnectionString = $deployment.storageConnectionString
$functionAppUrl = $deployment.functionAppUrl

Write-Host "Storage connection string: $storageConnectionString"
Write-Host "Function URL: $functionAppUrl"

# Set environment variables
$env:AZURE_STORAGE_CONNECTION_STRING = $storageConnectionString
$env:IMAGE_QUEUE = "image-job-queue"
$env:START_QUEUE = "start-job-queue"

# Build and publish function
Write-Host "Building function project..."
dotnet build $projectPath --configuration Release

Write-Host "Publishing function app to Azure..."
func azure functionapp publish $functionAppName --csharp

Write-Host "Deployment complete!"
Write-Host "Your function is available at: $functionAppUrl"
