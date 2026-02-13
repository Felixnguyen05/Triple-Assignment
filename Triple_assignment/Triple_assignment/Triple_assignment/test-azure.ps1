$baseUrl = "https://triple-func-obpnd3sto4eio.azurewebsites.net/api"
$funcKey = "vovH4kaO4Z3VnuD_w3Jfx64g2yrydBrvZX2O23DqJRgqAzFuXwZMEg=="

$user = "admin_hieu"
$pass = "neko-chann"
$basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$user`:$pass"))

$headers = @{
  "x-functions-key" = $funcKey
  "Authorization"   = "Basic $basic"
  "Content-Type"    = "application/json"
}

$response = Invoke-RestMethod -Uri "$baseUrl/start" -Method POST -Headers $headers -Body "{}"
$processId = $response.processId

$status = Invoke-RestMethod -Uri "$baseUrl/status/$processId" -Method GET -Headers $headers
$images = Invoke-RestMethod -Uri "$baseUrl/images/$processId" -Method GET -Headers $headers

$response
$status
$images
