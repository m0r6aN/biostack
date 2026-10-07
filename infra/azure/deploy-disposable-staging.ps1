param(
    [string]$ResourceGroup = "biostack-money-makers-stg-20260728",
    [string]$Location = "eastus2",
    [string]$BaseName = "bsmoney0728",
    [string]$ExpiresAt = "2026-07-30T23:59:59Z"
)

$ErrorActionPreference = "Stop"

$acrName = "${BaseName}acr"
$environmentName = "$BaseName-env"
$apiAppName = "$BaseName-api"
$webAppName = "$BaseName-web"
$acrLoginServer = "$acrName.azurecr.io"
$tags = @(
    "owner=Clint Morgan",
    "initiative=money-makers",
    "environment=staging",
    "expires-at=$ExpiresAt"
)

function Invoke-AzSafe {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $displayArguments = @($Arguments)
    $secretFlags = @("--secrets", "--registry-password", "--password", "--client-secret")
    for ($i = 0; $i -lt $displayArguments.Count; $i++) {
        if ($secretFlags -contains $displayArguments[$i]) {
            for ($j = $i + 1; $j -lt $displayArguments.Count; $j++) {
                if ($displayArguments[$j] -like "--*") { break }
                $displayArguments[$j] = "<redacted>"
            }
        }
    }

    Write-Host ("az " + ($displayArguments -join " ")) -ForegroundColor Cyan
    & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed."
    }
}

function Get-AzText {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $value = & az @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Azure CLI query failed." }
    return ($value | Out-String).Trim()
}

function Test-AzResource {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & az @Arguments 1>$null 2>$null
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    return $exitCode -eq 0
}

if (-not (Test-AzResource @("group", "show", "--name", $ResourceGroup))) {
    Invoke-AzSafe -Arguments (@("group", "create", "--name", $ResourceGroup, "--location", $Location, "--tags") + $tags)
}

if (-not (Test-AzResource @("containerapp", "env", "show", "--name", $environmentName, "--resource-group", $ResourceGroup))) {
    Invoke-AzSafe -Arguments (@("containerapp", "env", "create", "--name", $environmentName, "--resource-group", $ResourceGroup, "--location", $Location, "--tags") + $tags)
}

$jwtSecret = [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Maximum 256) }))

if (-not (Test-AzResource @("containerapp", "show", "--name", $apiAppName, "--resource-group", $ResourceGroup))) {
    $apiArguments = @(
        "containerapp", "create", "--name", $apiAppName, "--resource-group", $ResourceGroup,
        "--environment", $environmentName, "--image", "$acrLoginServer/biostack-api:latest",
        "--target-port", "5000", "--ingress", "external", "--registry-server", $acrLoginServer,
        "--registry-identity", "system", "--system-assigned",
        "--secrets", "jwt-secret=$jwtSecret",
        "--env-vars", "ASPNETCORE_ENVIRONMENT=Production", "ASPNETCORE_URLS=http://+:5000",
        "Jwt__Issuer=biostack", "Jwt__Audience=biostack-ui",
        "Cors__AllowedOrigins__0=https://placeholder.invalid", "PublicApiUrl=https://placeholder.invalid",
        "FrontendUrl=https://placeholder.invalid", "Jwt__Secret=secretref:jwt-secret",
        "ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db",
        "--min-replicas", "1", "--max-replicas", "1", "--cpu", "0.5", "--memory", "1Gi", "--tags"
    ) + $tags
    Invoke-AzSafe -Arguments $apiArguments
}

$apiFqdn = Get-AzText -Arguments @("containerapp", "show", "--name", $apiAppName, "--resource-group", $ResourceGroup, "--query", "properties.configuration.ingress.fqdn", "-o", "tsv")
$apiUrl = "https://$apiFqdn"

if (-not (Test-AzResource @("containerapp", "show", "--name", $webAppName, "--resource-group", $ResourceGroup))) {
    $webArguments = @(
        "containerapp", "create", "--name", $webAppName, "--resource-group", $ResourceGroup,
        "--environment", $environmentName, "--image", "$acrLoginServer/biostack-web:latest",
        "--target-port", "3000", "--ingress", "external", "--registry-server", $acrLoginServer,
        "--registry-identity", "system", "--system-assigned",
        "--env-vars", "NODE_ENV=production", "NEXT_PUBLIC_API_URL=$apiUrl",
        "--min-replicas", "1", "--max-replicas", "1", "--cpu", "0.5", "--memory", "1Gi", "--tags"
    ) + $tags
    Invoke-AzSafe -Arguments $webArguments
}

$webFqdn = Get-AzText -Arguments @("containerapp", "show", "--name", $webAppName, "--resource-group", $ResourceGroup, "--query", "properties.configuration.ingress.fqdn", "-o", "tsv")
$webUrl = "https://$webFqdn"

Invoke-AzSafe -Arguments @(
    "containerapp", "update", "--name", $apiAppName, "--resource-group", $ResourceGroup,
    "--set-env-vars", "Cors__AllowedOrigins__0=$webUrl", "PublicApiUrl=$apiUrl", "FrontendUrl=$webUrl"
)

Write-Host "Staging API: $apiUrl" -ForegroundColor Green
Write-Host "Staging web: $webUrl" -ForegroundColor Green
