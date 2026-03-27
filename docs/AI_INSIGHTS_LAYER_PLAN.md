# AI Insights Layer Plan — FinPilot

## 1. Objective
Add a safe, demo-friendly **AI Insights Layer** on top of the current backend so FinPilot can act as an **AI-powered personal finance coach** for the user.

This layer should:
- help users understand spending behavior
- guide budgeting and savings decisions
- surface anomalies and risks
- stay grounded in deterministic backend calculations
- avoid pretending to be a licensed investment adviser

## 2. Product Positioning
For hackathon and MVP scope, position this as:
- **AI Financial Coach**
- **Personal Finance Insights Assistant**
- **Budget and Savings Guide**

Avoid positioning it as:
- licensed financial adviser
- investment adviser
- portfolio manager
- tax or legal adviser

## 3. Core Principle
The existing backend remains the **system of record**.

### Deterministic backend handles:
- balances
- transaction classification
- budget utilization
- goal progress
- dashboard aggregates
- anomaly features / derived metrics

### AI layer handles:
- explanation
- prioritization
- guidance
- natural-language summaries
- coaching suggestions

## 4. Recommended Architecture
Use a **single orchestrator** with specialized subagents.

```text
User Request
   -> InsightsController
   -> InsightOrchestratorService
      -> Deterministic Summary Builder
      -> Specialized Insight Agents
         - Cashflow Analyst
         - Budget Coach
         - Savings Planner
         - Risk Watcher
      -> Insight Response Composer
   -> API Response
```

## 5. Recommended Agent Roles

### 5.1 Cashflow Analyst Agent
Purpose:
- explain income vs expense patterns
- identify major changes month-over-month
- summarize top spend categories

Inputs:
- dashboard summary
- spending trend
- category breakdown
- recent transactions snapshot

Outputs:
- key observations
- top 3 insights
- monthly narrative

### 5.2 Budget Coach Agent
Purpose:
- highlight overspending risk
- identify threshold breaches
- recommend budget adjustments

Inputs:
- budget health
- current month expenses
- category breakdown

Outputs:
- warning list
- at-risk categories
- practical budget actions

### 5.3 Savings Planner Agent
Purpose:
- explain goal progress
- suggest monthly savings actions
- estimate gap-to-goal

Inputs:
- goal progress
- current net amount
- account balances

Outputs:
- savings recommendations
- target gap explanation
- next milestone guidance

### 5.4 Risk Watcher Agent
Purpose:
- detect suspicious or unusual financial behavior
- surface unexpected spikes, duplicates, subscription creep, scam-like patterns

Inputs:
- recent transactions
- anomaly features from deterministic engine
- merchant frequency summaries

Outputs:
- anomaly alerts
- confidence/importance score
- suggested user review steps

## 6. Safe Skills / Tooling Model
Each agent should not directly access the entire database.

Instead, each agent gets **structured tools** that expose safe summaries.

### Example tool boundaries
- `get_dashboard_summary(userId)`
- `get_spending_trend(userId, months)`
- `get_category_breakdown(userId)`
- `get_budget_health(userId)`
- `get_goal_progress(userId)`
- `get_recent_transactions_snapshot(userId, limit)`
- `get_anomaly_features(userId)`

### Important rule
Agents should:
- receive **sanitized derived summaries**
- produce **recommendations only**
- never write financial records
- never execute actions autonomously

## 7. New Backend Modules to Add

### In `FinPilot.Application`
- `/DTOs/Insights`
- `/Interfaces/Insights`
- `/Services/Insights` (contracts only if keeping implementations in infrastructure)

### In `FinPilot.Infrastructure`
- `/Insights`
  - `InsightOrchestratorService.cs`
  - `InsightPromptFactory.cs`
  - `InsightRuleService.cs`
  - `CashflowInsightAgent.cs`
  - `BudgetCoachAgent.cs`
  - `SavingsPlannerAgent.cs`
  - `RiskWatcherAgent.cs`
  - `InsightSafetyPolicy.cs`
  - `InsightResponseMapper.cs`

### In `FinPilot.Api`
- `/Controllers/InsightsController.cs`

## 8. Suggested DTOs

### InsightRequest
```json
{
  "periodMonths": 6,
  "focus": "budget"
}
```

### InsightCardResponse
```json
{
  "title": "Food spending increased",
  "type": "warning",
  "priority": "high",
  "summary": "Food spending is up 18% vs last month.",
  "recommendations": [
    "Reduce weekly dining-out cap by 10%",
    "Review grocery duplicates"
  ]
}
```

### InsightBundleResponse
```json
{
  "headline": "You are saving well but food and transport are trending high.",
  "cards": [],
  "generatedAt": "2026-03-22T12:00:00Z",
  "disclaimer": "FinPilot provides informational coaching, not investment, tax, or legal advice."
}
```

## 9. Proposed API Endpoints

### Phase 1 — Deterministic + AI summary
- `GET /api/insights/monthly`
- `GET /api/insights/budget-risk`
- `GET /api/insights/anomalies`
- `GET /api/insights/goals`

