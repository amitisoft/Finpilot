# Backend Execution Plan — FinPilot

## 1. Objective
Build a **production-shaped backend** for the hackathon first, so the frontend can later plug into stable, testable, documented APIs without rework.

This backend should be:
- secure
- modular
- fast to demo
- realistic to finish in hackathon time
- ready for AI-assisted insights as a second layer

## 2. Backend Technology Decision
Use **ASP.NET Core 8 Web API** with:
- Entity Framework Core
- PostgreSQL
- Redis
- JWT + refresh tokens
- Swagger / OpenAPI
- Docker Compose

### Why this is the right choice
- strong structure for clean layered architecture
- excellent support for auth, middleware, DI, validation, and testing
- fast to scaffold and scale
- easy to present to judges as enterprise-grade backend engineering

## 3. Backend Goals

### Primary Goals
- expose reliable REST APIs for all core finance features
- enforce financial validation server-side
- keep calculations deterministic
- provide fast dashboard aggregation
- support future frontend without API redesign

### Secondary Goals
- add caching for dashboard performance
- support AI-generated insights without risking data integrity
- maintain auditability for money-impacting actions

## 4. Delivery Strategy

### Phase A — Core Platform
- project scaffolding
- environment configuration
- Docker setup
- database connection
- logging
- exception middleware
- response envelope standardization

### Phase B — Identity and Security
- user registration
- login
- JWT access token
- refresh token rotation
- logout / revoke refresh token
- role-ready auth model for future admin support

### Phase C — Core Finance Domain
- accounts
- categories
- transactions
- budgets
- goals

### Phase D — Aggregations and Dashboard
- summary totals
- monthly spending trends
- category breakdown
- budget status
- goal progress
- Redis caching

### Phase E — Insights Layer
- deterministic insight rules
- AI summary orchestration
- specialized subagent-ready service layer

## 5. Recommended Backend Folder Structure
```text
/financeManagementSystemBackend
  /src
    /FinPilot.Api
      /Controllers
      /Middleware
      /Extensions
      /Contracts
    /FinPilot.Application
      /DTOs
      /Interfaces
      /Services
      /Validators
      /Mappings
    /FinPilot.Domain
      /Entities
      /Enums
      /ValueObjects
      /Constants
    /FinPilot.Infrastructure
      /Persistence
      /Repositories
      /Auth
      /Caching
      /External
      /Logging
  /tests
    /FinPilot.UnitTests
    /FinPilot.IntegrationTests
```

## 6. Architecture Pattern

### Layers
- **API Layer**: controllers, auth filters, middleware, Swagger
- **Application Layer**: use-case orchestration, DTOs, validation, business services
- **Domain Layer**: entities, enums, value objects, core rules
- **Infrastructure Layer**: EF Core, repositories, JWT generation, Redis, persistence

### Request Flow
Controller -> Validator -> Service -> Repository/DbContext -> Response Envelope

### Key Rule
All business rules and money calculations stay in the **application/domain layers**, never in controllers.

## 7. Core Domain Model

### Entities

#### User
- `id`
- `full_name`
- `email`
- `password_hash`
- `is_active`
- `created_at`
- `updated_at`

#### RefreshToken
- `id`
- `user_id`
- `token`
- `expires_at`
- `revoked_at`
- `created_at`
- `created_by_ip`

#### Account
- `id`
- `user_id`
- `name`
- `type`
- `currency`
- `opening_balance`
- `current_balance`
- `created_at`
- `updated_at`

#### Category
- `id`
- `user_id` nullable for system defaults
- `name`
- `type` (`income` | `expense`)
- `color`
- `icon`
- `is_default`
- `created_at`
- `updated_at`

#### Transaction
- `id`
- `user_id`
- `account_id`
- `category_id`
- `type` (`income` | `expense`)
- `amount`
- `description`
- `transaction_date`
- `merchant`
- `notes`
- `created_at`
- `updated_at`

#### Budget
- `id`
- `user_id`
- `name`
- `month`
- `year`
- `total_limit`
- `alert_threshold_percent`
- `created_at`
- `updated_at`

#### BudgetItem
- `id`
- `budget_id`
- `category_id`
- `limit_amount`
- `spent_amount`
- `created_at`
- `updated_at`

#### Goal
- `id`
- `user_id`
- `name`
- `target_amount`
- `current_amount`
- `target_date`
- `status`
- `created_at`
- `updated_at`

#### AuditLog
- `id`
- `user_id`
- `entity_name`
- `entity_id`
- `action`
- `old_values`
- `new_values`
- `created_at`

## 8. Database Rules
- use UUID primary keys
- use `numeric(12,2)` for money
- add timestamps to all tables
- add indexes on:
  - `transactions(user_id, transaction_date)`
  - `transactions(category_id)`
  - `transactions(account_id)`
  - `budgets(user_id, month, year)`
  - `goals(user_id)`
  - `refresh_tokens(user_id)`
- seed default categories
- soft delete only if clearly needed later; avoid unnecessary complexity now

## 9. API Design Standards

### Response Envelope
```json
{
  "success": true,
  "data": {},
  "message": "Operation completed",
  "errors": null
}
```

