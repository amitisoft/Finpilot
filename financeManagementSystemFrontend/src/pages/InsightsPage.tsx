import { useEffect, useMemo, useState } from 'react';
import { ArrowRight, ShieldAlert, Sparkles, Wallet } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatDateTime } from '../lib/utils';
import type { InsightBundleResponse } from '../types/api';

const insightSections = [
  { key: 'monthly', title: 'Optimizer', endpoint: '/api/insights/monthly?months=6', icon: Sparkles },
  { key: 'budget', title: 'Risk', endpoint: '/api/insights/budget-risk', icon: Wallet },
  { key: 'anomalies', title: 'Anomaly', endpoint: '/api/insights/anomalies', icon: ShieldAlert },
  { key: 'goals', title: 'Goals', endpoint: '/api/insights/goals', icon: Sparkles }
] as const;

export function InsightsPage() {
  const { api } = useAuth();
  const [bundles, setBundles] = useState<Record<string, InsightBundleResponse>>({});
  const [dismissed, setDismissed] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const entries = await Promise.all(insightSections.map(async (section) => [section.key, await api.get<InsightBundleResponse>(section.endpoint)] as const));
        setBundles(Object.fromEntries(entries));
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

  if (loading) return <LoadingPanel label="Loading deep intelligence feed…" />;
  if (error) return <GlassCard className="text-rose-200">{error}</GlassCard>;

  return (
    <div className="space-y-6">
      <SectionTitle eyebrow="Deep Intelligence" title="Actionable AI insight feed" action={<Badge tone="violet">4 channels</Badge>} />
      <div className="grid gap-6 xl:grid-cols-2">
        {visibleSections.map((section) => {
          const Icon = section.icon;
          return (
            <GlassCard key={section.key}>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">{section.title}</p>
                  <h3 className="mt-2 text-2xl font-black">{section.data?.headline ?? 'No insight headline available'}</h3>
                </div>
                <div className="rounded-3xl bg-white/10 p-4 text-violet-200"><Icon className="h-5 w-5" /></div>
              </div>
              <p className="mt-3 text-xs uppercase tracking-[0.3em] text-slate-500">Generated {formatDateTime(section.data?.generatedAt)}</p>
              <div className="mt-5 space-y-4">
                {section.cards.length === 0 ? (
                  <EmptyState title="No cards left in this section" description="Dismissed insights stay hidden for this session." />
                ) : section.cards.map((card) => (
                  <div key={`${section.key}:${card.title}`} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <div className="flex items-center gap-2">
                          <Badge tone={card.priority === 'high' ? 'rose' : card.priority === 'medium' ? 'amber' : 'emerald'}>{card.priority}</Badge>
                          <Badge tone="slate">{card.type}</Badge>
                        </div>
                        <h4 className="mt-3 text-xl font-black">{card.title}</h4>
                      </div>
                      <button onClick={() => setDismissed((current) => [...current, `${section.key}:${card.title}`])} className="text-xs font-black uppercase tracking-[0.3em] text-slate-400">Dismiss</button>
                    </div>
                    <p className="mt-3 text-sm text-slate-600">{card.summary}</p>
                    <div className="mt-4 space-y-2">
                      {card.recommendations.map((recommendation) => (
                        <div key={recommendation} className="rounded-2xl bg-slate-50 px-3 py-2 text-sm text-slate-700">{recommendation}</div>
                      ))}
                    </div>
                    <button className="mt-4 inline-flex items-center gap-2 rounded-full bg-white px-4 py-2 text-xs font-black uppercase tracking-[0.3em] text-slate-950">
                      Primary action <ArrowRight className="h-3.5 w-3.5" />
                    </button>
                  </div>
                ))}
              </div>
              <p className="mt-4 text-xs text-slate-500">{section.data?.disclaimer}</p>
            </GlassCard>
          );
        })}
      </div>
    </div>
  );
}
