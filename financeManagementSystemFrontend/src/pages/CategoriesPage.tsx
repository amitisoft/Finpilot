import { useEffect, useMemo, useState } from 'react';
import { Pencil, Plus, Tag, Trash2 } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, transactionTypeLabels } from '../lib/utils';
import type { CategoryResponse } from '../types/api';

const emptyForm = { name: '', type: 2, color: '#6366F1', icon: 'tag' };

export function CategoriesPage() {
  const { api } = useAuth();
  const [items, setItems] = useState<CategoryResponse[]>([]);
  const [selected, setSelected] = useState<CategoryResponse | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await api.get<CategoryResponse[]>('/api/categories'));
    } catch (loadError) {
      setError(flattenErrors(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);
  useEffect(() => {
    if (!selected) return void setForm(emptyForm);
    setForm({ name: selected.name, type: selected.type, color: selected.color ?? '#6366F1', icon: selected.icon ?? 'tag' });
  }, [selected]);

  const customCategories = useMemo(() => items.filter((item) => !item.isDefault), [items]);
  const defaultCategories = useMemo(() => items.filter((item) => item.isDefault), [items]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      if (selected) await api.put(`/api/categories/${selected.id}`, form);
      else await api.post('/api/categories', form);
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
      await api.delete(`/api/categories/${id}`);
      await load();
    } catch (deleteError) {
      setError(flattenErrors(deleteError));
    }
  };

  if (loading) return <LoadingPanel label="Loading categories…" />;

  return (
    <div className="space-y-6">
      <SectionTitle eyebrow="Categories" title="Income and expense taxonomy" action={<Badge tone="violet">{items.length} total</Badge>} />
      {error && <GlassCard className="text-rose-200">{error}</GlassCard>}
      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="space-y-6">
          <GlassCard>
            <SectionTitle eyebrow="Default" title="Seeded categories" />
            <div className="grid gap-3 md:grid-cols-2">
              {defaultCategories.map((item) => (
                <div key={item.id} className="flex items-center justify-between rounded-3xl border border-slate-200 bg-slate-50 p-4">
                  <div className="flex items-center gap-3">
                    <span className="rounded-2xl p-3" style={{ backgroundColor: `${item.color ?? '#64748B'}33`, color: item.color ?? '#cbd5e1' }}><Tag className="h-4 w-4" /></span>
                    <div>
                      <p className="font-black">{item.name}</p>
                      <p className="text-xs uppercase tracking-[0.3em] text-slate-400">{transactionTypeLabels[item.type]}</p>
                    </div>
                  </div>
                  <Badge tone="slate">Default</Badge>
                </div>
              ))}
            </div>
          </GlassCard>

          <GlassCard>
            <SectionTitle eyebrow="Custom" title="Your editable categories" />
            {customCategories.length === 0 ? (
              <EmptyState title="No custom categories" description="Create custom categories for unique spending or income use cases." />
            ) : (
              <div className="grid gap-3 md:grid-cols-2">
                {customCategories.map((item) => (
                  <div key={item.id} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex items-center gap-3">
                        <span className="rounded-2xl p-3" style={{ backgroundColor: `${item.color ?? '#64748B'}33`, color: item.color ?? '#cbd5e1' }}><Tag className="h-4 w-4" /></span>
                        <div>
                          <p className="font-black">{item.name}</p>
                          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">{transactionTypeLabels[item.type]}</p>
                        </div>
                      </div>
                      <Badge tone={item.type === 1 ? 'emerald' : 'rose'}>{transactionTypeLabels[item.type]}</Badge>
                    </div>
                    <div className="mt-4 flex items-center gap-5 text-xs font-black uppercase tracking-[0.3em]">
                      <button onClick={() => setSelected(item)} className="inline-flex items-center gap-2 text-slate-700"><Pencil className="h-4 w-4" />Edit</button>
                      <button onClick={() => void remove(item.id)} className="inline-flex items-center gap-2 text-rose-200"><Trash2 className="h-4 w-4" />Delete</button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </GlassCard>
        </div>

        <GlassCard>
          <SectionTitle eyebrow="Category editor" title={selected ? 'Update category' : 'Create category'} action={<button onClick={() => setSelected(null)} className="rounded-full bg-white/10 px-3 py-1 text-xs font-black uppercase tracking-[0.3em] text-slate-600"><Plus className="inline h-3 w-3" /> New</button>} />
          <form onSubmit={submit} className="space-y-4">
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Name</span>
              <input value={form.name} onChange={(e) => setForm((current) => ({ ...current, name: e.target.value }))} required className="app-form-control w-full" />
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Type</span>
              <select value={form.type} onChange={(e) => setForm((current) => ({ ...current, type: Number(e.target.value) }))} className="app-form-control app-form-control--select w-full">
                <option value={1}>Income</option>
                <option value={2}>Expense</option>
              </select>
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Color</span>
              <input type="color" value={form.color} onChange={(e) => setForm((current) => ({ ...current, color: e.target.value }))} className="h-12 w-full rounded-3xl border border-slate-200 bg-white px-3 py-2" />
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Icon label</span>
              <input value={form.icon} onChange={(e) => setForm((current) => ({ ...current, icon: e.target.value }))} className="app-form-control w-full" />
            </label>
            <button disabled={submitting} className="w-full rounded-3xl bg-white px-5 py-3 text-sm font-black uppercase tracking-[0.35em] text-slate-950 disabled:opacity-60">{submitting ? 'Saving…' : selected ? 'Update Category' : 'Create Category'}</button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
