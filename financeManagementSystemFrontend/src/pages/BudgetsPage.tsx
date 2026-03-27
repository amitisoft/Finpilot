import { useEffect, useMemo, useState } from 'react';
import { Pencil, Plus, Sparkles, Trash2 } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, NumberInput, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatCurrency, monthName } from '../lib/utils';
import type { BudgetResponse, BudgetStatusResponse, CategoryResponse } from '../types/api';

const now = new Date();
const monthOptions = Array.from({ length: 12 }, (_, index) => ({ value: index + 1, label: monthName(index + 1) }));

const createEmptyForm = () => ({
  name: '',
  month: now.getMonth() + 1,
  year: now.getFullYear(),
  totalLimit: 10000,
  alertThresholdPercent: 80,
  items: [{ categoryId: '', limitAmount: 5000 }]
});

export function BudgetsPage() {
  const { api } = useAuth();
  const [budgets, setBudgets] = useState<BudgetResponse[]>([]);
  const [budgetHealth, setBudgetHealth] = useState<BudgetStatusResponse[]>([]);
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [selected, setSelected] = useState<BudgetResponse | null>(null);
  const [form, setForm] = useState(createEmptyForm());
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [budgetData, budgetStatusData, categoryData] = await Promise.all([
        api.get<BudgetResponse[]>('/api/budgets'),
        api.get<BudgetStatusResponse[]>('/api/budgets/status'),
        api.get<CategoryResponse[]>('/api/categories')
      ]);
      setBudgets(budgetData);
      setBudgetHealth(budgetStatusData);
      setCategories(categoryData.filter((item) => item.type === 2));
    } catch (loadError) {
      setError(flattenErrors(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  useEffect(() => {
    if (!selected) {
      const next = createEmptyForm();
      next.items = [{ categoryId: categories[0]?.id ?? '', limitAmount: 5000 }];
      setForm(next);
      return;
    }

    setForm({
      name: selected.name,
      month: selected.month,
      year: selected.year,
      totalLimit: selected.totalLimit,
      alertThresholdPercent: selected.alertThresholdPercent,
      items: selected.items.map((item) => ({ categoryId: item.categoryId, limitAmount: item.limitAmount }))
    });
  }, [selected, categories]);

  const remove = async (id: string) => {
    try {
      await api.delete(`/api/budgets/${id}`);
      await load();
    } catch (deleteError) {
      setError(flattenErrors(deleteError));
    }
  };

  const addItem = () => setForm((current) => ({ ...current, items: [...current.items, { categoryId: categories[0]?.id ?? '', limitAmount: 0 }] }));
  const updateItem = (index: number, key: 'categoryId' | 'limitAmount', value: string | number) => {
    setForm((current) => ({
      ...current,
      items: current.items.map((item, itemIndex) => itemIndex === index ? { ...item, [key]: value } : item)
    }));
  };

  const removeItem = (index: number) => setForm((current) => ({ ...current, items: current.items.filter((_, itemIndex) => itemIndex !== index) }));

  const totalItemAllocation = useMemo(() => form.items.reduce((sum, item) => sum + Number(item.limitAmount || 0), 0), [form.items]);
  const remainingAllocation = form.totalLimit - totalItemAllocation;
  const budgetValidationError = useMemo(() => {
    if (categories.length === 0) {
      return 'No expense categories are available yet. Please create or load at least one expense category first.';
    }

    if (form.name.trim().length < 2) {
      return 'Budget name must be at least 2 characters long.';
    }

    if (form.month < 1 || form.month > 12) {
      return 'Please choose a month between Jan and Dec.';
    }

    if (form.year < 2000 || form.year > 2100) {
      return 'Year must be between 2000 and 2100.';
    }

    if (form.totalLimit <= 0) {
      return 'Total limit must be greater than zero.';
    }

    if (form.alertThresholdPercent < 1 || form.alertThresholdPercent > 100) {
      return 'Alert threshold must be between 1 and 100.';
    }

    if (form.items.length === 0) {
      return 'Add at least one budget item.';
    }

    if (form.items.some((item) => !item.categoryId)) {
      return 'Each budget item needs a category.';
    }

    if (form.items.some((item) => item.limitAmount <= 0)) {
      return 'Each budget item limit must be greater than zero.';
    }

    const uniqueCategoryCount = new Set(form.items.map((item) => item.categoryId)).size;
    if (uniqueCategoryCount !== form.items.length) {
      return 'Duplicate categories are not allowed in a budget.';
    }

    if (totalItemAllocation > form.totalLimit) {
      return `Budget items total ${formatCurrency(totalItemAllocation)} which is above the overall limit ${formatCurrency(form.totalLimit)}. Reduce item limits or increase the total limit.`;
    }

    return null;
  }, [categories.length, form.alertThresholdPercent, form.items, form.month, form.name, form.totalLimit, form.year, totalItemAllocation]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (budgetValidationError) {
      setError(budgetValidationError);
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      if (selected) await api.put(`/api/budgets/${selected.id}`, form);
      else await api.post('/api/budgets', form);
      setSelected(null);
      const next = createEmptyForm();
      next.items = [{ categoryId: categories[0]?.id ?? '', limitAmount: 5000 }];
      setForm(next);
      await load();
    } catch (submitError) {
      setError(flattenErrors(submitError));
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <LoadingPanel label="Loading budget allocation…" />;

  return (
    <div className="space-y-6">
      <SectionTitle eyebrow="Budget Allocation" title="Planned vs actual spending" action={<Badge tone="amber">{budgetHealth.length} live budgets</Badge>} />
      {error && <GlassCard className="border-rose-200 bg-rose-50 text-rose-700">{error}</GlassCard>}
      <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <div className="space-y-6">
          {budgets.length === 0 ? (
            <EmptyState title="No budgets created" description="Set a monthly envelope with category limits and alert thresholds." />
          ) : budgets.map((budget) => {
            const status = budgetHealth.find((item) => item.budgetId === budget.id);
            const tone = status?.isOverBudget ? 'rose' : status?.thresholdReached ? 'amber' : 'emerald';
            return (
              <GlassCard key={budget.id}>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">{monthName(budget.month)} {budget.year}</p>
                    <h3 className="mt-2 text-3xl font-black">{budget.name}</h3>
                    <p className="mt-3 text-sm text-slate-600">{formatCurrency(budget.totalSpent)} spent of {formatCurrency(budget.totalLimit)}</p>
                  </div>
                  <Badge tone={tone}>{status?.isOverBudget ? 'Critical' : status?.thresholdReached ? 'High risk' : 'Low risk'}</Badge>
                </div>

                <div className="mt-5 h-3 overflow-hidden rounded-full bg-slate-200">
                  <div className={`h-full rounded-full ${status?.isOverBudget ? 'bg-rose-400' : status?.thresholdReached ? 'bg-amber-400' : 'bg-emerald-400'}`} style={{ width: `${Math.min(budget.usagePercent, 100)}%` }} />
                </div>

                <div className="mt-5 space-y-3">
                  {budget.items.map((item) => (
                    <div key={`${budget.id}-${item.categoryId}`} className="rounded-3xl border border-slate-200 bg-slate-50/5 p-4">
                      <div className="flex items-center justify-between gap-4">
                        <div>
                          <p className="text-sm font-black uppercase tracking-[0.3em] text-slate-600">{item.categoryName}</p>
                          <p className="mt-1 text-xs text-slate-400">{formatCurrency(item.spentAmount)} of {formatCurrency(item.limitAmount)}</p>
                        </div>
                        <Sparkles className="h-5 w-5 text-violet-300" />
                      </div>
                      <div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-200"><div className="h-full rounded-full bg-gradient-to-r from-indigo-500 to-rose-500" style={{ width: `${Math.min(item.usagePercent, 100)}%` }} /></div>
                      <div className="mt-3 flex items-center justify-between text-xs text-slate-400">
                        <span>{item.usagePercent.toFixed(0)}% used</span>
                        <span>Remaining {formatCurrency(item.remainingAmount)}</span>
                      </div>
                    </div>
                  ))}
                </div>

                <div className="mt-5 flex items-center gap-5 text-xs font-black uppercase tracking-[0.3em]">
                  <button onClick={() => setSelected(budget)} className="inline-flex items-center gap-2 text-slate-700"><Pencil className="h-4 w-4" />Edit</button>
                  <button onClick={() => void remove(budget.id)} className="inline-flex items-center gap-2 text-rose-200"><Trash2 className="h-4 w-4" />Delete</button>
                </div>
              </GlassCard>
            );
          })}
        </div>

        <GlassCard>
          <SectionTitle eyebrow="Budget editor" title={selected ? 'Update budget' : 'Create budget'} action={<button onClick={() => setSelected(null)} className="rounded-full bg-white/10 px-3 py-1 text-xs font-black uppercase tracking-[0.3em] text-slate-600"><Plus className="inline h-3 w-3" /> New</button>} />
          <form onSubmit={submit} className="space-y-4">
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Name</span><input value={form.name} onChange={(e) => setForm((current) => ({ ...current, name: e.target.value }))} placeholder="March household budget" required className="app-form-control w-full" /></label>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Month</span><select value={form.month} onChange={(e) => setForm((current) => ({ ...current, month: Number(e.target.value) }))} className="app-form-control app-form-control--select w-full">{monthOptions.map((month) => <option key={month.value} value={month.value}>{month.label}</option>)}</select></label>
              <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Year</span><NumberInput min="2024" max="2100" value={form.year} onValueChange={(year) => setForm((current) => ({ ...current, year }))} className="app-form-control w-full" /></label>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Total limit</span><NumberInput min="1" step="0.01" value={form.totalLimit} onValueChange={(totalLimit) => setForm((current) => ({ ...current, totalLimit }))} className="app-form-control w-full" /></label>
              <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Alert threshold</span><NumberInput min="1" max="100" value={form.alertThresholdPercent} onValueChange={(alertThresholdPercent) => setForm((current) => ({ ...current, alertThresholdPercent }))} className="app-form-control w-full" /></label>
            </div>

            <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <div className="mb-4 flex items-center justify-between gap-4">
                <div>
                  <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Budget items</p>
                  <p className={`mt-1 text-sm ${remainingAllocation < 0 ? 'text-rose-600' : 'text-slate-600'}`}>
                    Allocated: {formatCurrency(totalItemAllocation)} / {formatCurrency(form.totalLimit)}
                    <span className="ml-2">{remainingAllocation >= 0 ? `${formatCurrency(remainingAllocation)} left to allocate` : `${formatCurrency(Math.abs(remainingAllocation))} over the limit`}</span>
                  </p>
                </div>
                <button type="button" onClick={addItem} className="rounded-full bg-white/10 px-3 py-1 text-xs font-black uppercase tracking-[0.3em] text-slate-600">Add row</button>
              </div>
              <div className="space-y-3">
                {form.items.map((item, index) => (
                  <div key={`${index}-${item.categoryId}`} className="grid gap-3 md:grid-cols-[1fr_140px_52px]">
                    <select value={item.categoryId} onChange={(e) => updateItem(index, 'categoryId', e.target.value)} className="app-form-control app-form-control--select">{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select>
                    <NumberInput min="1" step="0.01" value={item.limitAmount} blankWhenZero placeholder="0" onValueChange={(limitAmount) => updateItem(index, 'limitAmount', limitAmount)} className="app-form-control" />
                    <button type="button" onClick={() => removeItem(index)} disabled={form.items.length === 1} className="rounded-3xl border border-slate-200 bg-white text-rose-200 disabled:opacity-40"><Trash2 className="mx-auto h-4 w-4" /></button>
                  </div>
                ))}
              </div>
            </div>

            {budgetValidationError && <div className="rounded-3xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">{budgetValidationError}</div>}

            <button disabled={submitting || Boolean(budgetValidationError)} className="w-full rounded-3xl bg-white px-5 py-3 text-sm font-black uppercase tracking-[0.35em] text-slate-950 disabled:cursor-not-allowed disabled:opacity-60">{submitting ? 'Saving…' : selected ? 'Update Budget' : 'Create Budget'}</button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
