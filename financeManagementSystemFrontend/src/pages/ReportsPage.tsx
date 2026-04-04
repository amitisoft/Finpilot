import { useEffect, useMemo, useState } from 'react';
import { ArrowRight, Download, LineChart, ScrollText, Wallet } from 'lucide-react';
import { Button } from '../components/Button';
import { Badge, DetailModal, EmptyState, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { PageIntro } from '../components/PageIntro';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatCurrency } from '../lib/utils';
import type { DailyForecastPointResponse, MonthlyForecastResponse, NetWorthPointResponse, ReportTrendPointResponse } from '../types/api';

interface ReportsState {
  forecast: MonthlyForecastResponse | null;
  dailyForecast: DailyForecastPointResponse[];
  trends: ReportTrendPointResponse[];
  netWorth: NetWorthPointResponse[];
}

type ReportDetailKey = 'forecast' | 'trend' | 'netWorth' | null;

function summaryValueClass(value: number, variant: 'default' | 'violet' | 'positive-negative' = 'default') {
  if (variant === 'violet') return 'reports-page__summary-value reports-page__summary-value--violet';
  if (variant === 'positive-negative') {
    return value >= 0 ? 'reports-page__summary-value reports-page__summary-value--positive' : 'reports-page__summary-value reports-page__summary-value--negative';
  }
  return 'reports-page__summary-value';
}

function trendValueClass(value: number, variant: 'income' | 'expense' | 'net') {
  if (variant === 'income') return 'reports-page__trend-metric-value reports-page__trend-metric-value--income';
  if (variant === 'expense') return 'reports-page__trend-metric-value reports-page__trend-metric-value--expense';
  return value >= 0 ? 'reports-page__trend-metric-value reports-page__trend-metric-value--default' : 'reports-page__trend-metric-value reports-page__trend-metric-value--negative';
}

function selectForecastCheckpoints(points: DailyForecastPointResponse[]) {
  if (points.length <= 8) {
    return points;
  }

  const targetCount = 8;
  const indexes = new Set<number>([0, points.length - 1]);

  for (let step = 1; step < targetCount - 1; step += 1) {
    const position = Math.round((step / (targetCount - 1)) * (points.length - 1));
    indexes.add(position);
  }

  return Array.from(indexes)
    .sort((left, right) => left - right)
    .map((index) => points[index]);
}

function buildForecastChart(points: DailyForecastPointResponse[]) {
  if (points.length === 0) {
    return null;
  }

  const width = 720;
  const height = 240;
  const padding = 20;
  const balances = points.map((point) => point.balance);
  const minBalance = Math.min(...balances);
  const maxBalance = Math.max(...balances);
  const range = Math.max(maxBalance - minBalance, 1);

  const chartPoints = points.map((point, index) => {
    const x = padding + (index / Math.max(points.length - 1, 1)) * (width - padding * 2);
    const y = height - padding - ((point.balance - minBalance) / range) * (height - padding * 2);
    return { ...point, x, y };
  });

  const line = chartPoints.map((point) => `${point.x},${point.y}`).join(' ');
  const area = `${padding},${height - padding} ${line} ${width - padding},${height - padding}`;

  return {
    width,
    height,
    line,
    area,
    points: chartPoints,
    minBalance,
    maxBalance
  };
}

