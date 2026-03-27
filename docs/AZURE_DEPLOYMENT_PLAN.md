# Azure Deployment Plan (Hackathon-Safe)

## Goal
Deploy the current FinPilot codebase to Azure with the least operational complexity while keeping the app production-looking.

## Target architecture
- **Azure App Service (Linux, .NET 8)**: serves the ASP.NET Core API and the built React frontend from `wwwroot`
- **Azure Database for PostgreSQL Flexible Server**: primary relational database
- **Azure Managed Redis / Azure Cache for Redis**: dashboard and insights caching

## Why this architecture
The frontend already builds into `financeManagementSystemBackend/src/FinPilot.Api/wwwroot`, so a single App Service is the cleanest deployment target for the current repo. It avoids extra frontend hosting work and removes cross-origin complexity unless a split deployment is introduced later.

## Branch strategy for deployment
- **develop**: active implementation branch
  - validated by `.github/workflows/validate-finpilot.yml`
- **main**: deployment branch
  - validated and deployed by `.github/workflows/deploy-azure-app-service.yml`

This keeps hackathon development simple: iterate on `develop`, merge to `main` when you want Azure deployment.

## Code changes included
1. **Configurable CORS**
   - Localhost origins stay in `appsettings.Development.json`
   - Production can use either:
     - `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...
     - or the simpler `CORS_ALLOWED_ORIGINS` comma-separated app setting
2. **Configurable Swagger**
   - Controlled with `Swagger__Enabled`
   - Enabled in development by default, disabled in production by default
3. **Azure-safe Data Protection key path**
   - Uses `$HOME/site/data-protection-keys` when `HOME` is present (Azure App Service compatible)
4. **GitHub Actions validation and deployment workflows**
   - `validate-finpilot.yml` builds frontend and runs backend tests on `develop`/`main`
   - `deploy-azure-app-service.yml` builds frontend, tests backend, publishes, and deploys on `main`

## Required Azure resources
1. Resource Group
2. App Service Plan
3. Web App (.NET 8, Linux)
4. Azure Database for PostgreSQL Flexible Server
5. Azure Managed Redis / Azure Cache for Redis

## Required GitHub repo settings
### Secret
- `AZURE_WEBAPP_PUBLISH_PROFILE`

### Variable
- `AZURE_WEBAPP_NAME`

## Required Azure App Service application settings
- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__Postgres=Host=<server>.postgres.database.azure.com;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true`
- `Redis__ConnectionString=<redis-connection-string>`
- `Jwt__Issuer=FinPilot.Api`
- `Jwt__Audience=FinPilot.Client`
- `Jwt__SecretKey=<very-long-random-secret>`
- `Jwt__AccessTokenMinutes=60`
- `Jwt__RefreshTokenDays=7`
- `Swagger__Enabled=false`
- `CORS_ALLOWED_ORIGINS=https://<frontend-host>` only if frontend is hosted separately from the API

## Deployment flow
1. Create Azure resources
2. Configure App Service application settings
3. Download the App Service publish profile
4. Save the publish profile as the GitHub secret `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Save the App Service name as the GitHub variable `AZURE_WEBAPP_NAME`
6. Push feature work to `develop`
7. Once ready, merge or fast-forward to `main`
8. GitHub Actions deploys the published output to Azure

## Verification checklist
- `GET /health` returns healthy
- app root `/` loads the React SPA
- register/login works
- database migrations run on startup
- dashboard loads with seeded/default data
- logs show successful PostgreSQL and Redis connections

## Later enhancements (not required for hackathon)
- Split frontend to Azure Static Web Apps
- Use Key Vault references for secrets
- Replace file-system Data Protection with Blob/Redis-backed persistence for scale-out
- Add staging slot deployment from `develop`
