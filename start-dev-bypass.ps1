param(
    [switch]$Detached
)

$previousBackend = [Environment]::GetEnvironmentVariable('DevBypass__Enabled', 'Process')
$previousFrontend = [Environment]::GetEnvironmentVariable('NEXT_PUBLIC_DEV_BYPASS_AUTH', 'Process')

try {
    $env:DevBypass__Enabled = 'true'
    $env:NEXT_PUBLIC_DEV_BYPASS_AUTH = 'true'

    $composeArgs = @('compose', '-f', 'docker-compose.dev.yml', 'up', '--build')
    if ($Detached) {
        $composeArgs += '-d'
    }

    & docker @composeArgs
    exit $LASTEXITCODE
}
finally {
    if ($null -eq $previousBackend) {
        Remove-Item Env:DevBypass__Enabled -ErrorAction SilentlyContinue
    } else {
        $env:DevBypass__Enabled = $previousBackend
    }

    if ($null -eq $previousFrontend) {
        Remove-Item Env:NEXT_PUBLIC_DEV_BYPASS_AUTH -ErrorAction SilentlyContinue
    } else {
        $env:NEXT_PUBLIC_DEV_BYPASS_AUTH = $previousFrontend
    }
}
