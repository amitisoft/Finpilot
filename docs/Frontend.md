FinPilot AI Agent Handoff Specification

Project Overview

FinPilot is a premium, high-fidelity personal finance management (PFM) application with integrated AI advisory features. It uses a "Bento Box" design system, glassmorphism effects, and a highly structured layout for financial data visualization.

Technical Stack

Framework: React

Styling: Tailwind CSS (Modern/JIT)

Icons: Lucide-React

Animations: Tailwind animate-in utilities

Detailed Screen-by-Screen Breakdown

1. Dashboard (Command Center)

Architecture: 12-column Responsive Bento Grid.

Header: Greeting + Dynamic status (Optimal) + AI/Manage buttons.

Liquidity Card (4/12 col): Large typography for balance, TrendingUp indicator, and a stack of account icons.

AI Coach Card (5/12 col): Dark mode variant (bg-slate-900) with glassmorphism overlays and mesh gradient background. Displays health score and high-impact suggestions.

Mini Stats (3/12 col): Quick-glance cards for Income/Expense.

Goals Row (6/12 col): Progress-bar centric layout for milestones.

Budget Alert (6/12 col): Semantic coloring (rose-50) for critical thresholds.

2. Capital Accounts

Architecture: Responsive 3-column Card Grid.

Account Cards: High-radius (2rem) containers.

UI Logic: Conditional icon rendering based on account type (CreditCard for Banks, Banknote for Cash, Zap for Investments).

Interactions: "Details" and "Transfer" buttons use uppercase, high-tracking typography for a premium look.

3. Unified Ledger (Transactions)

Architecture: Data Table within a Bento Container.

Columns: Status, Entity, Category, Account, Magnitude.

Anomaly Detection: Flagged transactions use a bg-rose-50 pulse animation and an AlertTriangle icon.

Visuals: Categorical chips and merchant iconography in a bg-slate-100 square.

Filters: Search bar with inner-left icon + Secondary filter button.

4. Budget Allocation

Architecture: 2-column Detailed Comparison View.

Risk Indicators: Dynamic styling for "low", "high", and "critical" risks.

AI Advice Bar: Embedded bg-slate-50 card within the budget item with Sparkles icon for proactive tips.

Progress Logic: Math.min((spent/limit)*100, 100) to ensure progress bars never overflow container.

5. Strategic Goals

Architecture: Progress-first Card Grid.

UI Element: 20% opacity ghost icons in the top-right corner of cards.

Tracking: Circular progress logic or full-width rounded bars.

Forecasting: Displays "Target Date" as the primary subtitle.

6. AI Coach (Chat)

Architecture: Fixed-height Flex Container with Scrolling History.

History: Alternating bubbles (Left: AI, Right: User). AI bubbles feature an "Advisor" header.

Prompt Chips: A horizontally scrolling list of uppercase tracking-widest buttons to trigger backend intents.

Input: Ultra-rounded (1.5rem) text field with a prominent "Up-Right" action button.

7. Deep Intelligence (Insights)

Architecture: 2-column Insight Feed.

Actionability: Every insight includes a "Primary Action" button and a "Dismiss" button.

Classification: Categorized by type: Optimizer, Anomaly, Investment, or Risk.

Data Structures (MOCK_DATA)

The application relies on a unified MOCK_DATA object:

accounts: ID, Name, Type, Balance, Icon, Color.

transactions: ID, Type (income/expense), Amount, Category, Merchant, Date, Anomaly (bool), Icon.

budgets: ID, Name, Spent, Limit, Category, Risk (low/high/critical).

goals: ID, Name, Target, Current, Date, Icon.

Design Tokens (THEME)

Glassmorphism: bg-white/70 backdrop-blur-xl border-white/20 shadow-[0_8px_32px_0_rgba(31,38,135,0.07)]

AI Accents: Gradient from-[#6366F1] via-[#A855F7] to-[#EC4899]

Typography: font-black (900) for headings, tracking-tighter for currency, tracking-widest for status/metadata.

Instructions for AI Agent

Component Mapping: When generating new screens, wrap content in animate-in fade-in duration-500.

Visual Hierarchy: Financial figures MUST use font-black. Subtitles MUST use font-black uppercase tracking-widest text-slate-400.

AI Integration: The CoachScreen is the priority for LLM integration. Use the prompt chips as preset system-instruction triggers.