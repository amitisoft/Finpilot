import { useEffect, useMemo, useState } from 'react';
import { ArrowRight, ShieldAlert, Sparkles, Wallet } from 'lucide-react';
import { Button } from '../components/Button';
import { Badge, DetailModal, EmptyState, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { PageIntro } from '../components/PageIntro';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatDateTime } from '../lib/utils';
import type { HealthScoreResponse, InsightBundleResponse, InsightsOverviewResponse } from '../types/api';

const insightSections = [
  { key: 'monthly', title: 'Optimizer', endpoint: '/api/insights/monthly?months=6', icon: Sparkles },
  { key: 'budget', title: 'Risk', endpoint: '/api/insights/budget-risk', icon: Wallet },
  { key: 'anomalies', title: 'Anomaly', endpoint: '/api/insights/anomalies', icon: ShieldAlert },
  { key: 'goals', title: 'Goals', endpoint: '/api/insights/goals', icon: Sparkles }
] as const;

type InsightSectionKey = typeof insightSections[number]['key'];

function healthTone(status?: string) {
  if (status === 'strong' || status === 'stable') return 'emerald';
  if (status === 'watch') return 'amber';
  if (status === 'weak') return 'rose';
  return 'slate';
}

export function InsightsPage() {
  const { api } = useAuth();
  const [overview, setOverview] = useState<InsightsOverviewResponse | null>(null);
  const [health, setHealth] = useState<HealthScoreResponse | null>(null);
  const [bundles, setBundles] = useState<Record<string, InsightBundleResponse>>({});
  const [dismissed, setDismissed] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<InsightSectionKey | null>(null);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const [overviewResponse, healthResponse, ...entries] = await Promise.all([
          api.get<InsightsOverviewResponse>('/api/insights'),
          api.get<HealthScoreResponse>('/api/insights/health-score'),
          ...insightSections.map((section) => api.get<InsightBundleResponse>(section.endpoint))
        ]);

        setOverview(overviewResponse);
        setHealth(healthResponse);
        setBundles(Object.fromEntries(insightSections.map((section, index) => [section.key, entries[index]])));
      } catch (loadError) {
        setError(flattenErrors(loadError));
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [api]);

  const visibleSections = useMemo(() => insightSections.map((section) => ({
    ...section,
    data: bundles[section.key],
    cards: (bundles[section.key]?.cards ?? []).filter((card) => !dismissed.includes(`${section.key}:${card.title}`))
  })), [bundles, dismissed]);

  const activeBundle = useMemo(() => visibleSections.find((section) => section.key === activeSection) ?? null, [activeSection, visibleSections]);

  if (loading) return <LoadingPanel label="Loading deep intelligence feed…" />;
  if (error) return <GlassCard className="insights-page__feedback">{error}</GlassCard>;

  const scoreTone = !health ? 'slate' : health.score >= 75 ? 'emerald' : health.score >= 50 ? 'amber' : 'rose';

  return (
    <div className="insights-page">
      <PageIntro
        eyebrow="Deep intelligence"
        title="Keep the page focused on the signals that matter most."
        description="The top of the page now keeps health score and summary guidance visible. Longer insight bundles move into a detail modal so this workspace does not turn into an endless scroll."
        aside={<Badge tone={scoreTone}>{health ? `${health.score}/100` : 'Standby'}</Badge>}
      />

      <div className="insights-page__overview-grid">
        <GlassCard className="insights-page__health-panel">
          <div className="insights-page__score-header">
            <div>
              <p className="insights-page__eyebrow">Health score</p>
              <h3 className="insights-page__score-value">{health?.score ?? 0}/100</h3>
              <p className="insights-page__score-copy">{health?.label ?? 'Health score is waiting for more tracking data.'}</p>
            </div>
            <Badge tone={scoreTone}>{health?.label ?? 'Standby'}</Badge>
          </div>

          <div className="insights-page__breakdown-list">
            {(health?.breakdown ?? []).slice(0, 3).map((item) => (
              <div key={item.category} className="insights-page__breakdown-card">
                <div className="insights-page__breakdown-head">
                  <p className="insights-page__breakdown-title">{item.category}</p>
                  <Badge tone={healthTone(item.status)}>{item.status}</Badge>
                </div>
                <p className="insights-page__card-copy">{item.summary}</p>
              </div>
            ))}
          </div>
        </GlassCard>

        <GlassCard className="insights-page__summary-panel">
          <div className="insights-page__section-header">
            <div>
              <p className="insights-page__eyebrow">Overview</p>
              <h3 className="insights-page__overview-title">{overview?.headline ?? 'Insight center overview is not available.'}</h3>
            </div>
            <Badge tone="violet">{overview?.sections.length ?? 0} lanes</Badge>
          </div>

          <div className="insights-page__overview-lanes">
            {(overview?.sections ?? []).slice(0, 4).map((section) => (
              <div key={section.key} className="insights-page__overview-card">
                <div className="insights-page__overview-section-head">
                  <p className="insights-page__mini-eyebrow">{section.title}</p>
                  <Badge tone={section.priority === 'high' ? 'rose' : section.priority === 'medium' ? 'amber' : 'emerald'}>{section.priority}</Badge>
                </div>
                <p className="insights-page__overview-title">{section.headline}</p>
              </div>
            ))}
          </div>

          <div className="insights-page__strength-grid">
            <div className="insights-page__strength-box">
              <p className="insights-page__mini-eyebrow">Strengths</p>
              <div className="insights-page__strength-list">
                {(health?.strengths ?? []).length === 0
                  ? <div className="insights-page__strength-item">Track more consistent activity to unlock strengths.</div>
                  : health!.strengths.slice(0, 3).map((item) => <div key={item} className="insights-page__strength-item">{item}</div>)}
              </div>
            </div>
            <div className="insights-page__focus-box">
              <p className="insights-page__mini-eyebrow">Focus next</p>
              <div className="insights-page__focus-list">
                {(health?.suggestions ?? []).length === 0
                  ? <div className="insights-page__focus-item">Suggestions will appear after the first few tracked weeks.</div>
                  : health!.suggestions.slice(0, 3).map((item) => <div key={item} className="insights-page__focus-item">{item}</div>)}
              </div>
            </div>
          </div>
          <p className="insights-page__disclaimer">{overview?.disclaimer ?? health?.disclaimer}</p>
        </GlassCard>
      </div>

      <GlassCard>
        <SectionTitle eyebrow="Bundles" title="Open full insight lanes only when you need them" action={<Badge tone="slate">Modal detail</Badge>} />
        <div className="insights-page__bundle-list">
          {visibleSections.map((section) => {
            const Icon = section.icon;
            const primaryTone = section.cards.some((card) => card.priority === 'high') ? 'rose' : section.cards.some((card) => card.priority === 'medium') ? 'amber' : 'emerald';

            return (
              <button key={section.key} type="button" className="insights-page__bundle-item" onClick={() => setActiveSection(section.key)}>
                <span className="insights-page__section-icon"><Icon size={20} /></span>
                <div>
                  <div className="insights-page__bundle-head">
                    <h4 className="insights-page__bundle-title">{section.title}</h4>
                    <div className="insights-page__bundle-badges">
                      <Badge tone={primaryTone}>{section.cards.length} cards</Badge>
                      <Badge tone="slate">Updated {formatDateTime(section.data?.generatedAt)}</Badge>
                    </div>
                  </div>
                  <p className="insights-page__card-copy">{section.data?.headline ?? 'No insight headline available'}</p>
                </div>
                <ArrowRight size={18} className="insights-page__bundle-arrow" />
              </button>
            );
          })}
        </div>
      </GlassCard>

      <DetailModal
        open={activeBundle !== null}
        title={activeBundle?.title ?? ''}
        eyebrow="Insight lane"
        description={activeBundle?.data?.headline ?? 'Detailed cards for this lane.'}
        onClose={() => setActiveSection(null)}
      >
        {activeBundle ? (
          <div className="insights-page__modal-layout">
            <div className="insights-page__modal-meta">
              <Badge tone="slate">Generated {formatDateTime(activeBundle.data?.generatedAt)}</Badge>
              <p className="insights-page__disclaimer">{activeBundle.data?.disclaimer}</p>
            </div>

            <div className="insights-page__cards">
              {activeBundle.cards.length === 0 ? (
                <EmptyState title="No cards left in this section" description="Dismissed insights stay hidden for this session." />
              ) : activeBundle.cards.map((card) => (
                <div key={`${activeBundle.key}:${card.title}`} className="insights-page__signal-card">
                  <div className="insights-page__card-header">
                    <div>
                      <div className="insights-page__card-actions insights-page__card-actions--inline">
                        <Badge tone={card.priority === 'high' ? 'rose' : card.priority === 'medium' ? 'amber' : 'emerald'}>{card.priority}</Badge>
                        <Badge tone="slate">{card.type}</Badge>
                      </div>
                      <h4 className="insights-page__signal-title">{card.title}</h4>
                    </div>
                  </div>
                  <p className="insights-page__card-copy">{card.summary}</p>
                  <div className="insights-page__recommendations">
                    {card.recommendations.map((recommendation) => (
                      <div key={recommendation} className="insights-page__recommendation">{recommendation}</div>
                    ))}
                  </div>
                  <div className="insights-page__card-actions">
                    <Button variant="secondary" size="sm" onClick={() => setDismissed((current) => [...current, `${activeBundle.key}:${card.title}`])}>Dismiss</Button>
                    <Button size="sm" iconTrailing={<ArrowRight size={14} />}>Primary action</Button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : null}
      </DetailModal>
    </div>
  );
}
