param(
    [string]$ApiUrl = "https://bsmoney0728-api.icysmoke-19dfe76e.eastus2.azurecontainerapps.io",
    [string]$WebUrl = "https://bsmoney0728-web.icysmoke-19dfe76e.eastus2.azurecontainerapps.io"
)

$ErrorActionPreference = "Continue"
$checks = @(
    @{ Name = "api-health"; Url = "$ApiUrl/health"; Expected = 200 },
    @{ Name = "api-keon-health"; Url = "$ApiUrl/health/keon"; Expected = 503 },
    @{ Name = "web-home"; Url = $WebUrl; Expected = 200 },
    @{ Name = "api-protected-route"; Url = "$ApiUrl/api/v1/billing/subscription"; Expected = 401 }
)

$results = foreach ($check in $checks) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $check.Url -TimeoutSec 30
        [pscustomobject]@{
            Name = $check.Name
            Url = $check.Url
            StatusCode = [int]$response.StatusCode
            Error = $null
            Pass = ([int]$response.StatusCode -eq $check.Expected)
        }
    }
    catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        [pscustomobject]@{
            Name = $check.Name
            Url = $check.Url
            StatusCode = $statusCode
            Error = $_.Exception.Message
            Pass = ($statusCode -eq $check.Expected)
        }
    }
}

$results | ConvertTo-Json -Depth 3
if ($results | Where-Object { -not $_.Pass }) { exit 1 }
