import { useEffect, useMemo, useState } from 'react';
import type { LucideIcon } from 'lucide-react';
import { ArrowRight, BarChart3, Bot, LineChart, PiggyBank, ReceiptText, ShieldCheck, Sparkles, Target, TrendingDown, TrendingUp, WalletCards } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Badge, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { PageIntro } from '../components/PageIntro';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatCompactCurrency, formatCurrency } from '../lib/utils';
import type {
  BudgetStatusResponse,
  CategoryBreakdownResponse,
  DashboardCoachWidgetResponse,
  DashboardReportWidgetResponse,
  DashboardSummaryResponse,
  GoalProgressResponse,
  HealthScoreResponse,
  MonthlyForecastResponse
} from '../types/api';

interface DashboardState {
  summary: DashboardSummaryResponse | null;
  breakdown: CategoryBreakdownResponse[];
  budgetHealth: BudgetStatusResponse[];
  goalProgress: GoalProgressResponse[];
  coachWidget: DashboardCoachWidgetResponse | null;
  reportWidget: DashboardReportWidgetResponse | null;
  forecast: MonthlyForecastResponse | null;
  health: HealthScoreResponse | null;
}

type CardTone = 'slate' | 'emerald' | 'rose' | 'amber' | 'violet';

interface WorkspaceCard {
  to: string;
  label: string;
  icon: LucideIcon;
  tone: CardTone;
  status: string;
  description: string;
}

function progressWidth(value: number) {
  return `${Math.max(0, Math.min(value, 100))}%`;
}

function metricVariantClass(variant: 'default' | 'income' | 'expense' | 'projection' = 'default') {
  return variant === 'default' ? 'dashboard-page__metric' : `dashboard-page__metric dashboard-page__metric--${variant}`;
}

