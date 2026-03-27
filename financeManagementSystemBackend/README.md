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