export function ReportsPage() {
  const { api } = useAuth();
  const [state, setState] = useState<ReportsState>({ forecast: null, dailyForecast: [], trends: [], netWorth: [] });
  const [loading, setLoading] = useState(true);
  const [downloadLoading, setDownloadLoading] = useState(false);
  const [pageError, setPageError] = useState<string | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const [activeDetail, setActiveDetail] = useState<ReportDetailKey>(null);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setPageError(null);
      try {
        const [forecast, dailyForecast, trends, netWorth] = await Promise.all([
          api.get<MonthlyForecastResponse>('/api/forecast/month'),
          api.get<DailyForecastPointResponse[]>('/api/forecast/daily'),
          api.get<ReportTrendPointResponse[]>('/api/reports/trends'),
          api.get<NetWorthPointResponse[]>('/api/reports/net-worth')
        ]);

        setState({ forecast, dailyForecast, trends, netWorth });
      } catch (loadError) {
        setPageError(flattenErrors(loadError));
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [api]);

  const condensedDailyForecast = useMemo(() => selectForecastCheckpoints(state.dailyForecast), [state.dailyForecast]);
  const forecastChart = useMemo(() => buildForecastChart(condensedDailyForecast), [condensedDailyForecast]);

  const detailItems = useMemo(() => {
    const forecast = state.forecast;
    return [
      {
        key: 'forecast' as const,
        icon: LineChart,
        title: 'Projected balance graph',
        badge: 'Primary',
        note: forecast ? `${forecast.daysRemaining} days remaining` : '30 day view',
        description: forecast
          ? `Projected month-end balance of ${formatCurrency(forecast.projectedEndOfMonthBalance)} with a net movement of ${formatCurrency(forecast.projectedMonthNetAmount)}.`
          : 'Open the 30-day projection chart and forecasting assumptions.'
      },
      {
        key: 'trend' as const,
        icon: ScrollText,
        title: 'Income vs expense trend',
        badge: 'Core report',
        note: `${state.trends.length} checkpoints`,
        description: 'Open the month-by-month operating trend and inspect where net cashflow was positive or negative.'
      },
      {
        key: 'netWorth' as const,
        icon: Wallet,
        title: 'Net worth trajectory',
        badge: 'Reference',
        note: `${state.netWorth.length} checkpoints`,
        description: 'Review balance-sheet movement over time without keeping the full series open on the page.'
      }
    ];
  }, [state.forecast, state.netWorth.length, state.trends.length]);

  const activeDetailMeta = useMemo(() => {
    if (activeDetail === 'forecast') {
      return {
        eyebrow: 'Forecast detail',
        title: 'Projected balance graph',
        description: 'Key balance checkpoints and the assumptions currently driving the month-end forecast.'
      };
    }

    if (activeDetail === 'trend') {
      return {
        eyebrow: 'Trend detail',
        title: 'Income vs expense over time',
        description: 'Monthly operating pattern for income, expenses, and net movement.'
      };
    }

    if (activeDetail === 'netWorth') {
      return {
        eyebrow: 'Balance sheet detail',
        title: 'Net worth trajectory',
        description: 'Checkpoint view of total net worth so the main page stays concise.'
      };
    }

    return null;
  }, [activeDetail]);

  const handleDownload = async () => {
    setDownloadLoading(true);
    setDownloadError(null);
    try {
      const { blob, fileName } = await api.download('/api/reports/pdf');
      const url = window.URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fileName ?? 'finpilot-report.pdf';
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.URL.revokeObjectURL(url);
    } catch (downloadIssue) {
      setDownloadError(flattenErrors(downloadIssue));
    } finally {
      setDownloadLoading(false);
    }
  };

  if (loading) return <LoadingPanel label="Loading reports workspace..." />;
  if (pageError) return <GlassCard className="reports-page__feedback">{pageError}</GlassCard>;
  if (!state.forecast) return <EmptyState title="Reports are not ready yet" description="Add transactions so forecasting and report cards have real history to work with." />;

  return (
    <div className="reports-page">
      <PageIntro
        eyebrow="Reports"
        title="Keep the page concise. Open heavy report detail on demand."
        description="The main page now prioritizes the financial summary, the current forecast, and a short list of report views. Deep charts and long report lanes open in a modal when you need them."
        actions={
          <div className="reports-page__header-actions">
            <Button onClick={() => void handleDownload()} disabled={downloadLoading} iconLeading={<Download size={16} />}>
              {downloadLoading ? 'Preparing PDF…' : 'Download PDF'}
            </Button>
          </div>
        }
        aside={<Badge tone="violet">{state.forecast.confidence}</Badge>}
      />

      {downloadError ? <GlassCard className="reports-page__feedback">{downloadError}</GlassCard> : null}

      <div className="reports-page__summary-grid">
        <div className="reports-page__summary-card">
          <p className="reports-page__summary-label">Current balance</p>
          <p className={summaryValueClass(state.forecast.currentBalance)}>{formatCurrency(state.forecast.currentBalance)}</p>
        </div>
        <div className="reports-page__summary-card">
          <p className="reports-page__summary-label">Projected balance</p>
          <p className={summaryValueClass(state.forecast.projectedEndOfMonthBalance, 'violet')}>{formatCurrency(state.forecast.projectedEndOfMonthBalance)}</p>
        </div>
        <div className="reports-page__summary-card">
          <p className="reports-page__summary-label">Projected month net</p>
          <p className={summaryValueClass(state.forecast.projectedMonthNetAmount, 'positive-negative')}>{formatCurrency(state.forecast.projectedMonthNetAmount)}</p>
        </div>
        <div className="reports-page__summary-card">
          <p className="reports-page__summary-label">Daily pace</p>
          <p className={summaryValueClass(state.forecast.averageDailyNetAmount, 'positive-negative')}>{formatCurrency(state.forecast.averageDailyNetAmount)}</p>
          <p className="reports-page__summary-note">{state.forecast.daysRemaining} days remaining</p>
        </div>
      </div>

      <div className="reports-page__top-grid">
        <GlassCard>
          <SectionTitle eyebrow="Priority" title="What most users need first" action={<Badge tone="emerald">Top level</Badge>} />
          <div className="reports-page__priority-grid">
            <div className="reports-page__priority-card">
              <p className="reports-page__mini-eyebrow">Month-end view</p>
              <h4 className="reports-page__priority-title">{formatCurrency(state.forecast.projectedEndOfMonthBalance)}</h4>
              <p className="reports-page__priority-copy">Projected end-of-month balance based on tracked pace and the remaining days in this cycle.</p>
            </div>
            <div className="reports-page__priority-card">
              <p className="reports-page__mini-eyebrow">Operating note</p>
              <h4 className="reports-page__priority-title">{formatCurrency(state.forecast.projectedRemainingNetAmount)}</h4>
              <p className="reports-page__priority-copy">Expected net movement still left in the month if your current pace holds.</p>
            </div>
          </div>
        </GlassCard>

        <GlassCard>
          <SectionTitle eyebrow="Deep dives" title="Open the report you need" action={<Badge tone="slate">Modal detail</Badge>} />
          <div className="reports-page__detail-list">
            {detailItems.map((item) => {
              const Icon = item.icon;
              return (
                <button key={item.key} type="button" className="reports-page__detail-item" onClick={() => setActiveDetail(item.key)}>
                  <span className="reports-page__detail-icon"><Icon size={18} /></span>
                  <div>
                    <div className="reports-page__detail-head">
                      <h4 className="reports-page__detail-title">{item.title}</h4>
                      <Badge tone={item.key === 'forecast' ? 'violet' : item.key == 'trend' ? 'emerald' : 'slate'}>{item.badge}</Badge>
                    </div>
                    <p className="reports-page__detail-copy">{item.description}</p>
                    <p className="reports-page__detail-note">{item.note}</p>
                  </div>
                  <ArrowRight className="reports-page__detail-arrow" size={18} />
                </button>
              );
            })}
          </div>
        </GlassCard>
      </div>

      <DetailModal
        open={activeDetail !== null}
        title={activeDetailMeta?.title ?? ''}
        eyebrow={activeDetailMeta?.eyebrow}
        description={activeDetailMeta?.description}
        onClose={() => setActiveDetail(null)}
      >
        {activeDetail === 'forecast' ? (
          <div className="reports-page__modal-layout">
            <div className="reports-page__modal-section">
              <div className="reports-page__modal-section-head">
                <SectionTitle eyebrow="Daily path" title="Projected balance graph" action={<Badge tone="violet">{condensedDailyForecast.length} checkpoints</Badge>} />
              </div>
              {forecastChart ? (
                <div className="reports-page__forecast-card">
                  <div className="reports-page__forecast-legend">
                    <div>
                      <p className="reports-page__mini-eyebrow">Start</p>
                      <p className="reports-page__forecast-legend-value">{formatCurrency(forecastChart.minBalance)}</p>
                    </div>
                    <div>
                      <p className="reports-page__mini-eyebrow">Range high</p>
                      <p className="reports-page__forecast-legend-value reports-page__forecast-legend-value--violet">{formatCurrency(forecastChart.maxBalance)}</p>
                    </div>
                  </div>
                  <div className="reports-page__forecast-chart">
                    <svg viewBox={`0 0 ${forecastChart.width} ${forecastChart.height}`} className="reports-page__forecast-svg" aria-hidden="true">
                      <defs>
                        <linearGradient id="forecast-area" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="0%" stopColor="rgba(93, 86, 214, 0.28)" />
                          <stop offset="100%" stopColor="rgba(93, 86, 214, 0.02)" />
                        </linearGradient>
                      </defs>
                      <line x1="20" y1={forecastChart.height - 20} x2={forecastChart.width - 20} y2={forecastChart.height - 20} className="reports-page__forecast-axis" />
                      <polygon points={forecastChart.area} className="reports-page__forecast-area" />
                      <polyline points={forecastChart.line} className="reports-page__forecast-line" />
                      {forecastChart.points.map((point) => (
                        <circle key={point.date} cx={point.x} cy={point.y} r="5" className={`reports-page__forecast-point${point.isProjected ? ' reports-page__forecast-point--projected' : ''}`} />
                      ))}
                    </svg>
                  </div>
                  <div className="reports-page__checkpoint-grid">
                    {condensedDailyForecast.map((point) => (
                      <div key={point.date} className="reports-page__checkpoint">
                        <p className="reports-page__daily-label">{point.label}</p>
                        <p className="reports-page__checkpoint-value">{formatCurrency(point.balance)}</p>
                        <p className="reports-page__checkpoint-note">{point.isProjected ? 'Projected' : 'Tracked'}</p>
                      </div>
                    ))}
                  </div>
                </div>
              ) : null}
            </div>
            <div className="reports-page__modal-section">
              <SectionTitle eyebrow="Assumptions" title="Forecast notes" />
              <div className="reports-page__assumptions">
                {state.forecast.assumptions.map((item) => (
                  <div key={item} className="reports-page__assumption">{item}</div>
                ))}
              </div>
            </div>
          </div>
        ) : null}

        {activeDetail === 'trend' ? (
          <div className="reports-page__modal-layout">
            <div className="reports-page__trend-list">
              {state.trends.map((point) => (
                <div key={point.label} className="reports-page__trend-card">
                  <div className="reports-page__trend-head">
                    <p className="reports-page__trend-label">{point.label}</p>
                    <Badge tone={point.netAmount >= 0 ? 'emerald' : 'rose'}>{point.netAmount >= 0 ? 'Positive' : 'Negative'}</Badge>
                  </div>
                  <div className="reports-page__trend-metrics">
                    <div>
                      <p className="reports-page__trend-metric-label">Income</p>
                      <p className={trendValueClass(point.income, 'income')}>{formatCurrency(point.income)}</p>
                    </div>
                    <div>
                      <p className="reports-page__trend-metric-label">Expense</p>
                      <p className={trendValueClass(point.expense, 'expense')}>{formatCurrency(point.expense)}</p>
                    </div>
                    <div>
                      <p className="reports-page__trend-metric-label">Net</p>
                      <p className={trendValueClass(point.netAmount, 'net')}>{formatCurrency(point.netAmount)}</p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : null}

        {activeDetail === 'netWorth' ? (
          <div className="reports-page__net-worth-grid">
            {state.netWorth.map((point, index) => {
              const previous = index > 0 ? state.netWorth[index - 1].netWorth : point.netWorth;
              const change = point.netWorth - previous;
              return (
                <div key={point.label} className="reports-page__net-worth-card">
                  <p className="reports-page__net-worth-label">{point.label}</p>
                  <p className={summaryValueClass(point.netWorth)}>{formatCurrency(point.netWorth)}</p>
                  <p className="reports-page__net-worth-note">{index === 0 ? 'Starting point' : `${change >= 0 ? '+' : ''}${formatCurrency(change)} vs previous checkpoint`}</p>
                </div>
              );
            })}
          </div>
        ) : null}
      </DetailModal>
    </div>
  );
}