export function DashboardPage() {
  const { api } = useAuth();
  const [state, setState] = useState<DashboardState>({
    summary: null,
    breakdown: [],
    budgetHealth: [],
    goalProgress: [],
    coachWidget: null,
    reportWidget: null,
    forecast: null,
    health: null
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const run = async () => {
      setLoading(true);
      setError(null);
      try {
        const [summary, breakdown, budgetHealth, goalProgress, coachWidget, reportWidget, forecast, health] = await Promise.all([
          api.get<DashboardSummaryResponse>('/api/dashboard/summary'),
          api.get<CategoryBreakdownResponse[]>('/api/dashboard/category-breakdown'),
          api.get<BudgetStatusResponse[]>('/api/dashboard/budget-health'),
          api.get<GoalProgressResponse[]>('/api/dashboard/goal-progress'),
          api.get<DashboardCoachWidgetResponse>('/api/agents/widgets/coach'),
          api.get<DashboardReportWidgetResponse>('/api/agents/widgets/report'),
          api.get<MonthlyForecastResponse>('/api/forecast/month'),
          api.get<HealthScoreResponse>('/api/insights/health-score')
        ]);

        setState({ summary, breakdown, budgetHealth, goalProgress, coachWidget, reportWidget, forecast, health });
      } catch (dashboardError) {
        setError(flattenErrors(dashboardError));
      } finally {
        setLoading(false);
      }
    };

    void run();
  }, [api]);

  const topBudget = useMemo(() => state.budgetHealth[0] ?? null, [state.budgetHealth]);
  const topGoal = useMemo(() => state.goalProgress[0] ?? null, [state.goalProgress]);
  const topSpending = useMemo(() => state.breakdown[0] ?? null, [state.breakdown]);
  const summary = state.summary;
  const healthScore = state.health?.score ?? state.coachWidget?.healthScore ?? 0;
  const hasTrackedActivity =
    (summary?.transactionCount ?? 0) > 0 ||
    (summary?.totalIncome ?? 0) > 0 ||
    (summary?.totalExpenses ?? 0) > 0 ||
    state.budgetHealth.length > 0 ||
    state.goalProgress.length > 0;

  const healthTone: CardTone = !hasTrackedActivity ? 'amber' : healthScore >= 70 ? 'emerald' : healthScore >= 45 ? 'amber' : 'rose';
  const statusLabel = !hasTrackedActivity ? 'Setup mode' : healthScore >= 70 ? 'Strong footing' : healthScore >= 45 ? 'Needs attention' : 'Action required';

  const setupSteps = [
    { title: 'Connect an account', complete: (summary?.totalBalance ?? 0) > 0 || (summary?.transactionCount ?? 0) > 0, to: '/accounts' },
    { title: 'Capture your first transactions', complete: (summary?.transactionCount ?? 0) > 0, to: '/transactions' },
    { title: 'Set a budget or a goal', complete: state.budgetHealth.length > 0 || state.goalProgress.length > 0, to: state.budgetHealth.length > 0 ? '/goals' : '/budgets' }
  ];

  const setupCompleted = setupSteps.filter((step) => step.complete).length;

  const workspaceCards = useMemo<WorkspaceCard[]>(() => {
    const totalBalance = summary?.totalBalance ?? 0;
    const transactionCount = summary?.transactionCount ?? 0;

    return [
    {
      to: '/accounts',
      label: 'Accounts',
      icon: WalletCards,
      tone: totalBalance > 0 ? 'emerald' : 'amber',
      status: totalBalance > 0 ? formatCompactCurrency(totalBalance) : 'Needs setup',
      description: totalBalance > 0
        ? 'Balances are live. Open Accounts to add, rename, or review funding sources.'
        : 'Add a bank or cash account so FinPilot can calculate a real position.'
    },
    {
      to: '/transactions',
      label: 'Ledger',
      icon: ReceiptText,
      tone: transactionCount > 0 ? 'emerald' : 'amber',
      status: `${transactionCount} tracked`,
      description: transactionCount > 0
        ? 'Income and expense history is flowing. Open the ledger for edits and deeper review.'
        : 'Add income and expenses to unlock reports, coaching, and forecasting.'
    },
    {
      to: '/budgets',
      label: 'Budgets',
      icon: PiggyBank,
      tone: topBudget ? (topBudget.isOverBudget ? 'rose' : topBudget.thresholdReached ? 'amber' : 'emerald') : 'amber',
      status: topBudget ? `${state.budgetHealth.length} active` : 'Not set',
      description: topBudget
        ? `${topBudget.budgetName} is currently at ${Math.round(topBudget.usagePercent)}% usage.`
        : 'Create a monthly budget to turn tracking into an operating plan.'
    },
    {
      to: '/goals',
      label: 'Goals',
      icon: Target,
      tone: topGoal ? (topGoal.progressPercent >= 75 ? 'emerald' : 'violet') : 'amber',
      status: topGoal ? `${state.goalProgress.length} active` : 'Not set',
      description: topGoal
        ? `${topGoal.goalName} is ${Math.round(topGoal.progressPercent)}% complete.`
        : 'Add a savings target so the app can track progress, not just balances.'
    },
    {
      to: '/reports',
      label: 'Reports',
      icon: LineChart,
      tone: state.forecast?.projectedEndOfMonthBalance && state.forecast.projectedEndOfMonthBalance >= (summary?.totalBalance ?? 0) ? 'emerald' : 'violet',
      status: state.forecast ? state.forecast.confidence : 'Live',
      description: state.forecast
        ? `Projected balance is ${formatCompactCurrency(state.forecast.projectedEndOfMonthBalance)} by month end. Open Reports for forecasts and net worth trajectory.`
        : 'Forecasting and trend reporting appear here once meaningful activity is tracked.'
    },
    {
      to: '/insights',
      label: 'Insights',
      icon: BarChart3,
      tone: topSpending ? 'violet' : 'slate',
      status: topSpending ? `${state.breakdown.length} categories` : 'Waiting for data',
      description: topSpending
        ? `${topSpending.categoryName} is the current top spending driver. Open Insights for the why behind the numbers.`
        : 'Monthly, risk, anomaly, and goal insights appear once activity is tracked.'
    },
    {
      to: '/coach',
      label: 'AI Coach',
      icon: Bot,
      tone: healthTone,
      status: hasTrackedActivity ? `${healthScore}/100` : 'Standby',
      description: state.coachWidget?.primaryAction ?? 'Use the coach when you want the app to turn raw data into next-step guidance.'
    }
    ];
  }, [healthScore, healthTone, hasTrackedActivity, state.breakdown.length, state.budgetHealth.length, state.coachWidget?.primaryAction, state.forecast, state.goalProgress.length, summary?.totalBalance, summary?.transactionCount, topBudget, topGoal, topSpending]);


  const systemLinks = useMemo<WorkspaceCard[]>(() => [
    {
      to: '/categories',
      label: 'Categories',
      icon: ShieldCheck,
      tone: 'slate',
      status: 'Manage taxonomy',
      description: 'Refine income and expense labels so reports and dashboards stay clean.'
    },
  ], []);

  if (loading) return <LoadingPanel label="Loading command center..." />;
  if (error) return <GlassCard className="dashboard-page__empty-state">{error}</GlassCard>;
  if (!summary) return <GlassCard className="dashboard-page__empty-state">No dashboard data available.</GlassCard>;

  return (
    <div className="dashboard-page">
      <SectionTitle eyebrow="Dashboard" title="Financial Command Center" action={<Badge tone={healthTone}>{statusLabel}</Badge>} />

      <GlassCard className="dashboard-page__hero">
        <PageIntro
          eyebrow="Overview"
          title={formatCompactCurrency(summary.totalBalance)}
          description={hasTrackedActivity
            ? 'This home page stays intentionally brief: current position, projected month-end balance, health score, and a clean route into each dedicated workspace.'
            : 'Keep setup lightweight. Add an account, record a few transactions, then move into the page built for the job.'}
          actions={
            <>
              <Link to={hasTrackedActivity ? '/transactions' : '/accounts'} className="app-button app-button--primary app-button--md">
                <span className="app-button__label">{hasTrackedActivity ? 'Open ledger' : 'Add first account'}</span>
                <ArrowRight className="app-button__icon app-button__icon--trailing" size={16} />
              </Link>
              <Link to="/reports" className="app-button app-button--secondary app-button--md">
                <span className="app-button__label">Open reports</span>
                <LineChart className="app-button__icon app-button__icon--trailing" size={16} />
              </Link>
            </>
          }
        />

        <div className="dashboard-page__metrics">
          <div className={metricVariantClass()}>
            <p className="dashboard-page__metric-label">Balance</p>
            <p className="dashboard-page__metric-value dashboard-page__metric-value--default">{formatCurrency(summary.totalBalance)}</p>
          </div>
          <div className={metricVariantClass('income')}>
            <div className="dashboard-page__metric-header">
              <TrendingUp className="dashboard-page__metric-icon" />
              <p className="dashboard-page__metric-label">Income</p>
            </div>
            <p className="dashboard-page__metric-value">{formatCurrency(summary.totalIncome)}</p>
          </div>
          <div className={metricVariantClass('expense')}>
            <div className="dashboard-page__metric-header">
              <TrendingDown className="dashboard-page__metric-icon" />
              <p className="dashboard-page__metric-label">Expenses</p>
            </div>
            <p className="dashboard-page__metric-value">{formatCurrency(summary.totalExpenses)}</p>
          </div>
          <div className={metricVariantClass()}>
            <p className="dashboard-page__metric-label">This month</p>
            <p className="dashboard-page__metric-value dashboard-page__metric-value--default">{formatCurrency(summary.netAmount)}</p>
            <p className="dashboard-page__metric-note">{summary.transactionCount} transactions</p>
          </div>
          <div className={metricVariantClass('projection')}>
            <p className="dashboard-page__metric-label">Projected balance</p>
            <p className="dashboard-page__metric-value">{formatCurrency(state.forecast?.projectedEndOfMonthBalance ?? summary.totalBalance)}</p>
            <p className="dashboard-page__metric-note">{state.forecast?.daysRemaining ?? 0} days remaining</p>
          </div>
          <div className={metricVariantClass()}>
            <p className="dashboard-page__metric-label">Health score</p>
            <p className="dashboard-page__metric-value dashboard-page__metric-value--default">{healthScore}/100</p>
            <p className="dashboard-page__metric-note">{state.health?.label ?? 'Standby'}</p>
          </div>
        </div>
      </GlassCard>

      <GlassCard>
        <SectionTitle eyebrow="Workspaces" title="Open the page built for the job" />
        <div className="dashboard-page__feature-grid">
          {workspaceCards.map(({ to, label, icon: Icon, tone, status, description }) => (
            <Link key={to} to={to} className="dashboard-page__feature-card">
              <div className="dashboard-page__feature-card-header">
                <span className="dashboard-page__feature-card-icon"><Icon size={20} /></span>
                <ArrowRight className="dashboard-page__feature-card-arrow" />
              </div>
              <div className="dashboard-page__feature-card-top">
                <h4 className="dashboard-page__feature-card-title">{label}</h4>
                <Badge tone={tone}>{status}</Badge>
              </div>
              <p className="dashboard-page__feature-card-copy">{description}</p>
            </Link>
          ))}
        </div>
      </GlassCard>

      <div className="dashboard-page__triptych">
        <GlassCard className="dashboard-page__triptych-card">
          <SectionTitle eyebrow="Setup" title="Keep the homepage small" action={<Badge tone={setupCompleted === setupSteps.length ? 'emerald' : 'amber'}>{setupCompleted}/{setupSteps.length} done</Badge>} />
          <div className="dashboard-page__progress-bar">
            <div className="dashboard-page__progress-fill" style={{ width: progressWidth((setupCompleted / setupSteps.length) * 100) }} />
          </div>
          <div className="dashboard-page__setup-list">
            {setupSteps.map((step) => (
              <Link key={step.title} to={step.to} className="dashboard-page__setup-link">
                <span>{step.title}</span>
                <span className={`dashboard-page__setup-dot dashboard-page__setup-dot--${step.complete ? 'complete' : 'pending'}`} />
              </Link>
            ))}
          </div>
        </GlassCard>

        <GlassCard className="dashboard-page__triptych-card">
          <SectionTitle eyebrow="Coach spotlight" title={state.coachWidget?.headline ?? 'AI coach'} action={<Badge tone={healthTone}>{hasTrackedActivity ? `${healthScore}/100` : 'Standby'}</Badge>} />
          <p className="dashboard-page__coach-copy">{state.coachWidget?.encouragement ?? 'Once activity is tracked, the coach will turn this space into a concrete recommendation instead of a generic placeholder.'}</p>
          <div className="dashboard-page__coach-action">
            <p className="dashboard-page__coach-action-title">Primary action</p>
            <p className="dashboard-page__coach-action-copy">{state.coachWidget?.primaryAction ?? 'Add income, expenses, and a budget to unlock targeted coaching.'}</p>
          </div>
          <Link to="/coach" className="dashboard-page__inline-link">
            <span>Open full coach workspace</span>
            <Bot size={16} />
          </Link>
        </GlassCard>

        <GlassCard className="dashboard-page__triptych-card">
          <SectionTitle eyebrow="Monthly brief" title={state.reportWidget?.title ?? 'Short report'} action={<Badge tone="slate">Summary</Badge>} />
          <p className="dashboard-page__brief-copy">{state.reportWidget?.summary ?? 'Use this card for a one-glance summary, then jump into Reports or Insights for the deeper breakdown.'}</p>
          {(state.reportWidget?.highlights?.length ?? 0) > 0 && (
            <div className="dashboard-page__brief-highlights">
              {state.reportWidget!.highlights.slice(0, 2).map((highlight) => (
                <div key={highlight} className="dashboard-page__brief-highlight">{highlight}</div>
              ))}
            </div>
          )}
          <Link to="/reports" className="dashboard-page__inline-link">
            <span>Open reports page</span>
            <Sparkles size={16} />
          </Link>
        </GlassCard>
      </div>

      <GlassCard>
        <SectionTitle eyebrow="Utilities" title="Open supporting pages only when you need them" />
        <div className="dashboard-page__utility-grid">
          {systemLinks.map(({ to, label, icon: Icon, tone, status, description }) => (
            <Link key={to} to={to} className="dashboard-page__utility-card">
              <span className="dashboard-page__utility-card-icon"><Icon size={20} /></span>
              <div>
                <div className="dashboard-page__utility-card-top">
                  <h4 className="dashboard-page__utility-card-title">{label}</h4>
                  <Badge tone={tone}>{status}</Badge>
                </div>
                <p className="dashboard-page__utility-card-copy">{description}</p>
              </div>
            </Link>
          ))}
        </div>
      </GlassCard>
    </div>
  );
}