### Error Envelope
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": {
    "field": ["message"]
  }
}
```

## 10. API Modules and Endpoints

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

### Accounts
- `GET /api/accounts`
- `POST /api/accounts`
- `PUT /api/accounts/{id}`
- `DELETE /api/accounts/{id}`

### Categories
- `GET /api/categories`
- `POST /api/categories`
- `PUT /api/categories/{id}`
- `DELETE /api/categories/{id}`

### Transactions
- `GET /api/transactions`
- `GET /api/transactions/{id}`
- `POST /api/transactions`
- `PUT /api/transactions/{id}`
- `DELETE /api/transactions/{id}`

### Budgets
- `GET /api/budgets`
- `GET /api/budgets/{id}`
- `POST /api/budgets`
- `PUT /api/budgets/{id}`
- `DELETE /api/budgets/{id}`
- `GET /api/budgets/status`

### Goals
- `GET /api/goals`
- `GET /api/goals/{id}`
- `POST /api/goals`
- `PUT /api/goals/{id}`
- `DELETE /api/goals/{id}`

### Dashboard
- `GET /api/dashboard/summary`
- `GET /api/dashboard/spending-trend`
- `GET /api/dashboard/category-breakdown`
- `GET /api/dashboard/budget-health`
- `GET /api/dashboard/goal-progress`

### Insights
- `GET /api/insights/monthly`
- `GET /api/insights/anomalies`
- `GET /api/insights/budget-risk`

## 11. Business Rules

### Transactions
- amount must be greater than zero
- transaction type must match allowed category type
- user can only access own transactions
- account balance updates must happen transactionally

### Budgets
- budget must be unique per user per month/year
- category budget item limits cannot exceed total budget unless explicitly allowed
- budget health must be calculated server-side only

### Goals
- current amount cannot exceed unreasonable bounds
- goal progress percentage calculated server-side
- completed goals should have clear status transitions

### Security
- all non-auth endpoints require JWT
- refresh token rotation on every refresh
- login rate limiting
- hashed passwords only
- no sensitive data in logs

## 12. Service Boundaries

### Core Services
- `AuthService`
- `TokenService`
- `AccountService`
- `CategoryService`
- `TransactionService`
- `BudgetService`
- `GoalService`
- `DashboardService`
- `AuditLogService`

### Cross-Cutting Services
- `CurrentUserService`
- `DateTimeProvider`
- `ResponseFactory`
- `CacheService`

### Future Insight Services
- `InsightRuleService`
- `InsightOrchestratorService`
- `AiSummaryService`

## 13. Controlled Subagent Strategy
Using subagents is a **good idea later**, but only behind a safe orchestration layer.

### Recommended Pattern
Use **one orchestrator service** in the backend that gathers deterministic financial summaries, then routes them to specialized insight generators.

### Suggested Specialized Roles
- **Spending Analyst Agent**
  - explains category trends
  - identifies unusual spend spikes

- **Budget Coach Agent**
  - flags overspending risk
  - recommends category adjustments

- **Savings Planner Agent**
  - suggests monthly savings actions
  - comments on goal progress

### Important Rule
Subagents should:
- **read derived summaries**
- **return advice**
- **never directly write financial data**
- **never be the source of truth for calculations**

### Why this is best
- easier to debug
- safer for finance use cases
- simpler to present to judges
- avoids “AI chaos architecture”

## 14. Redis Caching Plan
Use Redis only where it creates clear value:
- dashboard summary
- category spending breakdown
- monthly insight output

### Cache Invalidation
invalidate relevant user dashboard cache when:
- transaction created
- transaction updated
- transaction deleted
- budget changed
- goal changed

## 15. Logging and Monitoring
- structured logging with Serilog
- correlation/request id per request
- health endpoints:
  - `/health/live`
  - `/health/ready`
- log auth failures and money-impacting actions carefully

## 16. Validation Strategy
- FluentValidation for request DTOs
- database constraints as backup safety
- unified validation error formatting

### Validate for
- required fields
- positive money values
- email format
- date ranges
- ownership constraints
- duplicate budget prevention

## 17. Testing Plan

### Unit Tests
- auth token generation
- transaction calculations
- budget utilization logic
- dashboard aggregation logic
- goal progress calculations

### Integration Tests
- register/login/refresh flow
- protected endpoint authorization
- create/update/delete transaction
- create budget and fetch budget status
- dashboard summary with seeded data

### Test Priority
If time is limited, ensure at least:
1. auth flow
2. transaction creation/update
3. budget status calculation
4. dashboard summary

## 18. Implementation Order

### Step 1 — Scaffold
- create solution structure
- configure projects and references
- set up Docker Compose
- configure PostgreSQL and Redis

### Step 2 — Shared Foundations
- response envelope
- exception middleware
- Swagger
- base entity model
- DbContext

### Step 3 — Auth
- user entity
- password hashing
- JWT issuance
- refresh token storage and rotation
- auth endpoints

### Step 4 — Finance Core
- accounts
- categories
- transactions
- account balance sync logic

### Step 5 — Planning Features
- budgets
- budget items
- goals

### Step 6 — Dashboard
- summary queries
- spending trend queries
- category breakdown
- budget health
- goal progress

### Step 7 — Performance and Audit
- Redis cache
- audit logging
- query/index tuning

### Step 8 — Insights Layer
- deterministic rules first
- AI orchestration second
- specialized agent roles last

## 19. Definition of Done for Backend
Backend is ready for frontend integration when:
- auth is complete and tested
- all core CRUD APIs work
- dashboard endpoints are stable
- Swagger is complete
- Docker setup works from clean start
- migrations run successfully
- basic integration tests pass
- seeded demo data is available

## 20. Review Summary
This backend plan was reviewed before saving against `Agent.md` and current hackathon constraints:
- backend-first scope is correct given frontend is deferred
- architecture is concrete, not vague
- AI/subagents are included safely as a later layer
- the plan prioritizes deterministic finance logic over AI dependence
- implementation order reduces rework and demo risk

## 21. Immediate Next Backend Actions
1. scaffold ASP.NET Core solution and projects
2. add PostgreSQL + Redis with Docker Compose
3. configure DbContext and base entity infrastructure
4. implement auth module first
5. implement transactions and categories next
6. then add budgets, goals, and dashboard endpoints
