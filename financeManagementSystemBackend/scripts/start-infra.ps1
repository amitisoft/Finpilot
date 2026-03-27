param(
    [string]$EnvFile = ".env"
)

$ErrorActionPreference = "Stop"

$podman = "C:\Program Files\RedHat\Podman\podman.exe"
$root = Split-Path -Parent $PSScriptRoot

if (-not (Test-Path $podman)) {
    throw "Podman executable was not found at '$podman'."
}

$settings = @{
    POSTGRES_DB = "finpilot"
    POSTGRES_USER = "postgres"
    POSTGRES_PASSWORD = "postgres"
    POSTGRES_PORT = "5432"
}

$envPath = Join-Path $root $EnvFile
if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        if ($_ -match "^\s*#" -or [string]::IsNullOrWhiteSpace($_)) { return }
        $parts = $_ -split "=", 2
        if ($parts.Count -eq 2) {
            $settings[$parts[0].Trim()] = $parts[1].Trim()
        }
    }
}

& $podman machine start | Out-Null

& $podman info | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Podman is not reachable. Verify `podman machine init` and `podman machine start` have completed successfully."
}

& $podman pod exists finpilot-dev | Out-Null
if ($LASTEXITCODE -ne 0) {
    & $podman pod create --name finpilot-dev -p "$($settings.POSTGRES_PORT):5432" | Out-Null
}

& $podman container exists finpilot-postgres | Out-Null
if ($LASTEXITCODE -ne 0) {
    & $podman volume create finpilot-postgres-data | Out-Null
    & $podman run -d --name finpilot-postgres --pod finpilot-dev -e "POSTGRES_DB=$($settings.POSTGRES_DB)" -e "POSTGRES_USER=$($settings.POSTGRES_USER)" -e "POSTGRES_PASSWORD=$($settings.POSTGRES_PASSWORD)" -v "finpilot-postgres-data:/var/lib/postgresql/data" docker.io/library/postgres:15-alpine | Out-Null
} else {
    & $podman start finpilot-postgres | Out-Null
}

& $podman ps --filter "pod=finpilot-dev"
