import { useEffect, useMemo, useState } from 'react';
import { History, Search } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { PageIntro } from '../components/PageIntro';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatDateTime } from '../lib/utils';
import type { AuditLogResponse } from '../types/api';

type AuditFieldEntry = {
  key: string;
  value: string;
};

type AuditValueTab = 'new' | 'old';

function normalizeAuditValue(value: unknown): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }

  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
    return String(value);
  }

  return JSON.stringify(value);
}

function parseAuditEntries(raw?: string | null): AuditFieldEntry[] {
  if (!raw) {
    return [];
  }

  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return [{ key: 'value', value: normalizeAuditValue(parsed) }];
    }

    return Object.entries(parsed as Record<string, unknown>).map(([key, value]) => ({
      key,
      value: normalizeAuditValue(value)
    }));
  } catch {
    return [{ key: 'value', value: raw }];
  }
}

function actionTone(action: string) {
  const lower = action.toLowerCase();
  if (lower.includes('delete')) return 'rose';
  if (lower.includes('update')) return 'amber';
  return 'emerald';
}

export function ActivityPage() {
  const { api } = useAuth();
  const [items, setItems] = useState<AuditLogResponse[]>([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTabs, setActiveTabs] = useState<Record<string, AuditValueTab>>({});

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        setItems(await api.get<AuditLogResponse[]>('/api/audit-logs?take=100'));
      } catch (loadError) {
        setError(flattenErrors(loadError));
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [api]);

  const filtered = useMemo(
    () => items.filter((item) => [item.entityName, item.action, item.oldValues ?? '', item.newValues ?? ''].join(' ').toLowerCase().includes(query.toLowerCase())),
    [items, query]
  );

  if (loading) return <LoadingPanel label="Loading audit trail…" />;

  return (
    <div className="activity-page">
      <PageIntro
        eyebrow="Activity log"
        title="Read-only audit history"
        description="See what happened without exposing raw payloads. Each record now highlights the important fields in a cleaner, user-facing format."
        aside={<Badge tone="slate">{items.length} records</Badge>}
      />

      {error ? <GlassCard className="activity-page__feedback">{error}</GlassCard> : null}

      <GlassCard>
        <div className="activity-page__toolbar">
          <label className="activity-page__search">
            <Search className="activity-page__search-icon" size={16} />
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search entity, action, changed fields…"
              className="app-form-control activity-page__search-input"
            />
          </label>
          <div className="activity-page__toolbar-icon" aria-hidden="true">
            <History size={18} />
          </div>
        </div>

        {filtered.length === 0 ? (
          <EmptyState title="No activity matched your filter" description="Audit history appears here after login, CRUD actions, and agent interactions." />
        ) : (
          <div className="activity-page__list">
            {filtered.map((item) => {
              const oldEntries = parseAuditEntries(item.oldValues);
              const newEntries = parseAuditEntries(item.newValues);
              const activeTab = activeTabs[item.id] ?? (newEntries.length > 0 ? 'new' : 'old');
              const visibleEntries = activeTab === 'new' ? newEntries : oldEntries;

              return (
                <article key={item.id} className="activity-page__item">
                  <div className="activity-page__item-head">
                    <div className="activity-page__item-meta">
                      <Badge tone="slate">{item.entityName}</Badge>
                      <Badge tone={actionTone(item.action)}>{item.action}</Badge>
                      <p className="activity-page__timestamp">{formatDateTime(item.createdAt)}</p>
                    </div>
                    <p className="activity-page__entity-id">Entity ID: {item.entityId}</p>
                  </div>

                  <div className="activity-page__tabs" role="tablist" aria-label={`Values for ${item.entityName}`}>
                    <button
                      type="button"
                      role="tab"
                      className={`activity-page__tab${activeTab === 'new' ? ' activity-page__tab--active' : ''}`}
                      aria-selected={activeTab === 'new'}
                      onClick={() => setActiveTabs((current) => ({ ...current, [item.id]: 'new' }))}
                    >
                      New values
                    </button>
                    <button
                      type="button"
                      role="tab"
                      className={`activity-page__tab${activeTab === 'old' ? ' activity-page__tab--active' : ''}`}
                      aria-selected={activeTab === 'old'}
                      onClick={() => setActiveTabs((current) => ({ ...current, [item.id]: 'old' }))}
                    >
                      Old values
                    </button>
                  </div>

                  <div className="activity-page__values-panel">
                    {visibleEntries.length === 0 ? (
                      <div className="activity-page__empty-values">No {activeTab} values recorded for this event.</div>
                    ) : (
                      <div className="activity-page__values-list">
                        {visibleEntries.map((entry) => (
                          <div key={`${item.id}-${activeTab}-${entry.key}`} className="activity-page__value-row">
                            <p className="activity-page__value-key">{entry.key}</p>
                            <p className="activity-page__value-text">{entry.value}</p>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </GlassCard>
    </div>
  );
}
