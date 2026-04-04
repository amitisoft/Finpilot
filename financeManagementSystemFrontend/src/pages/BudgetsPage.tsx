import { useEffect, useMemo, useState } from 'react';
import { Pencil, Plus, Sparkles, Trash2 } from 'lucide-react';
import { Button } from '../components/Button';
import { FormField } from '../components/FormField';
import { PageIntro } from '../components/PageIntro';
import { Badge, EmptyState, GlassCard, LoadingPanel } from '../components/Ui';
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

function progressTone(status?: BudgetStatusResponse) {
  if (status?.isOverBudget) return 'danger';
  if (status?.thresholdReached) return 'warning';
  return 'success';
}

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
    if (categories.length === 0) return 'No expense categories are available yet. Please create or load at least one expense category first.';
    if (form.name.trim().length < 2) return 'Budget name must be at least 2 characters long.';
    if (form.month < 1 || form.month > 12) return 'Please choose a month between Jan and Dec.';
    if (form.year < 2000 || form.year > 2100) return 'Year must be between 2000 and 2100.';
    if (form.totalLimit <= 0) return 'Total limit must be greater than zero.';
    if (form.alertThresholdPercent < 1 || form.alertThresholdPercent > 100) return 'Alert threshold must be between 1 and 100.';
    if (form.items.length === 0) return 'Add at least one budget item.';
    if (form.items.some((item) => !item.categoryId)) return 'Each budget item needs a category.';
    if (form.items.some((item) => item.limitAmount <= 0)) return 'Each budget item limit must be greater than zero.';
    const uniqueCategoryCount = new Set(form.items.map((item) => item.categoryId)).size;
    if (uniqueCategoryCount !== form.items.length) return 'Duplicate categories are not allowed in a budget.';
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
    <div className="finance-page">
      <PageIntro
        eyebrow="Budget allocation"
        title="Planned vs actual spending"
        description="Set spending envelopes by month and category so the dashboard can show what is on plan, what is drifting, and what needs intervention."
        aside={<Badge tone="amber">{budgetHealth.length} live budgets</Badge>}
      />

      {error ? <GlassCard className="finance-page__feedback">{error}</GlassCard> : null}

      <div className="finance-page__grid finance-page__grid--wide-right">
        <div className="finance-page__stack">
          {budgets.length === 0 ? (
            <div className="finance-page__empty">
              <EmptyState title="No budgets created" description="Set a monthly envelope with category limits and alert thresholds." />
            </div>
          ) : budgets.map((budget) => {
            const status = budgetHealth.find((item) => item.budgetId === budget.id);
            const tone = status?.isOverBudget ? 'rose' : status?.thresholdReached ? 'amber' : 'emerald';
            const progressClass = `finance-page__progress-fill finance-page__progress-fill--${progressTone(status)}`;
            return (
              <GlassCard key={budget.id} className="finance-page__card">
                <div className="finance-page__card-top">
                  <div>
                    <p className="finance-page__card-eyebrow">{monthName(budget.month)} {budget.year}</p>
                    <h3 className="finance-page__card-title">{budget.name}</h3>
                    <p className="finance-page__card-subtitle">{formatCurrency(budget.totalSpent)} spent of {formatCurrency(budget.totalLimit)}</p>
                  </div>
                  <Badge tone={tone}>{status?.isOverBudget ? 'Critical' : status?.thresholdReached ? 'High risk' : 'Low risk'}</Badge>
                </div>

                <div className="finance-page__progress">
                  <div className={progressClass} style={{ width: `${Math.min(budget.usagePercent, 100)}%` }} />
                </div>

                <div className="finance-page__item-list">
                  {budget.items.map((item) => (
                    <div key={`${budget.id}-${item.categoryId}`} className="finance-page__item-panel">
                      <div className="finance-page__item-header">
                        <div>
                          <p className="finance-page__card-eyebrow">{item.categoryName}</p>
                          <p className="finance-page__helper-text">{formatCurrency(item.spentAmount)} of {formatCurrency(item.limitAmount)}</p>
                        </div>
                        <Sparkles size={18} />
                      </div>
                      <div className="finance-page__progress" style={{ marginTop: '16px' }}>
                        <div className="finance-page__progress-fill" style={{ width: `${Math.min(item.usagePercent, 100)}%` }} />
                      </div>
                      <div className="finance-page__item-stats">
                        <span>{item.usagePercent.toFixed(0)}% used</span>
                        <span>Remaining {formatCurrency(item.remainingAmount)}</span>
                      </div>
                    </div>
                  ))}
                </div>

                <div className="finance-page__card-actions">
                  <Button variant="secondary" size="sm" onClick={() => setSelected(budget)} iconLeading={<Pencil size={14} />}>Edit</Button>
                  <Button variant="ghost" size="sm" onClick={() => void remove(budget.id)} iconLeading={<Trash2 size={14} />} className="finance-page__danger-button">Delete</Button>
                </div>
              </GlassCard>
            );
          })}
        </div>

        <GlassCard className="finance-page__editor">
          <PageIntro
            eyebrow="Budget editor"
            title={selected ? 'Update budget' : 'Create budget'}
            description="Allocate limits at the category level and keep the total cap realistic enough to become a useful guardrail."
            aside={<Button variant="secondary" size="sm" onClick={() => setSelected(null)} iconLeading={<Plus size={14} />}>New</Button>}
          />

          <form onSubmit={submit} className="finance-page__form">
            <FormField label="Name">
              <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="March household budget" required className="app-form-control" />
            </FormField>

            <div className="finance-page__split">
              <FormField label="Month">
                <select value={form.month} onChange={(event) => setForm((current) => ({ ...current, month: Number(event.target.value) }))} className="app-form-control app-form-control--select">
                  {monthOptions.map((month) => <option key={month.value} value={month.value}>{month.label}</option>)}
                </select>
              </FormField>
              <FormField label="Year">
                <input value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: Number(event.target.value) || now.getFullYear() }))} type="number" min="2024" max="2100" className="app-form-control" />
              </FormField>
            </div>

            <div className="finance-page__split">
              <FormField label="Total limit">
                <input value={form.totalLimit} onChange={(event) => setForm((current) => ({ ...current, totalLimit: Number(event.target.value) || 0 }))} type="number" min="1" step="0.01" className="app-form-control" />
              </FormField>
              <FormField label="Alert threshold">
                <input value={form.alertThresholdPercent} onChange={(event) => setForm((current) => ({ ...current, alertThresholdPercent: Number(event.target.value) || 0 }))} type="number" min="1" max="100" className="app-form-control" />
              </FormField>
            </div>

            <div className="finance-page__item-panel">
              <div className="finance-page__item-header">
                <div>
                  <p className="finance-page__card-eyebrow">Budget items</p>
                  <p className="finance-page__helper-text">
                    Allocated {formatCurrency(totalItemAllocation)} / {formatCurrency(form.totalLimit)} • {remainingAllocation >= 0 ? `${formatCurrency(remainingAllocation)} left` : `${formatCurrency(Math.abs(remainingAllocation))} over limit`}
                  </p>
                </div>
                <Button variant="secondary" size="sm" onClick={addItem}>Add row</Button>
              </div>

              <div className="finance-page__item-list">
                {form.items.map((item, index) => (
                  <div key={`${index}-${item.categoryId}`} className="finance-page__item-grid">
                    <select value={item.categoryId} onChange={(event) => updateItem(index, 'categoryId', event.target.value)} className="app-form-control app-form-control--select">
                      {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                    </select>
                    <input value={item.limitAmount} onChange={(event) => updateItem(index, 'limitAmount', Number(event.target.value) || 0)} type="number" min="1" step="0.01" className="app-form-control" />
                    <Button variant="ghost" onClick={() => removeItem(index)} disabled={form.items.length === 1} iconLeading={<Trash2 size={14} />} className="finance-page__danger-button" />
                  </div>
                ))}
              </div>
            </div>

            {budgetValidationError ? <div className="finance-page__status-banner"><span>{budgetValidationError}</span></div> : null}

            <Button type="submit" fullWidth size="lg" disabled={submitting || Boolean(budgetValidationError)}>
              {submitting ? 'Saving…' : selected ? 'Update budget' : 'Create budget'}
            </Button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
