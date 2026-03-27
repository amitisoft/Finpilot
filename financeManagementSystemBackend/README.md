# FinPilot Backend

## Prerequisites
- .NET 8 SDK
- Podman machine running

## Start everything with one command
```powershell
./scripts/start-local.ps1
```

Then open Swagger at:
- `http://localhost:5000/swagger`

## Run deployed frontend from backend
This builds the frontend into the backend `wwwroot` folder, starts infra, and runs the API so the app is served from `http://localhost:5000`.

```powershell
./scripts/start-deployed.ps1
```

Fast path when frontend is already built and backend was already built:

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

## Test flow
1. `POST /api/auth/register`
2. Copy `accessToken`
3. Click **Authorize** in Swagger
4. Paste `Bearer <token>`
5. Test accounts, categories, transactions, budgets, goals, and dashboard

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

## Azure deployment notes
FinPilot is Azure-ready for the simplest deployment model: a single **Azure App Service** serving both the API and the built frontend bundle from `wwwroot`.

For the hackathon deployment path, caching now uses in-memory distributed cache, so Redis is not required.

Key production app settings:
- `ConnectionStrings__Postgres`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SecretKey`
- `Jwt__AccessTokenMinutes`
- `Jwt__RefreshTokenDays`
- `Swagger__Enabled`
- `CORS_ALLOWED_ORIGINS` (only needed when the frontend is hosted separately)

Branch usage:
- `develop` -> validation / active work
- `main` -> Azure deployment branch

GitHub workflows:
- `.github/workflows/validate-finpilot.yml`
- `.github/workflows/deploy-azure-app-service.yml`

Common deployment failure:
- If GitHub Actions shows `No credentials found. Add an Azure login action before this action.`, the publish profile secret is missing or empty. Re-download the publish profile from Azure and save it as `AZURE_WEBAPP_PUBLISH_PROFILE`.

Detailed checklist / architecture notes:
- `docs/AZURE_DEPLOYMENT_PLAN.md`
