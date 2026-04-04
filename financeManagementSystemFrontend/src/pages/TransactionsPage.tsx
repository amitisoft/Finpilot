import { useEffect, useMemo, useState } from 'react';
import { AlertTriangle, Pencil, Plus, Search, Sparkles, Trash2 } from 'lucide-react';
import { Button } from '../components/Button';
import { FormField } from '../components/FormField';
import { PageIntro } from '../components/PageIntro';
import { Badge, EmptyState, GlassCard, LoadingPanel } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { flattenErrors, formatCurrency, formatDate, transactionTypeLabels } from '../lib/utils';
import type { AccountResponse, AgentResultResponse, CategoryResponse, TransactionResponse } from '../types/api';

const emptyForm = {
  accountId: '',
  categoryId: '',
  type: 2,
  amount: 0,
  description: '',
  transactionDate: new Date().toISOString().slice(0, 16),
  merchant: '',
  notes: ''
};

export function TransactionsPage() {
  const { api } = useAuth();
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [transactions, setTransactions] = useState<TransactionResponse[]>([]);
  const [anomalies, setAnomalies] = useState<Record<string, AgentResultResponse>>({});
  const [selected, setSelected] = useState<TransactionResponse | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [accountsData, categoriesData, transactionData, anomalyData] = await Promise.all([
        api.get<AccountResponse[]>('/api/accounts'),
        api.get<CategoryResponse[]>('/api/categories'),
        api.get<TransactionResponse[]>('/api/transactions'),
        api.get<AgentResultResponse[]>('/api/agents/results?agent=1')
      ]);
      setAccounts(accountsData);
      setCategories(categoriesData);
      setTransactions(transactionData);
      setAnomalies(Object.fromEntries(anomalyData.filter((item) => item.sourceEntityId).map((item) => [item.sourceEntityId as string, item])));
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
      accountId: selected.accountId,
      categoryId: selected.categoryId,
      type: selected.type,
      amount: selected.amount,
      description: selected.description,
      transactionDate: new Date(selected.transactionDate).toISOString().slice(0, 16),
      merchant: selected.merchant ?? '',
      notes: selected.notes ?? ''
    });
  }, [selected]);

  useEffect(() => {
    const filtered = categories.filter((category) => category.type == form.type);
    if (filtered.length > 0 && !filtered.some((category) => category.id == form.categoryId)) {
      setForm((current) => ({ ...current, categoryId: filtered[0].id }));
    }
  }, [categories, form.type]);

  useEffect(() => {
    if (!form.accountId && accounts.length > 0) {
      setForm((current) => ({ ...current, accountId: accounts[0].id }));
    }
  }, [accounts, form.accountId]);

  const filteredTransactions = useMemo(
    () => transactions.filter((item) => [item.description, item.categoryName, item.accountName, item.merchant ?? ''].join(' ').toLowerCase().includes(query.toLowerCase())),
    [transactions, query]
  );
  const filteredCategories = useMemo(() => categories.filter((category) => category.type == form.type), [categories, form.type]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const payload = { ...form, transactionDate: new Date(form.transactionDate).toISOString() };
      if (selected) await api.put(`/api/transactions/${selected.id}`, payload);
      else await api.post('/api/transactions', payload);
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
      await api.delete(`/api/transactions/${id}`);
      await load();
    } catch (deleteError) {
      setError(flattenErrors(deleteError));
    }
  };

  if (loading) return <LoadingPanel label="Loading unified ledger…" />;

  return (
    <div className="finance-page">
      <PageIntro
        eyebrow="Unified ledger"
        title="Transactions, anomalies, and movement"
        description="Search, review, and correct money movement in one place. This page also surfaces anomaly screening tied to the transaction stream."
        aside={<Badge tone="slate">{transactions.length} entries</Badge>}
      />

      {error ? <GlassCard className="finance-page__feedback">{error}</GlassCard> : null}

      <div className="finance-page__grid finance-page__grid--wide-right">
        <GlassCard>
          <div className="finance-page__toolbar">
            <label className="finance-page__search">
              <Search className="finance-page__search-icon" size={16} />
              <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search merchant, category, account…" className="finance-page__search-input" />
            </label>
            <Badge tone="violet">Ledger table</Badge>
          </div>

          {filteredTransactions.length == 0 ? (
            <div className="finance-page__empty">
              <EmptyState title="No transactions yet" description="Add income and expense activity to populate the unified ledger." />
            </div>
          ) : (
            <div className="finance-page__table">
              <div className="finance-page__table-head">
                <div className="finance-page__table-head-grid finance-page__table-head-grid--transactions">
                  <span>Status</span>
                  <span>Entity</span>
                  <span>Category</span>
                  <span>Account</span>
                  <span>Magnitude</span>
                  <span>Actions</span>
                </div>
              </div>
              <div>
                {filteredTransactions.map((item) => {
                  const anomaly = anomalies[item.id];
                  const flagged = Boolean(anomaly && anomaly.severity !== 'none');
                  return (
                    <div key={item.id} className={`finance-page__table-row${flagged ? ' finance-page__table-row--flagged' : ''}`}>
                      <div className="finance-page__table-grid finance-page__table-grid--transactions">
                        <div className="finance-page__table-cell">
                          {flagged ? <AlertTriangle size={16} color="#c33e4d" /> : <Sparkles size={16} color="#12805c" />}
                          <span style={{ marginLeft: 8 }}><Badge tone={flagged ? 'rose' : 'emerald'}>{flagged ? anomaly?.severity : transactionTypeLabels[item.type]}</Badge></span>
                        </div>
                        <div className="finance-page__table-cell">
                          <div>
                            <p className="finance-page__table-title">{item.description}</p>
                            <p className="finance-page__table-meta">{item.merchant || 'No merchant'} • {formatDate(item.transactionDate)}</p>
                          </div>
                        </div>
                        <div className="finance-page__table-cell"><Badge tone={item.type === 1 ? 'emerald' : 'rose'}>{item.categoryName}</Badge></div>
                        <div className="finance-page__table-cell">{item.accountName}</div>
                        <div className="finance-page__table-cell"><strong>{formatCurrency(item.amount)}</strong></div>
                        <div className="finance-page__table-actions">
                          <Button variant="ghost" size="sm" onClick={() => setSelected(item)} iconLeading={<Pencil size={14} />}>Edit</Button>
                          <Button variant="ghost" size="sm" onClick={() => void remove(item.id)} iconLeading={<Trash2 size={14} />} className="finance-page__danger-button">Delete</Button>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </GlassCard>

        <GlassCard className="finance-page__editor">
          <PageIntro
            eyebrow="Transaction editor"
            title={selected ? 'Update movement' : 'Add movement'}
            description="Choose type, account, category, and a clean description so insights stay accurate."
            aside={<Button variant="secondary" size="sm" onClick={() => setSelected(null)} iconLeading={<Plus size={14} />}>New</Button>}
          />

          <form onSubmit={submit} className="finance-page__form">
            <FormField label="Type">
              <select value={form.type} onChange={(event) => setForm((current) => ({ ...current, type: Number(event.target.value) }))} className="app-form-control app-form-control--select">
                <option value={1}>Income</option>
                <option value={2}>Expense</option>
              </select>
            </FormField>

            <FormField label="Account">
              <select value={form.accountId} onChange={(event) => setForm((current) => ({ ...current, accountId: event.target.value }))} className="app-form-control app-form-control--select">
                {accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}
              </select>
            </FormField>

            <FormField label="Category">
              <select value={form.categoryId} onChange={(event) => setForm((current) => ({ ...current, categoryId: event.target.value }))} className="app-form-control app-form-control--select">
                {filteredCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
              </select>
            </FormField>

            <FormField label="Amount">
              <input value={form.amount} onChange={(event) => setForm((current) => ({ ...current, amount: Number(event.target.value) || 0 }))} type="number" min="0.01" step="0.01" className="app-form-control" />
            </FormField>

            <FormField label="Description">
              <input value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} required className="app-form-control" />
            </FormField>

            <FormField label="Date">
              <input type="datetime-local" value={form.transactionDate} onChange={(event) => setForm((current) => ({ ...current, transactionDate: event.target.value }))} className="app-form-control" />
            </FormField>

            <FormField label="Merchant">
              <input value={form.merchant} onChange={(event) => setForm((current) => ({ ...current, merchant: event.target.value }))} className="app-form-control" />
            </FormField>

            <FormField label="Notes">
              <textarea value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} className="app-form-control" style={{ minHeight: 110, resize: 'vertical' }} />
            </FormField>

            <Button type="submit" fullWidth size="lg" disabled={submitting}>
              {submitting ? 'Saving…' : selected ? 'Update transaction' : 'Create transaction'}
            </Button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
