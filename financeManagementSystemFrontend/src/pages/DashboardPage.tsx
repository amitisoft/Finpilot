import { useEffect, useMemo, useState } from 'react';
import { ArrowRight, Bot, PiggyBank, ReceiptText, Sparkles, Target, TrendingDown, TrendingUp, WalletCards } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatCompactCurrency, formatCurrency } from '../lib/utils';
import type { BudgetStatusResponse, CategoryBreakdownResponse, DashboardCoachWidgetResponse, DashboardReportWidgetResponse, DashboardSummaryResponse, GoalProgressResponse, SpendingTrendPointResponse } from '../types/api';

interface DashboardState {
  summary: DashboardSummaryResponse | null;
  trend: SpendingTrendPointResponse[];
  breakdown: CategoryBreakdownResponse[];
  budgetHealth: BudgetStatusResponse[];
  goalProgress: GoalProgressResponse[];
  coachWidget: DashboardCoachWidgetResponse | null;
  reportWidget: DashboardReportWidgetResponse | null;
}

const quickActions = [
  {
    to: '/accounts',
    label: 'Accounts',
    description: 'Add your primary cash or bank account so balances are real.',
    icon: WalletCards
  },
  {
    to: '/transactions',
    label: 'Transactions',
    description: 'Record income and expenses to unlock live coaching.',
    icon: ReceiptText
  },
  {
    to: '/budgets',
    label: 'Budgets',
    description: 'Set a monthly guardrail before spending drifts.',
    icon: PiggyBank
  },
  {
    to: '/goals',
    label: 'Goals',
    description: 'Track savings milestones with a clear target date.',
    icon: Target
  }
];

function progressWidth(value: number) {
  return `${Math.max(0, Math.min(value, 100))}%`;
}

