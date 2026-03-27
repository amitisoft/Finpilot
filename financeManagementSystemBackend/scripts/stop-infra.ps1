$ErrorActionPreference = "Stop"
$podman = "C:\Program Files\RedHat\Podman\podman.exe"

if (-not (Test-Path $podman)) {
    throw "Podman executable was not found at '$podman'."
}

& $podman stop finpilot-postgres 2>$null | Out-Null
& $podman pod stop finpilot-dev 2>$null | Out-Null
