param(
    [switch]$NoFrontendInstall,
    [switch]$NoFrontendBuild,
    [switch]$NoBackendBuild
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendRoot = Split-Path -Parent $scriptRoot
$repoRoot = Split-Path -Parent $backendRoot
$frontendRoot = Join-Path $repoRoot 'financeManagementSystemFrontend'

if (-not (Test-Path (Join-Path $frontendRoot 'package.json'))) {
    throw "Frontend project was not found at '$frontendRoot'."
}

if (-not $NoFrontendInstall -and -not (Test-Path (Join-Path $frontendRoot 'node_modules'))) {
    Write-Host 'Installing frontend dependencies...' -ForegroundColor Cyan
    Push-Location $frontendRoot
    try {
        npm install --no-fund --no-audit
    }
    finally {
        Pop-Location
    }
}

if (-not $NoFrontendBuild) {
    Write-Host 'Building frontend into backend wwwroot...' -ForegroundColor Cyan
    Push-Location $frontendRoot
    try {
        npm run build
    }
    finally {
        Pop-Location
    }
}

& (Join-Path $scriptRoot 'start-infra.ps1')

if ($NoBackendBuild) {
    & (Join-Path $scriptRoot 'run-api.ps1') -NoBuild
}
else {
    & (Join-Path $scriptRoot 'run-api.ps1')
}
