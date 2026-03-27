param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

& (Join-Path $scriptRoot 'start-infra.ps1')

if ($NoBuild) {
    & (Join-Path $scriptRoot 'run-api.ps1') -NoBuild
}
else {
    & (Join-Path $scriptRoot 'run-api.ps1')
}