export function DashboardPage() {
  const { api } = useAuth();
  const [state, setState] = useState<DashboardState>({
    summary: null,
    trend: [],
    breakdown: [],
    budgetHealth: [],
    goalProgress: [],
    coachWidget: null,
    reportWidget: null
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const run = async () => {
      setLoading(true);
      setError(null);
      try {
        const [summary, trend, breakdown, budgetHealth, goalProgress, coachWidget, reportWidget] = await Promise.all([
          api.get<DashboardSummaryResponse>('/api/dashboard/summary'),
          api.get<SpendingTrendPointResponse[]>('/api/dashboard/spending-trend?months=6'),
          api.get<CategoryBreakdownResponse[]>('/api/dashboard/category-breakdown'),
          api.get<BudgetStatusResponse[]>('/api/dashboard/budget-health'),
          api.get<GoalProgressResponse[]>('/api/dashboard/goal-progress'),
          api.get<DashboardCoachWidgetResponse>('/api/agents/widgets/coach'),
          api.get<DashboardReportWidgetResponse>('/api/agents/widgets/report')
        ]);

        setState({ summary, trend, breakdown, budgetHealth, goalProgress, coachWidget, reportWidget });
      } catch (dashboardError) {
        setError(flattenErrors(dashboardError));
      } finally {
        setLoading(false);
      }
    };

    void run();
  }, [api]);

  const topBudgets = useMemo(() => state.budgetHealth.slice(0, 2), [state.budgetHealth]);
  const topGoals = useMemo(() => state.goalProgress.slice(0, 2), [state.goalProgress]);
  const topSpending = useMemo(() => state.breakdown.slice(0, 3), [state.breakdown]);

  if (loading) return <LoadingPanel label="Loading command center…" />;
  if (error) return <GlassCard className="border-rose-200 bg-rose-50 text-rose-700">{error}</GlassCard>;
  if (!state.summary) return <GlassCard>No dashboard data available.</GlassCard>;

  const healthScore = state.coachWidget?.healthScore ?? 0;
  const hasTrackedActivity =
    state.summary.transactionCount > 0 ||
    state.summary.totalIncome > 0 ||
    state.summary.totalExpenses > 0 ||
    state.summary.totalBalance > 0 ||
    state.budgetHealth.length > 0 ||
    state.goalProgress.length > 0;

  const healthTone = !hasTrackedActivity ? 'amber' : healthScore >= 70 ? 'emerald' : healthScore >= 45 ? 'amber' : 'rose';
  const statusLabel = !hasTrackedActivity ? 'Setup mode' : healthScore >= 70 ? 'Strong footing' : healthScore >= 45 ? 'Needs attention' : 'Action required';

  const setupSteps = [
    {
      title: 'Connect an account',
      complete: state.summary.totalBalance > 0 || state.summary.transactionCount > 0,
      to: '/accounts'
    },
    {
      title: 'Capture your first transactions',
      complete: state.summary.transactionCount > 0,
      to: '/transactions'
    },
    {
      title: 'Set at least one budget or goal',
      complete: state.budgetHealth.length > 0 || state.goalProgress.length > 0,
      to: state.budgetHealth.length > 0 ? '/goals' : '/budgets'
    }
  ];

  const setupCompleted = setupSteps.filter((step) => step.complete).length;

  return (
    <div className="space-y-6">
      <SectionTitle
        eyebrow="Dashboard"
        title="Financial Command Center"
        action={<Badge tone={healthTone}>{statusLabel}</Badge>}
      />

      <div className="grid gap-6 xl:grid-cols-12">
        <GlassCard className="xl:col-span-7">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Overview</p>
              <p className="mt-3 text-4xl font-black tracking-tight text-slate-900 md:text-5xl">{formatCompactCurrency(state.summary.totalBalance)}</p>
              <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
                {hasTrackedActivity
                  ? 'This page now focuses on the essentials: your current position, the next best action, and whether budgets or goals need attention.'
                  : 'Start with one account and your first few transactions. FinPilot will keep this page compact until real data starts flowing.'}
              </p>
            </div>
            <Link
              to={hasTrackedActivity ? '/transactions' : '/accounts'}
              className="inline-flex items-center gap-2 rounded-2xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-800"
            >
              {hasTrackedActivity ? 'Open ledger' : 'Add first account'}
              <ArrowRight className="h-4 w-4" />
            </Link>
          </div>

          <div className="mt-6 grid gap-3 md:grid-cols-3">
            <div className="rounded-3xl border border-emerald-100 bg-emerald-50 p-4">
              <div className="flex items-center gap-2 text-emerald-700">
                <TrendingUp className="h-4 w-4" />
                <p className="text-xs font-black uppercase tracking-[0.35em]">Income</p>
              </div>
              <p className="mt-4 text-3xl font-black tracking-tight text-emerald-700">{formatCurrency(state.summary.totalIncome)}</p>
            </div>
            <div className="rounded-3xl border border-rose-100 bg-rose-50 p-4">
              <div className="flex items-center gap-2 text-rose-700">
                <TrendingDown className="h-4 w-4" />
                <p className="text-xs font-black uppercase tracking-[0.35em]">Expenses</p>
              </div>
              <p className="mt-4 text-3xl font-black tracking-tight text-rose-700">{formatCurrency(state.summary.totalExpenses)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Net this month</p>
              <p className="mt-4 text-3xl font-black tracking-tight text-slate-900">{formatCurrency(state.summary.netAmount)}</p>
              <p className="mt-2 text-sm text-slate-500">{state.summary.transactionCount} transactions tracked</p>
            </div>
          </div>
        </GlassCard>

        <GlassCard className="overflow-hidden xl:col-span-5">
          <div className="rounded-[1.75rem] bg-gradient-to-br from-slate-900 via-slate-800 to-violet-900 p-6 text-white">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-black uppercase tracking-[0.35em] text-white/65">AI Coach</p>
                <h3 className="mt-3 text-3xl font-black tracking-tight">
                  {state.coachWidget?.headline ?? 'Coach snapshot unavailable'}
                </h3>
              </div>
              <div className="rounded-3xl bg-white/10 px-4 py-3 text-right">
                <p className="text-xs font-black uppercase tracking-[0.35em] text-white/60">Health score</p>
                <p className="text-3xl font-black text-white">{healthScore}</p>
              </div>
            </div>

            <p className="mt-4 text-sm leading-6 text-white/75">
              {state.coachWidget?.encouragement ?? 'Once the app sees a few transactions, budgets, and goals, this card will turn into a focused financial coach.'}
            </p>

            <div className="mt-5 space-y-3">
              {(state.coachWidget?.topPatterns ?? []).slice(0, 2).map((pattern) => (
                <div key={pattern} className="rounded-3xl border border-white/10 bg-white/10 p-4 text-sm text-white/85">
                  {pattern}
                </div>
              ))}
            </div>

            <div className="mt-5 flex items-center justify-between gap-4 rounded-3xl border border-white/10 bg-white/10 p-4">
              <div>
                <p className="text-xs font-black uppercase tracking-[0.35em] text-white/60">Primary action</p>
                <p className="mt-1 text-base font-semibold text-white/90">{state.coachWidget?.primaryAction ?? 'Add a few transactions to unlock smarter coaching.'}</p>
              </div>
              <Link
                to="/coach"
                className="inline-flex items-center gap-2 rounded-2xl bg-white px-4 py-3 text-sm font-semibold text-slate-900 transition hover:bg-slate-100"
              >
                Open coach
                <Bot className="h-4 w-4" />
              </Link>
            </div>
          </div>
        </GlassCard>
      </div>

      <div className="grid gap-6 xl:grid-cols-12">
        <GlassCard className="xl:col-span-4">
          <SectionTitle eyebrow="Action center" title="Keep the setup simple" />

          <div className="space-y-3">
            {quickActions.map(({ to, label, description, icon: Icon }) => (
              <Link
                key={to}
                to={to}
                className="flex items-start gap-3 rounded-3xl border border-slate-200 bg-slate-50 p-4 transition hover:border-slate-300 hover:bg-white"
              >
                <div className="rounded-2xl bg-white p-3 text-slate-700 shadow-sm">
                  <Icon className="h-5 w-5" />
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-black uppercase tracking-[0.3em] text-slate-500">{label}</p>
                  <p className="mt-2 text-sm leading-6 text-slate-700">{description}</p>
                </div>
              </Link>
            ))}
          </div>

          <div className="mt-5 rounded-3xl border border-slate-200 bg-white p-4">
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Workspace progress</p>
              <Badge tone={setupCompleted === setupSteps.length ? 'emerald' : 'amber'}>{setupCompleted}/{setupSteps.length} done</Badge>
            </div>
            <div className="mt-4 space-y-3">
              {setupSteps.map((step) => (
                <Link key={step.title} to={step.to} className="flex items-center justify-between gap-3 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700 transition hover:bg-slate-100">
                  <span>{step.title}</span>
                  <span className={`inline-flex h-2.5 w-2.5 rounded-full ${step.complete ? 'bg-emerald-500' : 'bg-amber-400'}`} />
                </Link>
              ))}
            </div>
          </div>
        </GlassCard>

        <GlassCard className="xl:col-span-8">
          <SectionTitle
            eyebrow="Planning snapshot"
            title="Budgets and goals"
            action={<Link to="/insights" className="text-sm font-semibold text-slate-500 transition hover:text-slate-900">Open details</Link>}
          />

          <div className="grid gap-4 lg:grid-cols-2">
            <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Budgets</p>
                  <h4 className="mt-2 text-xl font-black text-slate-900">Live risk view</h4>
                </div>
                <Badge tone={topBudgets.some((budget) => budget.isOverBudget) ? 'rose' : topBudgets.some((budget) => budget.thresholdReached) ? 'amber' : 'emerald'}>
                  {topBudgets.length === 0 ? 'Not set' : `${topBudgets.length} tracked`}
                </Badge>
              </div>

              <div className="mt-4 space-y-3">
                {topBudgets.length === 0 ? (
                  <div className="rounded-3xl border border-dashed border-slate-300 bg-white p-4 text-sm leading-6 text-slate-500">
                    Create one monthly budget so FinPilot can warn you before spending goes off course.
                  </div>
                ) : topBudgets.map((budget) => (
                  <div key={budget.budgetId} className="rounded-3xl bg-white p-4 shadow-sm">
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-black text-slate-900">{budget.budgetName}</p>
                        <p className="mt-1 text-sm text-slate-500">{formatCurrency(budget.totalSpent)} of {formatCurrency(budget.totalLimit)}</p>
                      </div>
                      <Badge tone={budget.isOverBudget ? 'rose' : budget.thresholdReached ? 'amber' : 'emerald'}>
                        {Math.round(budget.usagePercent)}%
                      </Badge>
                    </div>
                    <div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-200">
                      <div className={`h-full rounded-full ${budget.isOverBudget ? 'bg-rose-500' : budget.thresholdReached ? 'bg-amber-500' : 'bg-emerald-500'}`} style={{ width: progressWidth(budget.usagePercent) }} />
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Goals</p>
                  <h4 className="mt-2 text-xl font-black text-slate-900">Savings momentum</h4>
                </div>
                <Badge tone={topGoals.some((goal) => goal.progressPercent >= 75) ? 'emerald' : 'violet'}>
                  {topGoals.length === 0 ? 'No goals yet' : `${topGoals.length} active`}
                </Badge>
              </div>

              <div className="mt-4 space-y-3">
                {topGoals.length === 0 ? (
                  <div className="rounded-3xl border border-dashed border-slate-300 bg-white p-4 text-sm leading-6 text-slate-500">
                    Add a savings goal to turn the dashboard into a progress tracker, not just a ledger.
                  </div>
                ) : topGoals.map((goal) => (
                  <div key={goal.goalId} className="rounded-3xl bg-white p-4 shadow-sm">
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-black text-slate-900">{goal.goalName}</p>
                        <p className="mt-1 text-sm text-slate-500">{formatCurrency(goal.currentAmount)} of {formatCurrency(goal.targetAmount)}</p>
                      </div>
                      <Badge tone={goal.progressPercent >= 75 ? 'emerald' : 'violet'}>{Math.round(goal.progressPercent)}%</Badge>
                    </div>
                    <div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-200">
                      <div className="h-full rounded-full bg-gradient-to-r from-indigo-500 to-fuchsia-500" style={{ width: progressWidth(goal.progressPercent) }} />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </GlassCard>
      </div>

      <div className="grid gap-6 xl:grid-cols-12">
        <GlassCard className="xl:col-span-8">
          <SectionTitle
            eyebrow="Monthly brief"
            title={state.reportWidget?.title ?? 'This month, simplified'}
            action={<Badge tone="slate">Concise</Badge>}
          />
          <p className="text-base leading-7 text-slate-600">
            {state.reportWidget?.summary ?? 'Add a few weeks of activity and this space will summarize your month in plain English.'}
          </p>
          <div className="mt-5 grid gap-3 md:grid-cols-2">
            {(state.reportWidget?.highlights ?? []).slice(0, 4).map((highlight) => (
              <div key={highlight} className="rounded-3xl border border-slate-200 bg-slate-50 p-4 text-sm leading-6 text-slate-700">
                {highlight}
              </div>
            ))}
            {(state.reportWidget?.highlights ?? []).length === 0 && (
              <div className="rounded-3xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-6 text-slate-500 md:col-span-2">
                The monthly brief will become more useful after at least one income and one expense cycle is tracked.
              </div>
            )}
          </div>
        </GlassCard>

        <GlassCard className="xl:col-span-4">
          <SectionTitle eyebrow="Spending focus" title="Only the top categories" />
          <div className="space-y-3">
            {topSpending.length === 0 ? (
              <div className="rounded-3xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm leading-6 text-slate-500">
                Once expenses are recorded, this area will stay focused on the top categories instead of listing everything.
              </div>
            ) : topSpending.map((item) => (
              <div key={item.categoryId} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="text-sm font-black text-slate-900">{item.categoryName}</p>
                    <p className="mt-1 text-sm text-slate-500">{formatCurrency(item.amount)}</p>
                  </div>
                  <Badge tone="rose">{item.percentage.toFixed(0)}%</Badge>
                </div>
                <div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-200">
                  <div className="h-full rounded-full bg-gradient-to-r from-fuchsia-500 to-rose-500" style={{ width: progressWidth(item.percentage) }} />
                </div>
              </div>
            ))}
          </div>

          <Link
            to="/insights"
            className="mt-5 inline-flex items-center gap-2 text-sm font-semibold text-slate-500 transition hover:text-slate-900"
          >
            View deeper intelligence
            <Sparkles className="h-4 w-4" />
          </Link>
        </GlassCard>
      </div>
    </div>
  );
}
