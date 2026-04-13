param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$BaseName,

    [Parameter(Mandatory = $true)]
    [string]$JwtSecret,

    [string]$DatabaseProvider = "sqlite",
    [string]$PostgresConnectionString = "",
    [string]$WebUrlOverride = "",
    [string]$ApiUrlOverride = ""
)

$ErrorActionPreference = "Stop"

function Require-AzCli {
    $azCmd = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    if (-not (Test-Path $azCmd)) {
        throw "Azure CLI not found at $azCmd"
    }
    return $azCmd
}

function Invoke-Az {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $azCmd = Require-AzCli
    Write-Host ("az " + ($Arguments -join " ")) -ForegroundColor Cyan
    & $azCmd @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: az $($Arguments -join ' ')"
    }
}

$acrName = (($BaseName -replace "[^a-zA-Z0-9]", "") + "acr").ToLower()
if ($acrName.Length -gt 50) {
    $acrName = $acrName.Substring(0, 50)
}

$envName = "$BaseName-env"
$apiAppName = "$BaseName-api"
$webAppName = "$BaseName-web"
Invoke-Az @("group", "create", "--name", $ResourceGroup, "--location", $Location)
Invoke-Az @("provider", "register", "--namespace", "Microsoft.App")
Invoke-Az @("provider", "register", "--namespace", "Microsoft.OperationalInsights")
Invoke-Az @("provider", "register", "--namespace", "Microsoft.ContainerRegistry")
Invoke-Az @("provider", "register", "--namespace", "Microsoft.Storage")

Invoke-Az @("acr", "create", "--resource-group", $ResourceGroup, "--name", $acrName, "--sku", "Basic", "--admin-enabled", "true")

$acrLoginServer = & (Require-AzCli) acr show --name $acrName --resource-group $ResourceGroup --query loginServer -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve ACR login server." }

$acrUsername = & (Require-AzCli) acr credential show --name $acrName --resource-group $ResourceGroup --query username -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve ACR username." }

$acrPassword = & (Require-AzCli) acr credential show --name $acrName --resource-group $ResourceGroup --query "passwords[0].value" -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve ACR password." }

Invoke-Az @("acr", "build", "--resource-group", $ResourceGroup, "--registry", $acrName, "--image", "biostack-api:latest", "--file", "backend/Dockerfile", "backend")

Invoke-Az @("containerapp", "env", "create", "--name", $envName, "--resource-group", $ResourceGroup, "--location", $Location)
Invoke-Az @("containerapp", "create", "--name", $apiAppName, "--resource-group", $ResourceGroup, "--environment", $envName, "--image", "$acrLoginServer/biostack-api:latest", "--target-port", "5000", "--ingress", "external", "--registry-server", $acrLoginServer, "--registry-username", $acrUsername, "--registry-password", $acrPassword)

$apiFqdn = & (Require-AzCli) containerapp show --name $apiAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve API FQDN." }
$apiUrl = "https://$apiFqdn"
$publicApiUrl = if ([string]::IsNullOrWhiteSpace($ApiUrlOverride)) { $apiUrl } else { $ApiUrlOverride }

$apiSecrets = @(
    "jwt-secret=$JwtSecret"
)

$apiEnvVars = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:5000",
    "Jwt__Issuer=biostack",
    "Jwt__Audience=biostack-ui",
    "Cors__AllowedOrigins__0=https://placeholder",
    "PublicApiUrl=$publicApiUrl",
    "FrontendUrl=https://placeholder",
    "Jwt__Secret=secretref:jwt-secret"
)

$usePostgres = $DatabaseProvider.Equals("postgres", [System.StringComparison]::OrdinalIgnoreCase) -or
    $DatabaseProvider.Equals("postgresql", [System.StringComparison]::OrdinalIgnoreCase) -or
    $DatabaseProvider.Equals("npgsql", [System.StringComparison]::OrdinalIgnoreCase)

