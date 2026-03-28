# FinPilot Development History

This document tells the product story of FinPilot from initial concept to hackathon-ready application.

## 1. Starting point: a finance app that could win a hackathon
The original idea was not just to build another CRUD finance tracker. The goal was to ship a product that could show judges:
- practical day-to-day value
- strong backend engineering
- a polished interface
- an AI layer that felt useful instead of gimmicky

That led to a product direction centered on **clarity, control, and guided action**.

## 2. Phase one: establish the core system of record
The first major milestone was a stable backend foundation.

### What was added
- ASP.NET Core 8 API with layered architecture
- PostgreSQL persistence with EF Core migrations
- consistent response envelope
- global exception handling
- JWT auth with refresh tokens
- initial health and Swagger setup

### Why it mattered
Every later feature - analytics, coaching, dashboards, and deployment - depends on trustworthy source-of-truth data.

## 3. Phase two: ship the money-management backbone
Once auth and persistence were stable, the app expanded into the actual finance domain.

### Features added
- accounts
- categories
- transactions
- budgets
- goals
- seed data for default categories
- audit logging for meaningful mutations

### Outcome
The app could now support a realistic money journey:
- create an account
- categorize money movement
- set a monthly plan
- track progress against a goal

## 4. Phase three: turn data into visibility
The next step was making the app explain the user's finances instead of just storing them.

### Features added
- dashboard summary
- spending trends
- category breakdown
- budget health
- goal progress
- monthly insights
- budget risk insights
- anomaly insights
- goal insights

### Product shift
This was the moment FinPilot stopped feeling like a ledger and started feeling like a decision-support product.

## 5. Phase four: add the coaching layer
The most important differentiator was the coaching experience.

### Features added
- financial coach agent
- anomaly agent
- budget advisor agent
- investment guidance agent
- report generator agent
- agent widgets for dashboard surfaces
- agent results persistence and dismiss flows

### Product positioning
The app deliberately stayed in the lane of:
- financial coaching
- spending guidance
- budgeting support
- explanation and prioritization

It explicitly avoided pretending to be:
- a licensed adviser
- a tax platform
- an autonomous financial actor

## 6. Phase five: frontend productization
The backend already had depth by this point, but the demo still needed to look like a product.

### Frontend work added
- premium dashboard shell
- auth flow
- accounts, ledger, budgets, goals, categories, insights, coach, and activity pages
- improved chat-style AI coach workspace
- better numeric form UX
- clearer budgeting validation
- stronger visual hierarchy and lighter professional theme

### UX lessons applied during development
- keep the dashboard concise for new users
- make only the content area scroll, not the full shell
- keep assistant pages conversation-first
- surface validation rules before the backend rejects a request

## 7. Phase six: deployment simplification
As deployment planning matured, the docs and app were simplified for hackathon reality.

### What changed
- Redis was removed as an active deployment dependency
- dashboard and insight caching moved to in-memory distributed cache
- deployment path was simplified to app + PostgreSQL
- GitHub Actions were prepared for Azure App Service deployment

### Why this improved the story
A hackathon app needs to be credible, but it should also be deployable quickly. Removing unnecessary infrastructure made the deployment story cleaner and easier to explain.

## 8. Current implemented feature set
### Authentication
- register
- login
- refresh token
- logout
- current user endpoint

### Core finance
- accounts
- categories
- transactions
- budgets
- goals

### Analytics and insights
- dashboard summary
- trend and breakdown visual data
- budget health
- goal progress
- monthly insights
- budget risk
- anomaly detection
- goal insights

### AI-style guidance
- coach chat
- anomaly review
- budget advisory
- investment guidance
- report generation
- dashboard widgets
- persisted agent results

### Operational features
- audit logs
- health endpoints
- Swagger in non-production / configurable mode
- GitHub Actions validation and deploy workflow

## 9. What was intentionally cut or simplified
To keep the app demoable and stable, some ideas were intentionally limited.

### Intentionally not used right now
- external paid LLM dependencies
- Redis as a required infrastructure component
- cron jobs / weekly email digest
- production-scale multi-instance infrastructure

### Why
The goal was to keep the story honest: the app should feel production-shaped, but it should still be realistic for a hackathon team to ship and operate.

## 10. The story the app now tells
FinPilot now tells a full user story:
1. **Capture reality** - record accounts and money movement.
2. **Create a plan** - budgets and goals.
3. **Understand performance** - dashboard and insights.
4. **Get guided next steps** - coach and advisory outputs.
5. **Keep trust** - audit trail and deterministic calculations.

That progression is what makes the app feel cohesive instead of feature-stacked.

## 11. Recommended demo narrative
For the strongest demo, present the app in this order:
1. auth and onboarding
2. account and transaction setup
3. budget and goal creation
4. dashboard + insights
5. AI coach and advisory widgets
6. audit/history view to close the loop

## 12. Source-of-truth note
For current operational guidance, use these docs:
- [../README.md](../README.md)
- [README.md](README.md)
- [AZURE_DEPLOYMENT_PLAN.md](AZURE_DEPLOYMENT_PLAN.md)
- [FIGMA_FEATURE_LIST.md](FIGMA_FEATURE_LIST.md)

The archived plan docs remain useful for context, but this file is the cleanest story of how the application evolved.
