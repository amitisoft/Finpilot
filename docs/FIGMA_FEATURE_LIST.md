# FinPilot Figma Feature List

> Designer-facing feature inventory for the current app scope.
> Purpose: help create complete Figma screens for the features that already exist in the backend.

## 1. Product Summary
FinPilot is a personal finance management app with AI-powered coaching and advisory-style insights.

Current implemented modules:
- authentication
- accounts
- categories
- transactions
- budgets
- goals
- dashboard analytics
- AI insights
- AI agent chat / widgets / results
- audit logs

---

## 2. Core User Flows to Design

### 2.1 Authentication
- Register
- Login
- Logout
- Refresh session silently
- View current user profile (`me`)

### 2.2 Money Management
- View accounts
- Create account
- Edit account
- Delete unused account
- View categories
- Create custom category
- Edit custom category
- Delete unused custom category
- View transactions
- Add income transaction
- Add expense transaction
- Edit transaction
- Delete transaction

### 2.3 Planning
- View budgets
- Create monthly budget
- Update monthly budget
- Delete budget
- View budget status / health
- View goals
- Create goal
- Update goal
- Delete goal

### 2.4 Analytics
- Dashboard summary
- Spending trend
- Category breakdown
- Budget health
- Goal progress

### 2.5 AI Layer
- Monthly insights
- Budget-risk insights
- Anomaly insights
- Goal insights
- AI coach widget
- AI report widget
- AI chat
- Manual agent invocation
- Agent results list
- Dismiss agent suggestion

### 2.6 History / Activity
- Audit log list

---

## 3. Suggested Screens for Figma

## 3.1 Auth Screens
### A. Register
Fields:
- full name
- email
- password

States:
- default
- loading
- validation error
- success

### B. Login
Fields:
- email
- password

States:
- default
- loading
- invalid credentials
- success

---

## 3.2 Main App Shell
### C. App Layout / Navigation
Recommended navigation items:
- Dashboard
- Accounts
- Transactions
- Budgets
- Goals
- Insights
- AI Coach
- Activity Log
- Profile / Logout

Include:
- page header
- global add button
- quick stats cards
- empty states
- loading skeletons
- error banners

---

## 3.3 Dashboard
### D. Dashboard Home
Show these widgets/cards:
- Total Income
- Total Expenses
- Net Amount
- Total Balance
- Transaction Count

Charts/sections:
- spending trend chart
- category breakdown chart
- budget health section
- goal progress section
- AI coach widget
- AI report widget

Quick actions:
- add income
- add expense
- create budget
- create goal
- ask AI coach

States:
- first-time empty state
- populated state
- loading state

---

## 3.4 Accounts
### E. Accounts List
Each account item can show:
- account name
- type
- currency
- opening balance
- current balance

Actions:
- create
- edit
- delete
- view details

### F. Create / Edit Account Modal or Page
Fields:
- name
- type
- currency
- opening balance

Account types currently supported:
- Cash
- Bank
- Credit Card
- Wallet
- Investment

---

## 3.5 Categories
### G. Categories List
Sections:
- default categories
- custom categories
- income categories
- expense categories

Each item can show:
- category name
- type
- color
- icon
- default/custom badge

Actions:
- create custom category
- edit custom category
- delete custom category

### H. Create / Edit Category
Fields:
- name
- type
- color
- icon

---

## 3.6 Transactions
### I. Transactions List
Each transaction item can show:
- type (income/expense)
- amount
- description
- account
- category
- merchant
- notes
- transaction date

Filters/search to design:
- all / income / expense
- by account
- by category
- by date

Actions:
- add transaction
- edit transaction
- delete transaction
- inspect anomaly status

### J. Add / Edit Transaction
Fields:
- account
- category
- type
- amount
- description
- date
- merchant
- notes

Important UX note:
- category type must match transaction type

---

## 3.7 Budgets
### K. Budgets List
Each budget card can show:
- budget name
- month / year
- total limit
- total spent
- remaining amount
- usage percent
- threshold reached / over-budget state

Actions:
- create budget
- edit budget
- delete budget
- view details

### L. Budget Detail
Show:
- top-level budget stats
- category budget items
- per-category limit
- spent amount
- remaining amount
- usage percent
- AI budget advice summary

### M. Create / Edit Budget
Fields:
- name
- month
- year
- total limit
- alert threshold percent
- category line items
  - category
  - limit amount

Validation-aware states:
- duplicate month/year not allowed
- only expense categories allowed
- item totals cannot exceed total budget

---

