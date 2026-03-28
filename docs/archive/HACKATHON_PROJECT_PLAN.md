# Hackathon Project Plan — FinPilot

## 1. Project Vision
Build an **AI-assisted Personal Finance Tracker** that helps users understand spending, control budgets, and make smarter money decisions through a modern dashboard, actionable insights, and clean financial workflows.

## 2. Why This Can Win
- Solves a real daily problem: money visibility and planning
- Combines strong engineering fundamentals with a polished UX
- Adds a hackathon-worthy differentiator: **AI financial insights**
- Demo-friendly: clear before/after impact with charts, budgets, and recommendations

## 3. Selected Stack

### Frontend
- React 18 + TypeScript
- Vite
- Tailwind CSS
- TanStack Query
- Zustand
- React Hook Form + Zod
- Recharts

### Backend
- ASP.NET Core 8 Web API
- Entity Framework Core
- PostgreSQL
- Redis for dashboard caching
- JWT + refresh tokens
- Swagger / OpenAPI

### DevOps
- Docker + Docker Compose
- GitHub Actions

## 4. Product Theme
**FinPilot** — “Track money, predict trends, and get guided next steps.”

## 5. Core User Journey
1. User signs up / logs in
2. Adds accounts and transactions
3. Sees categorized spending on dashboard
4. Creates monthly budgets and savings goals
5. Receives AI-generated insights:
   - unusual spending
   - overspending risk
   - savings recommendations
   - category-level trends

## 6. Scope Plan

### MVP — Must Ship
- Authentication (JWT + refresh token rotation)
- Dashboard with totals, charts, and recent transactions
- Transaction CRUD
- Categories
- Budget creation and tracking
- Savings goals
- Responsive UI
- Swagger docs
- Dockerized local setup

### Demo Differentiators — High Priority
- AI “Financial Coach” insights panel
- Smart monthly summary with top spending categories
- Budget risk indicator
- Cached dashboard endpoint for fast demo performance

### Stretch Goals
- Recurring transactions
- CSV import
- Smart category suggestions
- Bill reminder system
- Shared household budget view

## 7. Functional Modules

### Frontend Modules
- `features/auth`
- `features/dashboard`
- `features/transactions`
- `features/budgets`
- `features/goals`
- `features/insights`
- `components/ui`

### Backend Modules
- Auth
- Users
- Accounts
- Categories
- Transactions
- Budgets
- Goals
- Insights
- Audit Logging

## 8. High-Level Architecture

### Frontend
- Feature-based React app
- Query layer for API communication
- Zustand for auth/session/UI state
- Shared chart and card components

### Backend
- Controllers -> Services -> Repositories -> EF Core -> PostgreSQL
- Standard response envelope:
  - `{ success, data, message, errors }`
- Middleware for:
  - global exception handling
  - request logging
  - JWT validation

### Data Flow
Frontend -> REST API -> Service Layer -> PostgreSQL / Redis -> Response Envelope

## 9. Proposed Database Entities
- `users`
- `refresh_tokens`
- `accounts`
- `categories`
- `transactions`
- `budgets`
- `budget_items`
- `goals`
- `audit_logs`

### Key Standards
- UUID primary keys
- `created_at`, `updated_at`
- `numeric(12,2)` for money
- indexes on `user_id`, `date`, `category_id`, `budget_id`

## 10. API Plan

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

### Dashboard
- `GET /api/dashboard/summary`
- `GET /api/dashboard/spending-trend`
- `GET /api/dashboard/category-breakdown`

### Transactions
- `GET /api/transactions`
- `POST /api/transactions`
- `PUT /api/transactions/{id}`
- `DELETE /api/transactions/{id}`

### Budgets
- `GET /api/budgets`
- `POST /api/budgets`
- `PUT /api/budgets/{id}`
- `GET /api/budgets/status`

### Goals
- `GET /api/goals`
- `POST /api/goals`
- `PUT /api/goals/{id}`

### Insights
- `GET /api/insights/monthly`
- `GET /api/insights/anomalies`

## 11. Frontend Screens
- Login / Register
- Dashboard
- Transactions page
- Budgets page
- Goals page
- Insights page
- Profile / Settings

## 12. AI Feature Plan
Use AI to generate:
- monthly financial summary
- savings recommendations
- overspending warnings
- transaction trend explanations

### Guardrails
- AI is advisory only
- all financial calculations remain server-side
- no sensitive data logged

## 13. Delivery Plan

### Phase 1 — Foundation
- Initialize frontend and backend projects
- Configure Docker Compose for API, DB, Redis
- Set up linting, formatting, env files, Swagger, Tailwind

### Phase 2 — Core Backend
- Auth flow
- Entity models and migrations
- Transactions, budgets, goals APIs
- Response envelope + error middleware

### Phase 3 — Core Frontend
- Auth screens and protected routing
- Dashboard layout and charts
- CRUD flows for transactions, budgets, goals

### Phase 4 — Wow Features
- AI insights service
- cached dashboard summary
- visual budget health indicators

### Phase 5 — Polish
- seed demo data
- loading/empty/error states
- responsive pass
- accessibility pass
- final demo script

## 14. Suggested Folder Structure
```text
/financeManagementSystemFrontend
  /src
    /components
    /features
    /hooks
    /services
    /store
    /types
    /utils

/financeManagementSystemBackend
  /Controllers
  /Services
  /DTOs
  /Entities
  /Repositories
  /Middleware
  /Infrastructure
```

## 15. Quality Gates
- TypeScript strict mode enabled
- No `any`
- Input validation with Zod and backend DTO validation
- Unit tests for core business logic
- Integration tests for auth + transactions + budgets
- Swagger must be usable before demo
- Docker setup must work from clean clone

## 16. Hackathon Demo Story
1. Sign in as demo user
2. Show dashboard summary and spending trend
3. Add a transaction
4. Watch budget status update
5. Open AI insights panel
6. Show one savings goal moving toward completion
7. End with “what the user should do next this month”

## 17. Risks and Mitigation

### Risk: Scope too large
**Mitigation:** lock MVP first, treat AI and import flows as controlled stretch features

### Risk: Time lost on infra
**Mitigation:** keep Docker Compose simple, no premature Kubernetes work

### Risk: AI feature slows delivery
**Mitigation:** build deterministic insight rules first, then layer AI summary on top

### Risk: Weak demo data
**Mitigation:** prepare realistic seeded data and a scripted walkthrough

## 18. Review Summary
This plan was reviewed before saving against the project rules in `Agent.md`:
- stack selection matches the approved frontend/backend standards
- scope is split into MVP vs differentiators vs stretch goals
- security, testing, Docker, and documentation are included
- the architecture is strong enough for judging, but still realistic for a hackathon timeline
- AI is positioned as a differentiator without blocking core delivery

## 19. Immediate Next Steps
1. scaffold frontend with Vite + React + TypeScript
2. scaffold ASP.NET Core Web API
3. set up PostgreSQL + Redis in Docker Compose
4. implement auth and base project structure
5. build dashboard + transactions first
