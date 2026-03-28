# FinPilot

FinPilot is a hackathon-ready personal finance application that tells a complete money story for a user: capture accounts and transactions, track budgets and goals, surface insights, and layer on an AI-style financial coach grounded in the user's real data.

## What the app does today
- secure auth with JWT + refresh tokens
- account, category, and transaction management
- budgets with per-category allocation and tracking
- savings goals with progress tracking
- dashboard summaries, trends, and category breakdowns
- deterministic insights for monthly analysis, anomalies, budget risk, and goals
- agent-style coaching flows for coach, budget, anomaly, investment guidance, and reports
- audit/history visibility for important user actions
- a polished frontend served directly from the ASP.NET backend

## Product story
FinPilot was built in phases so the demo feels like a coherent product instead of a pile of features:
1. **Foundation** - establish secure auth, stable APIs, and a finance domain model.
2. **Money control** - add accounts, categories, transactions, budgets, and goals.
3. **Visibility** - add dashboard analytics and deterministic insights.
4. **Guidance** - add agent-style financial coaching on top of real data.
5. **Presentation** - ship a frontend that looks polished and follows the same product story.
6. **Deployment hardening** - simplify hosting for the hackathon by removing Redis and keeping deployment focused on the app plus PostgreSQL.

Read the full journey in [docs/DEVELOPMENT_HISTORY.md](docs/DEVELOPMENT_HISTORY.md).

## Architecture snapshot
- **Frontend:** React + TypeScript + Vite + Tailwind
- **Backend:** ASP.NET Core 8 + EF Core
- **Database:** PostgreSQL
- **Caching:** in-memory distributed cache for hackathon deployment
- **Auth:** JWT bearer + refresh tokens
- **CI/CD:** GitHub Actions
- **Hosting target:** Azure App Service or any .NET-compatible host, with PostgreSQL hosted separately

## Local run
### Backend + frontend served together
```powershell
cd financeManagementSystemBackend
./scripts/start-deployed.ps1
```
Open:
- `http://localhost:5000`

### Backend API only
```powershell
cd financeManagementSystemBackend
./scripts/start-local.ps1
```
Open:
- `http://localhost:5000/swagger`

## Demo path
A clean demo story for judges is:
1. Register or log in
2. Create an account
3. Add income + expense transactions
4. Create a monthly budget and a savings goal
5. Open dashboard + insights
6. Show the AI coach / widgets / advisory outputs
7. Show audit activity to prove the app tracks meaningful actions

## Documentation map
Start here:
- [docs/README.md](docs/README.md)

Most important docs:
- [docs/DEVELOPMENT_HISTORY.md](docs/DEVELOPMENT_HISTORY.md)
- [docs/FIGMA_FEATURE_LIST.md](docs/FIGMA_FEATURE_LIST.md)
- [docs/AZURE_DEPLOYMENT_PLAN.md](docs/AZURE_DEPLOYMENT_PLAN.md)
- [financeManagementSystemBackend/README.md](financeManagementSystemBackend/README.md)
