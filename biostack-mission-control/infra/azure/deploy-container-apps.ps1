param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$BaseName,

    [Parameter(Mandatory = $true)]
    [string]$AuthSecret,

    [Parameter(Mandatory = $true)]
    [string]$JwtSecret,

    [Parameter(Mandatory = $true)]
    [string]$CallbackSecret,

    [string]$GoogleClientId = "",
    [string]$GoogleClientSecret = "",
    [string]$GitHubClientId = "",
    [string]$GitHubClientSecret = "",
    [string]$DiscordClientId = "",
    [string]$DiscordClientSecret = "",
    [string]$AppleClientId = "",
    [string]$AppleClientSecret = ""
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

$acrUsername = & (Require-AzCli) acr credential show --name $acrName --query username -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve ACR username." }

$acrPassword = & (Require-AzCli) acr credential show --name $acrName --query "passwords[0].value" -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve ACR password." }

Invoke-Az @("acr", "build", "--registry", $acrName, "--image", "biostack-api:latest", "--file", "backend/Dockerfile", "backend")

Invoke-Az @("containerapp", "env", "create", "--name", $envName, "--resource-group", $ResourceGroup, "--location", $Location)
Invoke-Az @("containerapp", "create", "--name", $apiAppName, "--resource-group", $ResourceGroup, "--environment", $envName, "--image", "$acrLoginServer/biostack-api:latest", "--target-port", "5000", "--ingress", "external", "--registry-server", $acrLoginServer, "--registry-username", $acrUsername, "--registry-password", $acrPassword)

$apiFqdn = & (Require-AzCli) containerapp show --name $apiAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve API FQDN." }
$apiUrl = "https://$apiFqdn"

Invoke-Az @("containerapp", "secret", "set", "--name", $apiAppName, "--resource-group", $ResourceGroup, "--secrets", "jwt-secret=$JwtSecret", "callback-secret=$CallbackSecret")
Invoke-Az @(
    "containerapp", "update",
    "--name", $apiAppName,
    "--resource-group", $ResourceGroup,
    "--set-env-vars",
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:5000",
    "Jwt__Issuer=biostack",
    "Jwt__Audience=biostack-ui",
    "ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db",
    "Cors__AllowedOrigins__0=https://placeholder",
    "Auth__CallbackSecret=secretref:callback-secret",
    "Jwt__Secret=secretref:jwt-secret",
    "--min-replicas", "1",
    "--max-replicas", "1"
)

Invoke-Az @("acr", "build", "--registry", $acrName, "--image", "biostack-web:latest", "--build-arg", "NEXT_PUBLIC_API_URL=$apiUrl", "--file", "frontend/Dockerfile", "frontend")

Invoke-Az @("containerapp", "create", "--name", $webAppName, "--resource-group", $ResourceGroup, "--environment", $envName, "--image", "$acrLoginServer/biostack-web:latest", "--target-port", "3000", "--ingress", "external", "--registry-server", $acrLoginServer, "--registry-username", $acrUsername, "--registry-password", $acrPassword)

$webFqdn = & (Require-AzCli) containerapp show --name $webAppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
if ($LASTEXITCODE -ne 0) { throw "Failed to resolve web FQDN." }
$webUrl = "https://$webFqdn"

Invoke-Az @("containerapp", "secret", "set", "--name", $webAppName, "--resource-group", $ResourceGroup, "--secrets", "auth-secret=$AuthSecret", "callback-secret=$CallbackSecret", "google-client-secret=$GoogleClientSecret", "github-client-secret=$GitHubClientSecret", "discord-client-secret=$DiscordClientSecret", "apple-client-secret=$AppleClientSecret")
Invoke-Az @(
    "containerapp", "update",
    "--name", $webAppName,
    "--resource-group", $ResourceGroup,
    "--set-env-vars",
    "NODE_ENV=production",
    "NEXT_PUBLIC_API_URL=$apiUrl",
    "AUTH_URL=$webUrl",
    "AUTH_TRUST_HOST=true",
    "AUTH_SECRET=secretref:auth-secret",
    "AUTH_CALLBACK_SECRET=secretref:callback-secret",
    "GOOGLE_CLIENT_ID=$GoogleClientId",
    "GOOGLE_CLIENT_SECRET=secretref:google-client-secret",
    "GITHUB_CLIENT_ID=$GitHubClientId",
    "GITHUB_CLIENT_SECRET=secretref:github-client-secret",
    "DISCORD_CLIENT_ID=$DiscordClientId",
    "DISCORD_CLIENT_SECRET=secretref:discord-client-secret",
    "APPLE_CLIENT_ID=$AppleClientId",
    "APPLE_CLIENT_SECRET=secretref:apple-client-secret",
    "--min-replicas", "1",
    "--max-replicas", "1"
)

Invoke-Az @(
    "containerapp", "update",
    "--name", $apiAppName,
    "--resource-group", $ResourceGroup,
    "--set-env-vars",
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://+:5000",
    "Jwt__Issuer=biostack",
    "Jwt__Audience=biostack-ui",
    "ConnectionStrings__DefaultConnection=Data Source=/app/data/biostack.db",
    "Cors__AllowedOrigins__0=$webUrl",
    "Auth__CallbackSecret=secretref:callback-secret",
    "Jwt__Secret=secretref:jwt-secret",
    "--min-replicas", "1",
    "--max-replicas", "1"
)

Write-Host ""
Write-Host "Deployment complete." -ForegroundColor Green
Write-Host "Frontend: $webUrl"
Write-Host "API:      $apiUrl"
Write-Host ""
Write-Host "Next step: update OAuth provider callback URLs to use $webUrl/api/auth/callback/{provider}"
