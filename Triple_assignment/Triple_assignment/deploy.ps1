param(
    [string]$resourceGroup = "Hieu",
    [string]$location = "swedencentral",
    [string]$bicepFile = "./Azure/main.bicep",
    [string]$projectPath = "./Triple_assignment.csproj",
    [string]$prefix = "triple",

    # ✅ Basic Auth credentials (share these with your teacher)
    [string]$apiUsername = "admin_hieu",
    [string]$apiPassword = "neko-chann"
)

$ErrorActionPreference = "Stop"

Write-Host "=== deploy.ps1 v7 (ARM REST deploy + zip deploy + API creds) ==="

Write-Host "==> Ensuring Azure session..."
try { az account show | Out-Null } catch { az login | Out-Null }

$subId = (az account show --query id -o tsv).Trim()
if ([string]::IsNullOrWhiteSpace($subId)) { throw "Could not read subscription id from az account show." }
Write-Host "==> Subscription: $subId"

# ✅ Create RG only if missing (do NOT try to change location)
$rgExists = az group exists --name $resourceGroup -o tsv
if ($rgExists -ne "true") {
    Write-Host "==> Creating resource group: $resourceGroup ($location)"
    az group create --name $resourceGroup --location $location | Out-Null
} else {
    Write-Host "==> Resource group exists: $resourceGroup (keeping original location)"
}

Write-Host "==> Building Bicep to ARM JSON..."
$azureDir = Split-Path -Parent $bicepFile
if ([string]::IsNullOrWhiteSpace($azureDir)) { $azureDir = "." }

az bicep build --file $bicepFile --outdir $azureDir | Out-Null

$templateJsonPath = [System.IO.Path]::ChangeExtension($bicepFile, ".json")
if (-not (Test-Path $templateJsonPath)) {
    throw "Bicep build did not produce $templateJsonPath"
}
Write-Host "==> Template JSON: $templateJsonPath"

$template = Get-Content $templateJsonPath -Raw | ConvertFrom-Json

$deploymentName = "deploy-" + (Get-Date -Format "yyyyMMdd-HHmmss")
Write-Host "==> Deploying via ARM REST: $deploymentName"

$token = (az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv).Trim()
if ([string]::IsNullOrWhiteSpace($token)) { throw "Could not obtain ARM access token." }

$path = "/subscriptions/$subId/resourceGroups/$resourceGroup/providers/Microsoft.Resources/deployments/$deploymentName"
$uri = "https://management.azure.com$path`?api-version=2021-04-01"
$uriObj = [Uri]$uri

Write-Host "==> ARM URI: $($uriObj.AbsoluteUri)"

$body = @{
    properties = @{
        mode = "Incremental"
        template = $template
        parameters = @{
            location    = @{ value = $location }
            prefix      = @{ value = $prefix }
            apiUsername = @{ value = $apiUsername }
            apiPassword = @{ value = $apiPassword }
        }
    }
} | ConvertTo-Json -Depth 100

$headers = @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Method Put -Uri $uriObj -Headers $headers -Body $body | Out-Null

Write-Host "==> Waiting for deployment to complete..."
$state = ""
$attempts = 0
do {
    Start-Sleep -Seconds 5
    $attempts++

    $result = Invoke-RestMethod -Method Get -Uri $uriObj -Headers $headers
    $state = $result.properties.provisioningState
    Write-Host "   - provisioningState: $state"

    if ($attempts -gt 120) { throw "Deployment timed out after ~10 minutes." }
} while ($state -eq "Running" -or $state -eq "Accepted")

if ($state -ne "Succeeded") {
    $err = $result.properties.error | ConvertTo-Json -Depth 50
    Write-Host "Deployment failed details:"
    Write-Host $err
    throw "Deployment provisioningState is $state"
}

$outputs = $result.properties.outputs
if (-not $outputs -or -not $outputs.functionAppName -or [string]::IsNullOrWhiteSpace($outputs.functionAppName.value)) {
    throw "Deployment succeeded but outputs missing functionAppName."
}

$functionAppName = $outputs.functionAppName.value
$functionBaseUrl = $outputs.functionBaseUrl.value

Write-Host "==> Outputs:"
Write-Host "Function App: $functionAppName"
Write-Host "Base URL: $functionBaseUrl"

Write-Host "==> Building Function App: $projectPath"
$publishDir = Join-Path $PSScriptRoot "publish"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir | Out-Null

dotnet publish $projectPath -c Release -o $publishDir

Write-Host "==> Creating zip package"
$zipPath = Join-Path $PSScriptRoot "functionapp.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $zipPath)

Write-Host "==> Deploying package to Azure Function App (zip deploy)"
az functionapp deployment source config-zip `
    --resource-group $resourceGroup `
    --name $functionAppName `
    --src $zipPath | Out-Null

Write-Host "==> Fetching Function host keys"
$keysJson = az functionapp keys list --resource-group $resourceGroup --name $functionAppName -o json
$keys = $keysJson | ConvertFrom-Json

$hostKey = $null
if ($keys.functionKeys.default) { $hostKey = $keys.functionKeys.default }
elseif ($keys.masterKey) { $hostKey = $keys.masterKey }
else {
    $first = $keys.functionKeys.PSObject.Properties | Select-Object -First 1
    if ($first) { $hostKey = $first.Value }
}

Write-Host ""
Write-Host "==================== DEPLOYMENT COMPLETE ===================="
Write-Host "Function Base URL: $functionBaseUrl"
Write-Host "Use this header for authentication:"
Write-Host "x-functions-key: $hostKey"
Write-Host "Basic Auth credentials:"
Write-Host "Username: $apiUsername"
Write-Host "Password: $apiPassword"
Write-Host "============================================================="
Write-Host ""
