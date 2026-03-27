import { useEffect, useState } from 'react';
import { Pencil, Plus, Target, Trash2 } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, NumberInput, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatCurrency, formatDate, goalStatusLabels } from '../lib/utils';
import type { GoalResponse } from '../types/api';

const emptyForm = { name: '', targetAmount: 100000, currentAmount: 0, targetDate: '', status: 1 };

export function GoalsPage() {
  const { api } = useAuth();
  const [items, setItems] = useState<GoalResponse[]>([]);
  const [selected, setSelected] = useState<GoalResponse | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await api.get<GoalResponse[]>('/api/goals'));
    } catch (loadError) {
      setError(flattenErrors(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);
  useEffect(() => {
    if (!selected) return void setForm(emptyForm);
    setForm({
      name: selected.name,
      targetAmount: selected.targetAmount,
      currentAmount: selected.currentAmount,
      targetDate: selected.targetDate?.slice(0, 10) ?? '',
      status: selected.status
    });
  }, [selected]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const payload = {
        ...form,
        targetDate: form.targetDate ? new Date(form.targetDate).toISOString() : null
      };
      if (selected) await api.put(`/api/goals/${selected.id}`, payload);
      else await api.post('/api/goals', payload);
      setSelected(null);
      setForm(emptyForm);
      await load();
    } catch (submitError) {
      setError(flattenErrors(submitError));
    } finally {
      setSubmitting(false);
    }
  };

  const remove = async (id: string) => {
    try {
      await api.delete(`/api/goals/${id}`);
      await load();
    } catch (deleteError) {
      setError(flattenErrors(deleteError));
    }
  };

  if (loading) return <LoadingPanel label="Loading strategic goals…" />;

  return (
    <div className="space-y-6">
      <SectionTitle eyebrow="Strategic Goals" title="Progress-first financial milestones" action={<Badge tone="violet">{items.length} tracked</Badge>} />
      {error && <GlassCard className="text-rose-200">{error}</GlassCard>}
      <div className="grid gap-6 xl:grid-cols-[1.25fr_0.75fr]">
        <div>
          {items.length === 0 ? (
            <EmptyState title="No goals yet" description="Create a savings or financial milestone goal to track progress visually." />
          ) : (
            <div className="grid gap-5 md:grid-cols-2">
              {items.map((goal) => (
                <GlassCard key={goal.id} className="relative overflow-hidden">
                  <Target className="absolute right-5 top-5 h-20 w-20 text-white/10" />
                  <div className="relative">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Target date</p>
                        <p className="mt-2 text-sm text-slate-600">{formatDate(goal.targetDate)}</p>
                      </div>
                      <Badge tone={goal.status === 2 ? 'emerald' : goal.status === 3 ? 'slate' : 'violet'}>{goalStatusLabels[goal.status]}</Badge>
                    </div>
                    <h3 className="mt-6 text-3xl font-black">{goal.name}</h3>
                    <p className="mt-4 text-4xl font-black tracking-tighter">{goal.progressPercent.toFixed(0)}%</p>
                    <p className="mt-2 text-sm text-slate-600">{formatCurrency(goal.currentAmount)} of {formatCurrency(goal.targetAmount)}</p>
                    <div className="mt-5 h-3 overflow-hidden rounded-full bg-slate-200">
                      <div className="h-full rounded-full bg-gradient-to-r from-indigo-500 via-violet-500 to-fuchsia-500" style={{ width: `${Math.min(goal.progressPercent, 100)}%` }} />
                    </div>
                    <div className="mt-5 flex items-center gap-5 text-xs font-black uppercase tracking-[0.3em]">
                      <button onClick={() => setSelected(goal)} className="inline-flex items-center gap-2 text-slate-700"><Pencil className="h-4 w-4" />Edit</button>
                      <button onClick={() => void remove(goal.id)} className="inline-flex items-center gap-2 text-rose-200"><Trash2 className="h-4 w-4" />Delete</button>
                    </div>
                  </div>
                </GlassCard>
              ))}
            </div>
          )}
        </div>

        <GlassCard>
          <SectionTitle eyebrow="Goal editor" title={selected ? 'Update goal' : 'Create goal'} action={<button onClick={() => setSelected(null)} className="rounded-full bg-white/10 px-3 py-1 text-xs font-black uppercase tracking-[0.3em] text-slate-600"><Plus className="inline h-3 w-3" /> New</button>} />
          <form onSubmit={submit} className="space-y-4">
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Name</span><input value={form.name} onChange={(e) => setForm((current) => ({ ...current, name: e.target.value }))} required className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Target amount</span><NumberInput min="1" step="0.01" value={form.targetAmount} onValueChange={(targetAmount) => setForm((current) => ({ ...current, targetAmount }))} className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Current amount</span><NumberInput min="0" step="0.01" value={form.currentAmount} blankWhenZero placeholder="0" onValueChange={(currentAmount) => setForm((current) => ({ ...current, currentAmount }))} className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Target date</span><input type="date" value={form.targetDate} onChange={(e) => setForm((current) => ({ ...current, targetDate: e.target.value }))} className="app-form-control w-full" /></label>
            {selected && (
              <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Status</span><select value={form.status} onChange={(e) => setForm((current) => ({ ...current, status: Number(e.target.value) }))} className="app-form-control app-form-control--select w-full">{Object.entries(goalStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
            )}
            <button disabled={submitting} className="w-full rounded-3xl bg-white px-5 py-3 text-sm font-black uppercase tracking-[0.35em] text-slate-950 disabled:opacity-60">{submitting ? 'Saving…' : selected ? 'Update Goal' : 'Create Goal'}</button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}

