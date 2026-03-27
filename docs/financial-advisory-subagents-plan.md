# 💰 Financial Advisory Subagents — Implementation Plan

> A comprehensive blueprint for integrating AI-powered financial coaching and advisory subagents into an existing Finance Management System with CRUD capabilities.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Architecture Design](#2-architecture-design)
3. [Agent Definitions](#3-agent-definitions)
4. [Data Flow & Context Scoping](#4-data-flow--context-scoping)
5. [Integration Patterns](#5-integration-patterns)
6. [API Design](#6-api-design)
7. [Implementation Phases](#7-implementation-phases)
8. [Rules & Guardrails](#8-rules--guardrails)
9. [Security & Compliance](#9-security--compliance)
10. [Error Handling & Fallbacks](#10-error-handling--fallbacks)
11. [Cost Management](#11-cost-management)
12. [Testing Strategy](#12-testing-strategy)

---

## 1. System Overview

### What We're Building

An AI subagent layer that sits on top of your existing finance management CRUD system. The subagents consume structured financial data (accounts, transactions, configs) and return personalized, actionable financial guidance.

### Goals

- Provide behavioral financial coaching to users
- Automate budget analysis and recommendations
- Detect anomalies and unusual spending patterns
- Generate human-readable financial summaries and forecasts
- Enable conversational financial Q&A

### Non-Goals (Out of Scope)

- Executing trades or financial transactions autonomously
- Storing raw agent responses as source of truth
- Replacing licensed financial advisors
- Real-time market data without verified integrations

---

## 2. Architecture Design

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  Finance Management App                  │
│           (Existing CRUD + Config Entities)              │
└──────────────────────┬──────────────────────────────────┘
                       │ structured data
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  Agent Orchestrator                      │
│   - Route requests to the right agent                   │
│   - Scope and sanitize context                          │
│   - Aggregate multi-agent responses                     │
│   - Handle retries and fallbacks                        │
└──┬──────────┬──────────┬──────────┬──────────┬──────────┘
   │          │          │          │          │
   ▼          ▼          ▼          ▼          ▼
[Coach]  [Budget]  [Anomaly]  [Invest]  [Report]
Agent    Advisor   Detector   Advisor   Generator
```

### Component Responsibilities

| Component | Responsibility |
|---|---|
| **CRUD Layer** | Manage accounts, transactions, app config — unchanged |
| **Orchestrator** | Route, scope, coordinate, aggregate |
| **Subagents** | Single-purpose AI reasoning units |
| **Response Cache** | Avoid redundant API calls |
| **Guardrail Layer** | Validate, sanitize, and disclaim agent output |

---

## 3. Agent Definitions

### 3.1 Financial Coach Agent

**Purpose:** Analyze behavioral spending patterns and provide empathetic, actionable habit coaching.

**Trigger:** Weekly schedule, dashboard open, or user-initiated chat.

**Input Context:**
```json
{
  "user_goals": ["save $500/month", "pay off credit card"],
  "transactions_90d": [...],
  "category_breakdown": { "food": 800, "entertainment": 400 },
  "income_monthly": 4500,
  "savings_rate": 0.08
}
```

**System Prompt:**
```
You are a compassionate financial coach. Analyze the user's spending behavior 
and financial patterns. Provide 3 specific behavioral observations and 
3 actionable improvement suggestions. Be encouraging, not judgmental. 
Focus on habits and mindset, not just raw numbers.
Always respond in valid JSON only.
```

**Output Schema:**
```json
{
  "health_score": 72,
  "behavioral_patterns": [
    { "pattern": "Weekend splurging", "impact": "high", "description": "..." }
  ],
  "suggestions": [
    { "title": "...", "action": "...", "expected_saving": "$80/month" }
  ],
  "encouragement": "You're doing better than 60% of users in your income bracket."
}
```

---

### 3.2 Budget Advisor Agent

**Purpose:** Compare actual vs planned spending, flag overruns, recommend budget adjustments.

**Trigger:** New transaction added, month-end, or on-demand.

**Input Context:**
```json
{
  "budget_limits": { "food": 600, "entertainment": 200 },
  "actual_spending": { "food": 780, "entertainment": 95 },
  "days_remaining_in_month": 8,
  "upcoming_bills": [{ "name": "Rent", "amount": 1200, "due_in_days": 5 }]
}
```

**System Prompt:**
```
You are a precise budget advisor. Analyze actual vs. planned spending. 
Identify categories over budget, calculate projected month-end totals, 
and suggest specific reallocation or reduction actions.
Always respond in valid JSON only.
```

**Output Schema:**
```json
{
  "status": "over_budget",
  "overrun_categories": [
    { "category": "food", "overrun_amount": 180, "projected_month_end": 920 }
  ],
  "recommendations": [...],
  "safe_to_spend": { "food": 0, "entertainment": 105 }
}
```

---

### 3.3 Anomaly Detection Agent

**Purpose:** Identify unusual, suspicious, or potentially fraudulent transactions.

**Trigger:** Every new transaction (async, non-blocking).

**Input Context:**
```json
{
  "new_transaction": { "amount": 1200, "merchant": "Unknown Store", "category": "shopping" },
  "user_avg_transaction": 85,
  "category_avg": { "shopping": 120 },
  "recent_transactions_7d": [...]
}
```

**System Prompt:**
```
You are a financial anomaly detection specialist. Compare the new transaction 
against the user's historical averages and patterns. Rate anomaly severity 
(none/low/medium/high) and explain why. Never accuse — frame as "unusual activity."
Always respond in valid JSON only.
```

**Output Schema:**
```json
{
  "severity": "high",
  "anomaly_type": "amount_spike",
  "explanation": "This transaction is 14x your average shopping purchase.",
  "recommended_action": "verify",
  "flag_for_review": true
}
```

---

### 3.4 Investment Advisor Agent

**Purpose:** Suggest savings allocation and basic investment positioning based on financial profile.

**Trigger:** On-demand or monthly analysis.

**⚠️ Note:** Always include regulatory disclaimer in output. Not a replacement for licensed advice.

**Input Context:**
```json
{
  "net_worth": 25000,
  "monthly_surplus": 400,
  "existing_investments": { "stocks": 5000, "savings": 10000 },
  "risk_profile": "moderate",
  "age": 32,
  "goals": ["retirement", "house_down_payment"]
}
```

**Output Schema:**
```json
{
  "disclaimer": "This is not licensed financial advice. Consult a certified advisor.",
  "allocation_suggestion": { "emergency_fund": "40%", "index_funds": "40%", "bonds": "20%" },
  "reasoning": "...",
  "priority_actions": [...]
}
```

---

### 3.5 Report Generator Agent

**Purpose:** Produce human-readable monthly/quarterly financial summaries and forecasts.

**Trigger:** Month-end schedule, user request.

**Output:** Markdown-formatted narrative report with key metrics.

---

## 4. Data Flow & Context Scoping

### Golden Rule: Minimum Necessary Context

Never pass all user data to every agent. Scope strictly to what each agent needs.

```
Transaction Created
       │
       ├──► Anomaly Agent   ← last 30 days + new transaction only
       │
       └──► Budget Agent    ← current month actuals + budget limits only

Dashboard Opened
       │
       └──► Coach Agent     ← 90-day transactions + goals + income

Month End (Scheduled)
       │
       ├──► Budget Agent    ← full month summary
       ├──► Coach Agent     ← behavioral monthly review
       └──► Report Agent    ← all of the above aggregated
```

### Data Sanitization Before Agent Calls

```javascript
function scopeContextForAgent(agentType, rawUserData) {
  const scopeMap = {
    coach: ['transactions_90d', 'goals', 'income', 'savings_rate'],
    budget: ['budget_limits', 'actual_spending', 'days_remaining', 'upcoming_bills'],
    anomaly: ['new_transaction', 'user_averages', 'recent_7d'],
    investment: ['net_worth', 'surplus', 'risk_profile', 'goals', 'age'],
  };

  const allowedKeys = scopeMap[agentType];
  return Object.fromEntries(
    Object.entries(rawUserData).filter(([k]) => allowedKeys.includes(k))
  );
}
```

---

## 5. Integration Patterns

### Pattern A: Event-Triggered (Recommended for Anomaly + Budget)

```
User adds transaction (CRUD)
        │
        ▼
  Post-save hook fires
        │
        ▼
  Queue async job → call Anomaly Agent
        │
        ▼
  Result stored in notifications table
        │
        ▼
  User sees alert on next app open
```

**Pros:** Non-blocking, responsive UX, no latency for user.
**Use for:** Anomaly detection, budget breach alerts.

---

### Pattern B: On-Demand Chat (Recommended for Coach + Investment)

```
User types question in chat UI
        │
        ▼
  Orchestrator classifies intent
        │
   ┌────┴────┐
   ▼         ▼
Coach?   Investment?
Agent     Agent
   │         │
   └────┬────┘
        ▼
   Response streamed to UI
```

**Pros:** Interactive, user-controlled, feels conversational.
**Use for:** Financial coach Q&A, investment suggestions.

---

### Pattern C: Scheduled Batch (Recommended for Reports)

```
Cron: 1st of every month
        │
        ▼
  Fetch all users with active accounts
        │
        ▼
  For each user → call Report Agent
        │
        ▼
  Store report → send email/push notification
```

**Pros:** Consistent, predictable, cost-controllable.
**Use for:** Monthly summaries, quarterly reviews.

---

## 6. API Design

### Orchestrator Endpoint

```
POST /api/agents/invoke

Body:
{
  "agent": "coach" | "budget" | "anomaly" | "investment" | "report",
  "trigger": "event" | "on_demand" | "scheduled",
  "user_id": "uuid",
  "extra_context": {}   // optional override
}

Response:
{
  "agent": "coach",
  "result": { ...structured JSON from agent... },
  "cached": false,
  "generated_at": "2026-03-22T10:00:00Z",
  "disclaimer": "..."
}
```

### Chat Endpoint (Conversational)

```
POST /api/agents/chat

Body:
{
  "user_id": "uuid",
  "message": "Am I spending too much on food?",
  "conversation_history": [...]
}

Response:
{
  "reply": "Based on your last 3 months...",
  "agent_used": "coach",
  "follow_up_suggestions": ["Show me my food trends", "Set a food budget"]
}
```

---

## 7. Implementation Phases

### Phase 1 — Foundation (Week 1–2)

- [ ] Set up Orchestrator service (Node.js / Python)
- [ ] Implement context scoping utility (`scopeContextForAgent`)
- [ ] Build Anomaly Detection Agent (highest immediate value)
- [ ] Add post-save transaction hook to trigger anomaly check
- [ ] Store agent results in `agent_results` table
- [ ] Basic notification system for anomaly alerts

**Deliverable:** Every new transaction is automatically screened.

---

### Phase 2 — Budget Intelligence (Week 3–4)

- [ ] Build Budget Advisor Agent
- [ ] Connect to existing budget config entities
- [ ] Trigger on transaction create + month-end cron
- [ ] Build budget alert UI component
- [ ] Implement response caching (Redis or DB-level)

**Deliverable:** Users get real-time budget breach warnings.

---

### Phase 3 — Financial Coach (Week 5–6)

- [ ] Build Financial Coach Agent
- [ ] Design chat UI with conversation history
- [ ] Implement intent classifier (route user questions to right agent)
- [ ] Add coach summary widget to dashboard
- [ ] Weekly coach digest email

**Deliverable:** Users can chat with their financial coach.

---

### Phase 4 — Reports & Investment (Week 7–8)

- [ ] Build Report Generator Agent
- [ ] Set up monthly batch job
- [ ] Build Investment Advisor Agent (with disclaimer layer)
- [ ] PDF/markdown export for reports
- [ ] User risk profile onboarding flow

**Deliverable:** Monthly AI reports + investment suggestions.

---

### Phase 5 — Optimization (Ongoing)

- [ ] Evaluate agent response quality
- [ ] Fine-tune prompts based on user feedback
- [ ] Optimize caching strategy
- [ ] Add token usage monitoring
- [ ] A/B test different coaching tones

---

## 8. Rules & Guardrails

### 🔴 Hard Rules (Never Violate)

```
RULE-001  Agents MUST NOT execute any financial transaction autonomously.
RULE-002  Agents MUST NOT access data outside their defined context scope.
RULE-003  All investment-related output MUST include the regulatory disclaimer.
RULE-004  Agents MUST NOT store PII in prompts or logs.
RULE-005  Agent responses MUST be validated against output schema before display.
RULE-006  No agent response should ever be presented as "guaranteed" or "certain."
RULE-007  Agents MUST NOT identify or name specific third-party financial products
          unless verified (e.g., "consider index funds" not "buy FXAIX").
RULE-008  All agent calls MUST be rate-limited per user (max 20 calls/day).
RULE-009  Agent results older than 24 hours MUST be re-generated or flagged as stale.
RULE-010  Users MUST be able to dismiss, ignore, or turn off any agent suggestion.
```

---

### 🟡 Behavioral Rules (Prompt-Level)

```
RULE-101  The Financial Coach MUST use encouraging, non-judgmental language.
RULE-102  Agents MUST frame anomalies as "unusual activity" — never "fraud" or "theft."
RULE-103  Budget advice MUST acknowledge upcoming bills before suggesting discretionary cuts.
RULE-104  Coach responses MUST acknowledge user goals before giving suggestions.
RULE-105  All suggestions MUST include an estimated impact (e.g., "saves ~$80/month").
RULE-106  Agents MUST NOT compare users to named individuals or specific demographics.
RULE-107  If data is insufficient (< 30 days), agents MUST say so and limit recommendations.
RULE-108  Agents MUST NOT suggest high-risk financial behavior (leveraged trading, crypto speculation).
```

---

### 🟢 Quality Rules (Output Validation)

```
RULE-201  All agent responses MUST be valid, parseable JSON.
RULE-202  Health/risk scores MUST be integers in range 0–100.
RULE-203  Monetary values MUST be returned as numbers, not strings.
RULE-204  Every agent response MUST include a `generated_at` ISO timestamp.
RULE-205  Responses with schema validation errors MUST fall back to a safe default message.
RULE-206  Agent responses MUST be < 2000 tokens to stay within UI rendering limits.
```

---

## 9. Security & Compliance

### Data Privacy

- Strip all PII (names, account numbers, SSN) before sending to agent
- Use internal user IDs only — never emails or real names in prompts
- Log prompt inputs and outputs in encrypted storage for audit
- Implement data retention policy for agent logs (90 days max)

### Prompt Injection Protection

```javascript
function sanitizeUserInput(input) {
  // Remove prompt injection attempts
  const dangerous = ['ignore previous instructions', 'system:', 'assistant:', '<|'];
  for (const pattern of dangerous) {
    if (input.toLowerCase().includes(pattern)) {
      throw new Error('Invalid input detected');
    }
  }
  return input.trim().slice(0, 500); // hard length limit
}
```

### Regulatory Considerations

- Include disclaimer on all financial advice outputs
- Do not store agent-generated investment advice as the basis for decisions
- Log all agent interactions for potential regulatory audit trails
- Consider GDPR/CCPA: users must be able to delete their agent interaction history

---

## 10. Error Handling & Fallbacks

### Failure Modes & Responses

| Failure | Fallback Response |
|---|---|
| Agent API timeout | Show cached result (if < 6 hours old) or generic tip |
| JSON parse error | Log error, show: "We couldn't generate advice right now." |
| Schema validation fail | Discard response, do not show partial data |
| Rate limit exceeded | Queue for next available slot, notify user |
| Insufficient data | "Keep tracking for 30 days to unlock personalized insights." |

### Retry Strategy

```javascript
async function callAgentWithRetry(agentFn, context, maxRetries = 2) {
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      const result = await agentFn(context);
      return validateAgentResponse(result);
    } catch (err) {
      if (attempt === maxRetries) return getFallbackResponse();
      await sleep(1000 * Math.pow(2, attempt)); // exponential backoff
    }
  }
}
```

---

## 11. Cost Management

### Token Budget per Agent (Estimated)

| Agent | Input Tokens (avg) | Output Tokens (avg) | Cost/call (Sonnet) |
|---|---|---|---|
| Coach | ~800 | ~400 | ~$0.005 |
| Budget Advisor | ~400 | ~300 | ~$0.003 |
| Anomaly Detection | ~300 | ~150 | ~$0.002 |
| Investment Advisor | ~500 | ~400 | ~$0.004 |
| Report Generator | ~1200 | ~800 | ~$0.010 |

### Cost Control Strategies

1. **Cache aggressively** — Coach summaries valid for 24h, reports for 30 days
2. **Lazy load agents** — Only invoke when user navigates to relevant section
3. **Batch scheduled jobs** — Run report generation in off-peak hours
4. **Set hard token limits** — `max_tokens: 1000` on all agent calls
5. **Monitor per-user cost** — Alert if single user exceeds $0.50/day

---

## 12. Testing Strategy

### Unit Tests (Per Agent)

- Test with minimal data (< 30 days)
- Test with edge case data (zero income, negative balance)
- Test schema validation on malformed agent responses
- Test fallback behavior on API failure

### Integration Tests

- End-to-end: Transaction created → Anomaly detected → Notification stored
- End-to-end: Month end cron → Report generated → Email sent
- Context scoping: Verify no data leaks between agent types

### Prompt Regression Tests

Maintain a golden dataset of input/output pairs. Run after every prompt change:

```
input: { savings_rate: 0.02, overspend_categories: ["dining", "shopping"] }
expected_output: { health_score: range(30, 50), suggestions.length: >= 2 }
```

### User Acceptance Criteria

- [ ] Agent responses load within 3 seconds (or show loading state)
- [ ] Users can dismiss any suggestion permanently
- [ ] Disclaimer visible on all investment outputs
- [ ] Coach tone rated "helpful" by > 80% of beta users
- [ ] Zero incidents of PII appearing in agent logs

---

## Appendix: Tech Stack Recommendation

| Layer | Recommended |
|---|---|
| Orchestrator | Node.js (Express) or Python (FastAPI) |
| Agent API | Anthropic Claude Sonnet via `/v1/messages` |
| Cache | Redis (TTL-based) |
| Job Queue | BullMQ (Node) or Celery (Python) |
| DB for results | PostgreSQL (`agent_results` table) |
| Monitoring | Datadog or PostHog for agent call metrics |

---

*Last Updated: March 2026 | Version 1.0*
*This document is a living plan — update as agents are built and tested.*
