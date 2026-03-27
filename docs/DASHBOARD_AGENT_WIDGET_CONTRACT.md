# Dashboard Agent Widget API Contract

## Purpose
Frontend-friendly dashboard endpoints for the two summary widgets:
- coach widget
- report widget

These endpoints return already-shaped data so the dashboard does not need to parse full agent payloads.

---

## Authentication
All endpoints require:
- `Authorization: Bearer <access_token>`

Base URL examples assume local API:
- `http://localhost:5000`

---

## 1. Coach Widget

### Endpoint
`GET /api/agents/widgets/coach`

### Behavior
- uses the existing **coach agent**
- returns cached coach output when valid
- otherwise generates a fresh coach snapshot

### Response shape
```json
{
  "success": true,
  "data": {
    "healthScore": 78,
    "headline": "Coach focus: Cap Food",
    "encouragement": "You're on a workable path; tightening one or two categories can noticeably improve your monthly cushion.",
    "topPatterns": [
      "Food is taking 43.75% of this month's expense spend.",
      "1 budget area is already over plan or close to the warning threshold.",
      "Emergency Fund is currently at 25% progress."
    ],
    "primaryAction": "Set a weekly cap for Food and review purchases before the weekend or other trigger periods.",
    "estimatedMonthlyImpact": 1680,
    "disclaimer": "FinPilot provides informational guidance only and does not execute financial actions.",
    "generatedAt": "2026-03-22T18:00:00Z"
  },
  "message": "Coach widget fetched successfully",
  "errors": null
}
```

### Frontend usage
Use for:
- dashboard hero card
- coach health ring / score badge
- quick CTA button

Recommended UI mapping:
- `healthScore` → score chip / progress ring
- `headline` → main card title
- `encouragement` → supporting copy
- `topPatterns[0..2]` → bullets
- `primaryAction` → primary CTA text
- `estimatedMonthlyImpact` → savings badge

---

## 2. Report Widget

### Endpoint
`GET /api/agents/widgets/report`

### Behavior
- uses the existing **report generator agent**
- returns cached monthly-style summary when valid
- otherwise generates a fresh report snapshot

### Response shape
```json
{
  "success": true,
  "data": {
    "title": "FinPilot Report - March 2026",
    "summary": "You finished the current month with a positive net cashflow of 27000.",
    "highlights": [
      "Income this month: 30000",
      "Expenses this month: 3000",
      "Net cashflow: 27000",
      "Top expense category: Food at 3000"
    ],
    "forecast": "At the recent pace, next month's net cashflow could be about 27000.",
    "disclaimer": "FinPilot provides informational guidance only and does not execute financial actions.",
    "generatedAt": "2026-03-22T18:00:00Z"
  },
  "message": "Report widget fetched successfully",
  "errors": null
}
```

### Frontend usage
Use for:
- monthly summary card
- report preview section
- expandable "view full report" entry point

Recommended UI mapping:
- `title` → card header
- `summary` → one-line executive summary
- `highlights[0..3]` → quick bullets
- `forecast` → future-looking footer text

---

## Error behavior
Standard API envelope is preserved:
```json
{
  "success": false,
  "data": null,
  "message": "...",
  "errors": ["..."]
}
```

Expected failure cases:
- expired / missing JWT → `401`
- unexpected server error → `500`

---

## Recommended dashboard load order
1. `GET /api/dashboard/summary`
2. `GET /api/agents/widgets/coach`
3. `GET /api/agents/widgets/report`
4. optionally load deeper panels after first paint:
   - `/api/dashboard/spending-trend`
   - `/api/insights/monthly`
   - `/api/agents/results?agent=1`

---

## Notes
- These widget endpoints are intentionally **summary-first**.
- For full raw agent payloads, continue using:
  - `POST /api/agents/invoke`
  - `GET /api/agents/results`
- Investment guidance remains available through invoke/chat flows and should stay behind disclaimer-heavy UX.