### Phase 2 — Combined user guidance
- `POST /api/insights/coach`

### Response style
Keep the current standard envelope:
```json
{
  "success": true,
  "data": {},
  "message": "Insights generated successfully",
  "errors": null
}
```

## 10. Integration with Current App
The current backend already has the right base for this.

### Existing modules already available
- auth
- accounts
- categories
- transactions
- budgets
- goals
- dashboard
- Redis cache

### Integration strategy
Do **not** bypass these modules.

Instead:
1. use existing services to build structured summaries
2. feed those summaries to orchestrator/subagents
3. return insight cards and coaching text to frontend

### Example integration path
- `InsightsController` receives request
- `InsightOrchestratorService` calls:
  - `DashboardService`
  - `BudgetService`
  - `GoalService`
  - `TransactionService` snapshot helper
- orchestrator builds `InsightContext`
- orchestrator routes to specialized agents
- responses are merged into `InsightBundleResponse`

## 11. Insight Context Contract
Introduce one internal structured object:

### InsightContext
- user id
- dashboard summary
- trend points
- category breakdown
- budget statuses
- goal progress
- recent transactions snapshot
- anomaly signals
- generation timestamp

This becomes the **single source of truth** for all subagents.

## 12. Deterministic Insight Rules First
Before using LLM output, build a deterministic rules engine.

### Rule examples
- expense up > 20% month-over-month
- category exceeds 80% budget threshold
- subscription/merchant appears more than N times/month
- current goal progress below expected pace
- income drop vs prior month

### Why this matters
- gives grounded signals to AI
- easier to test
- safer in finance domain
- better explainability for judges/demo

## 13. Recommended Output Composition
For each insight endpoint, return:
1. **headline**
2. **structured cards**
3. **recommended next actions**
4. **confidence / severity metadata**
5. **disclaimer**

This gives frontend flexibility and keeps outputs predictable.

## 14. Guardrails and Safety Rules

### Must-have guardrails
- never recommend specific securities or investment products
- never claim guaranteed returns
- never say "you should invest in X"
- never provide tax/legal/compliance advice
- never mutate finances directly
- always disclose that guidance is informational

### Allowed guidance examples
- reduce category spend
- review recurring merchants
- increase monthly savings toward a goal
- delay discretionary purchases when budget risk is high
- review unusual transactions

## 15. Caching Plan
Cache insight bundles per user for a short TTL.

### Suggested cache keys
- `insights:monthly:{userId}`
- `insights:budget-risk:{userId}`
- `insights:anomalies:{userId}`
- `insights:goals:{userId}`

### Invalidate when
- transaction created/updated/deleted
- budget created/updated/deleted
- goal created/updated/deleted

## 16. Suggested Implementation Order

### Step 1
Add deterministic insight contracts and context builder.

### Step 2
Add rule-based endpoints without LLM:
- monthly insights
- budget risk
- anomalies
- goal guidance

### Step 3
Add orchestrator service with pluggable agent interface.

### Step 4
Implement first three specialized agents:
- cashflow analyst
- budget coach
- savings planner

### Step 5
Add risk watcher.

### Step 6
Add Redis caching for insights.

### Step 7
Expose consolidated coaching endpoint.

## 17. Minimal MVP for Hackathon
To stay practical, MVP should include:
- `GET /api/insights/monthly`
- `GET /api/insights/budget-risk`
- `GET /api/insights/anomalies`
- rule-based signals
- optional AI-generated natural-language summary over deterministic signals

This is enough for a strong demo.

## 18. Nice-to-Have After MVP
- conversational coaching endpoint
- per-user personalization memory
- notification suggestions
- monthly report generation
- exportable finance summary
- scam/fraud coaching patterns

## 19. Testing Plan

### Unit tests
- insight rule triggers
- severity scoring
- context builder correctness
- anomaly feature calculation
- orchestrator merge behavior

### Integration tests
- monthly insight endpoint with seeded data
- budget risk endpoint with overspend sample
- anomalies endpoint with duplicate/large transaction sample
- coaching endpoint auth protection

### E2E tests
- create income/expense/budget/goal
- fetch insight endpoints
- confirm coherent outputs and cache invalidation

## 20. Definition of Done
Insights layer is ready when:
- insight endpoints are authenticated and stable
- outputs are grounded in backend summaries
- deterministic rules exist and are tested
- AI guidance is constrained by safety policy
- frontend can render cards without custom parsing
- cache invalidation works after finance mutations

## 21. Review Summary
This plan was reviewed for alignment with the current backend and hackathon constraints.

### Why it fits this app
- integrates cleanly with current service boundaries
- leverages dashboard/budget/goal modules already built
- avoids risky AI autonomy
- keeps financial truth deterministic
- provides a strong demo narrative: **enterprise backend + safe agentic insights**

## 22. Immediate Next Actions
1. create `Insights` DTOs and interfaces
2. build `InsightContextBuilder`
3. implement deterministic monthly/budget/anomaly endpoints
4. add orchestrator service
5. add first 3 specialized agents
6. wire Redis caching and tests
