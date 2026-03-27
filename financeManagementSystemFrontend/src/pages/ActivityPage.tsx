import { useEffect, useState } from 'react';
import { History, Search } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatDateTime } from '../lib/utils';
import type { AuditLogResponse } from '../types/api';

export function ActivityPage() {
  const { api } = useAuth();
  const [items, setItems] = useState<AuditLogResponse[]>([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

  const filtered = items.filter((item) => [item.entityName, item.action, item.oldValues ?? '', item.newValues ?? ''].join(' ').toLowerCase().includes(query.toLowerCase()));

  if (loading) return <LoadingPanel label="Loading audit trail…" />;

  return (
    <div className="space-y-6">
      <SectionTitle eyebrow="Activity Log" title="Read-only audit history" action={<Badge tone="slate">{items.length} records</Badge>} />
      {error && <GlassCard className="text-rose-200">{error}</GlassCard>}
      <GlassCard>
        <div className="mb-5 flex items-center gap-3">
          <label className="relative flex-1">
            <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
            <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search entity, action, serialized values…" className="w-full rounded-3xl border border-slate-200 bg-white py-3 pl-11 pr-4" />
          </label>
          <div className="rounded-3xl bg-slate-50 p-3 text-slate-600"><History className="h-5 w-5" /></div>
        </div>

        {filtered.length === 0 ? (
          <EmptyState title="No activity matched your filter" description="Audit history appears here after login, CRUD actions, and agent interactions." />
        ) : (
          <div className="space-y-3">
            {filtered.map((item) => (
              <div key={item.id} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                <div className="flex flex-wrap items-center gap-3">
                  <Badge tone="slate">{item.entityName}</Badge>
                  <Badge tone={item.action.includes('delete') ? 'rose' : item.action.includes('update') ? 'amber' : 'emerald'}>{item.action}</Badge>
                  <p className="text-xs uppercase tracking-[0.3em] text-slate-500">{formatDateTime(item.createdAt)}</p>
                </div>
                <p className="mt-3 text-sm text-slate-600">Entity ID: {item.entityId}</p>
                <div className="mt-4 grid gap-3 md:grid-cols-2">
                  <div className="rounded-2xl bg-white p-3 text-xs text-slate-600"><p className="mb-2 font-black uppercase tracking-[0.3em] text-slate-500">Old values</p><pre className="overflow-auto whitespace-pre-wrap break-words">{item.oldValues || '—'}</pre></div>
                  <div className="rounded-2xl bg-white p-3 text-xs text-slate-600"><p className="mb-2 font-black uppercase tracking-[0.3em] text-slate-500">New values</p><pre className="overflow-auto whitespace-pre-wrap break-words">{item.newValues || '—'}</pre></div>
                </div>
              </div>
            ))}
          </div>
        )}
      </GlassCard>
    </div>
  );
}
