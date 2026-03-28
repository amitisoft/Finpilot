# FinPilot Backend

This README focuses on backend run/test/deploy tasks. For the full product overview and documentation map, start with:
- [../README.md](../README.md)
- [../docs/README.md](../docs/README.md)

## Prerequisites
- .NET 8 SDK
- Podman machine running
- PostgreSQL only for local infra (Redis is no longer required)

## Start everything with one command
```powershell
./scripts/start-local.ps1
```

Then open Swagger at:
- `http://localhost:5000/swagger`

## Run deployed frontend from backend
This builds the frontend into the backend `wwwroot` folder, starts local infra, and runs the API so the app is served from `http://localhost:5000`.

```powershell
./scripts/start-deployed.ps1
```

Fast path when frontend and backend are already built:

```powershell
./scripts/start-deployed.ps1 -NoFrontendBuild -NoBackendBuild
```

## Optional split commands
### Start infrastructure only
```powershell
./scripts/start-infra.ps1
```

### Run API only
```powershell
./scripts/run-api.ps1
```

## Health endpoints
- `/health`
- `/health/live`
- `/health/ready`

## Verification commands
```powershell
dotnet restore FinPilot.sln
dotnet build FinPilot.sln
dotnet test FinPilot.sln
```

## Deployment notes
For the current deployment path, use:
- [../docs/AZURE_DEPLOYMENT_PLAN.md](../docs/AZURE_DEPLOYMENT_PLAN.md)

Important production settings:
- `ConnectionStrings__Postgres`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SecretKey`
- `Jwt__AccessTokenMinutes`
- `Jwt__RefreshTokenDays`
- `Swagger__Enabled`
- `CORS_ALLOWED_ORIGINS` (only if frontend is hosted separately)

Common deployment failure:
- If GitHub Actions shows `No credentials found. Add an Azure login action before this action.`, the publish profile secret is missing or empty. Re-download the publish profile from Azure and save it as `AZURE_WEBAPP_PUBLISH_PROFILE`.