if ($usePostgres) {
    if ([string]::IsNullOrWhiteSpace($PostgresConnectionString)) {
        throw "PostgresConnectionString is required when DatabaseProvider is set to PostgreSQL."
    }

    $apiSecrets += "db-conn-string=$PostgresConnectionString"
    $apiEnvVars += "ConnectionStrings__DefaultConnection=secretref:db-conn-string"
    $apiEnvVars += "Database__Provider=postgresql"
}
else {
    $apiEnvVars += "ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db"
}

$apiSecretArgs = @(
    "containerapp", "secret", "set",
    "--name", $apiAppName,
    "--resource-group", $ResourceGroup,
    "--secrets"
) + $apiSecrets

Invoke-Az -Arguments $apiSecretArgs

$initialApiUpdateArgs = @(
    "containerapp", "update",
    "--name", $apiAppName,
    "--resource-group", $ResourceGroup,
    "--set-env-vars"
) + $apiEnvVars + @(
    "--min-replicas", "1",
    "--max-replicas", "1"
)

Invoke-Az -Arguments $initialApiUpdateArgs

Invoke-Az @("acr", "build", "--resource-group", $ResourceGroup, "--registry", $acrName, "--image", "biostack-web:latest", "--build-arg", "NEXT_PUBLIC_API_URL=$publicApiUrl", "--file", "frontend/Dockerfile", "frontend")

Invoke-Az @("containerapp", "create", "--name", $webAppName, "--resource-group", $ResourceGroup, "--environment", $envName, "--image", "$acrLoginServer/biostack-web:latest", "--target-port", "3000", "--ingress", "external", "--registry-server", $acrLoginServer, "--registry-username", $acrUsername, "--registry-password", $acrPassword)

$webFqdn = & (Require-AzCli) containerapp show --name $webAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve web FQDN." }
$webUrl = "https://$webFqdn"
$publicWebUrl = if ([string]::IsNullOrWhiteSpace($WebUrlOverride)) { $webUrl } else { $WebUrlOverride }

$webSecrets = @()

$webEnvVars = @(
    "NODE_ENV=production",
    "NEXT_PUBLIC_API_URL=$publicApiUrl"
)

if ($webSecrets.Count -gt 0) {
    $webSecretArgs = @(
        "containerapp", "secret", "set",
        "--name", $webAppName,
        "--resource-group", $ResourceGroup,
        "--secrets"
    ) + $webSecrets

    Invoke-Az -Arguments $webSecretArgs
}

$webUpdateArgs = @(
    "containerapp", "update",
    "--name", $webAppName,
    "--resource-group", $ResourceGroup,
    "--set-env-vars"
) + $webEnvVars + @(
    "--min-replicas", "1",
    "--max-replicas", "1"
)

Invoke-Az -Arguments $webUpdateArgs

$apiFinalEnvVars = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:5000",
    "Jwt__Issuer=biostack",
    "Jwt__Audience=biostack-ui",
    "Cors__AllowedOrigins__0=$publicWebUrl",
    "PublicApiUrl=$publicApiUrl",
    "FrontendUrl=$publicWebUrl",
    "Jwt__Secret=secretref:jwt-secret"
)

if ($usePostgres) {
    $apiFinalEnvVars += "ConnectionStrings__DefaultConnection=secretref:db-conn-string"
    $apiFinalEnvVars += "Database__Provider=postgresql"
}
else {
    $apiFinalEnvVars += "ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db"
}

$apiFinalUpdateArgs = @(
    "containerapp", "update",
    "--name", $apiAppName,
    "--resource-group", $ResourceGroup,
    "--set-env-vars"
) + $apiFinalEnvVars + @(
    "--min-replicas", "1",
    "--max-replicas", "1"
)

Invoke-Az -Arguments $apiFinalUpdateArgs

Write-Host ""
Write-Host "Deployment complete." -ForegroundColor Green
Write-Host "Frontend: $publicWebUrl"
Write-Host "API:      $publicApiUrl"
Write-Host ""
Write-Host "Passwordless email auth is configured. Use /dev/auth/inbox only in local development."
