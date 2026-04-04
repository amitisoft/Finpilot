import { useEffect, useState } from 'react';
import { Pencil, Plus, Target, Trash2 } from 'lucide-react';
import { Button } from '../components/Button';
import { FormField } from '../components/FormField';
import { PageIntro } from '../components/PageIntro';
import { Badge, EmptyState, GlassCard, LoadingPanel } from '../components/Ui';
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
    if (!selected) {
      setForm(emptyForm);
      return;
    }
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
    <div className="finance-page">
      <PageIntro
        eyebrow="Strategic goals"
        title="Progress-first financial milestones"
        description="Track the goals that matter most, whether they are emergency savings, major purchases, or long-horizon financial targets."
        aside={<Badge tone="violet">{items.length} tracked</Badge>}
      />

      {error ? <GlassCard className="finance-page__feedback">{error}</GlassCard> : null}

      <div className="finance-page__grid finance-page__grid--goals">
        <div className="finance-page__stack">
          {items.length === 0 ? (
            <div className="finance-page__empty">
              <EmptyState title="No goals yet" description="Create a savings or financial milestone goal to track progress visually." />
            </div>
          ) : (
            <div className="finance-page__cards">
              {items.map((goal) => (
                <GlassCard key={goal.id} className="finance-page__card">
                  <Target className="finance-page__goal-watermark" />
                  <div className="finance-page__card-top">
                    <div>
                      <p className="finance-page__card-eyebrow">Target date</p>
                      <p className="finance-page__card-subtitle">{formatDate(goal.targetDate)}</p>
                    </div>
                    <Badge tone={goal.status === 2 ? 'emerald' : goal.status === 3 ? 'slate' : 'violet'}>{goalStatusLabels[goal.status]}</Badge>
                  </div>
                  <h3 className="finance-page__card-title">{goal.name}</h3>
                  <p className="finance-page__card-value">{goal.progressPercent.toFixed(0)}%</p>
                  <p className="finance-page__card-note">{formatCurrency(goal.currentAmount)} of {formatCurrency(goal.targetAmount)}</p>
                  <div className="finance-page__progress">
                    <div className="finance-page__progress-fill" style={{ width: `${Math.min(goal.progressPercent, 100)}%` }} />
                  </div>
                  <div className="finance-page__card-actions">
                    <Button variant="secondary" size="sm" onClick={() => setSelected(goal)} iconLeading={<Pencil size={14} />}>Edit</Button>
                    <Button variant="ghost" size="sm" onClick={() => void remove(goal.id)} iconLeading={<Trash2 size={14} />} className="finance-page__danger-button">Delete</Button>
                  </div>
                </GlassCard>
              ))}
            </div>
          )}
        </div>

        <GlassCard className="finance-page__editor">
          <PageIntro
            eyebrow="Goal editor"
            title={selected ? 'Update goal' : 'Create goal'}
            description="Define the target, current progress, and timeline so FinPilot can show realistic momentum."
            aside={<Button variant="secondary" size="sm" onClick={() => setSelected(null)} iconLeading={<Plus size={14} />}>New</Button>}
          />

          <form onSubmit={submit} className="finance-page__form">
            <FormField label="Name">
              <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required className="app-form-control" />
            </FormField>

            <FormField label="Target amount">
              <input value={form.targetAmount} onChange={(event) => setForm((current) => ({ ...current, targetAmount: Number(event.target.value) || 0 }))} type="number" min="1" step="0.01" className="app-form-control" />
            </FormField>

            <FormField label="Current amount">
              <input value={form.currentAmount} onChange={(event) => setForm((current) => ({ ...current, currentAmount: Number(event.target.value) || 0 }))} type="number" min="0" step="0.01" className="app-form-control" />
            </FormField>

            <FormField label="Target date">
              <input type="date" value={form.targetDate} onChange={(event) => setForm((current) => ({ ...current, targetDate: event.target.value }))} className="app-form-control" />
            </FormField>

            {selected ? (
              <FormField label="Status">
                <select value={form.status} onChange={(event) => setForm((current) => ({ ...current, status: Number(event.target.value) }))} className="app-form-control app-form-control--select">
                  {Object.entries(goalStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </FormField>
            ) : null}

            <Button type="submit" fullWidth size="lg" disabled={submitting}>
              {submitting ? 'Saving…' : selected ? 'Update goal' : 'Create goal'}
            </Button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
