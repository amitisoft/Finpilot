param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5000'

function Stop-ExistingFinPilotApi {
    $currentPid = $PID

    $processes = Get-CimInstance Win32_Process | Where-Object {
        $_.ProcessId -ne $currentPid -and (
            $_.Name -ieq 'FinPilot.Api.exe' -or
            ($_.Name -ieq 'dotnet.exe' -and $_.CommandLine -match 'FinPilot\.Api(\.csproj|\.dll)')
        )
    }

    foreach ($process in $processes) {
        Write-Host "Stopping existing FinPilot.Api process $($process.ProcessId)..." -ForegroundColor Yellow
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
    }

    if ($processes) {
        Start-Sleep -Seconds 1
    }
}

Stop-ExistingFinPilotApi

if (-not $NoBuild) {
    dotnet build "$root\FinPilot.sln"
    dotnet run --project "$root\src\FinPilot.Api\FinPilot.Api.csproj" --no-launch-profile
}
else {
    dotnet run --project "$root\src\FinPilot.Api\FinPilot.Api.csproj" --no-launch-profile --no-build
}