## 3.8 Goals
### N. Goals List
Each goal card can show:
- goal name
- target amount
- current amount
- progress percent
- target date
- status

Statuses:
- Active
- Completed
- Archived

Actions:
- create
- edit
- delete
- view progress

### O. Create / Edit Goal
Fields:
- name
- target amount
- current amount
- target date
- status

---

## 3.9 Insights
### P. Insights Overview
Tabs or sections:
- Monthly
- Budget Risk
- Anomalies
- Goals

Each insight card can contain:
- title
- severity / priority
- summary
- recommendation(s)
- generated time
- disclaimer

### Q. Monthly Insights Screen
Should show:
- overall headline
- trend-based observations
- spending behavior changes
- top recommendations

### R. Budget Risk Screen
Should show:
- at-risk budgets
- threshold warnings
- safe-to-spend style guidance
- recommendation cards

### S. Anomaly Insights Screen
Should show:
- unusual transactions
- anomaly severity
- explanation
- recommended action

### T. Goal Insights Screen
Should show:
- goal progress summary
- behind-schedule / on-track signals
- suggestions to improve saving pace

---

## 3.10 AI Coach / Agents
### U. AI Chat Screen
Supported intents already exist for:
- coach
- budget
- anomaly
- investment guidance
- report summary

Chat UI should support:
- message input
- quick suggestion chips
- response card with disclaimer
- source/agent label
- follow-up prompt ideas

Suggested quick prompts:
- How can I reduce food spending?
- Am I over budget this month?
- Does this transaction look suspicious?
- How should I invest my monthly surplus?
- Give me my monthly report summary.

### V. Coach Widget
Widget fields available:
- health score
- headline
- encouragement
- top patterns
- primary action
- estimated monthly impact
- disclaimer
- generated at

### W. Report Widget
Widget fields available:
- title
- summary
- highlights
- forecast
- disclaimer
- generated at

### X. Agent Results / Alerts Screen
Show stored AI results such as:
- anomaly results
- budget advisor results
- coach results
- investment guidance results
- report results

Each result can show:
- agent type
- summary
- severity/status
- source entity
- generated time
- dismissed/active state

Actions:
- view result details
- dismiss result

### Y. Manual Agent Invocation Panel (optional advanced screen)
Useful for demo/admin/testing flows.
Can include actions like:
- invoke anomaly agent for a transaction
- invoke budget advisor for a budget
- invoke coach
- invoke investment advisor
- invoke report generator

---

## 3.11 Activity / Audit Log
### Z. Audit Log Screen
Show a chronological list of important user actions.

Each row can show:
- entity name
- action type
- entity id or human-friendly label
- created at
- old values/new values preview

Likely useful filters:
- all
- auth
- account
- category
- transaction
- budget
- goal

---

## 4. Important UX States to Include in Figma
Design these for every major module:
- empty state
- loading state
- success toast/snackbar
- validation error state
- destructive confirmation modal
- API/server error state
- no-results search/filter state

Special states:
- over-budget
- threshold reached
- completed goal
- suspicious transaction alert
- dismissed AI suggestion
- no enough data yet / start tracking to unlock insights

---

## 5. Shared Components to Design
- app shell / sidebar / top nav
- metric cards
- chart cards
- section headers
- filter bar
- search input
- modal / drawer forms
- confirmation dialog
- toast/snackbar
- status badges
- chips/tags
- AI insight card
- AI result card
- budget progress bar
- goal progress bar
- anomaly severity badge
- disclaimer banner

---

## 6. Backend-Supported Constraints the Design Should Respect
- authentication required for most app screens
- budgets are monthly
- one budget per month/year per user
- budget items only use expense categories
- transaction type must match category type
- accounts with transactions cannot be deleted
- categories in use cannot be deleted
- AI suggestions can be dismissed
- audit logs are read-only history
- rate limiting exists on auth and AI-heavy endpoints

---

## 7. Recommended MVP Figma Priority Order
Design in this order:
1. Auth
2. Main app shell
3. Dashboard
4. Accounts
5. Transactions
6. Budgets
7. Goals
8. Insights
9. AI Chat + widgets
10. Audit log

---

## 8. Not Required Right Now
Do not design these as committed features yet:
- cron-based scheduled jobs
- weekly email digest
- production admin console
- complex multi-user roles/permissions
- autonomous financial actions

---

## 9. Designer Handoff Note
If time is limited, the strongest hackathon demo flow is:
- register/login
- dashboard overview
- add income + expense
- create budget
- create goal
- view AI insights
- open coach/report widgets
- ask AI coach a question
- inspect anomaly/budget alerts
