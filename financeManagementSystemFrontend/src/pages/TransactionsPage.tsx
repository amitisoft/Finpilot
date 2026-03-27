import { useEffect, useMemo, useState } from 'react';
import { AlertTriangle, Pencil, Plus, Search, Sparkles, Trash2 } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, NumberInput, SectionTitle } from '../components/Ui';
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
    if (!selected) return void setForm(emptyForm);
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
    const filteredCategories = categories.filter((category) => category.type === form.type);
    if (filteredCategories.length > 0 && !filteredCategories.some((category) => category.id === form.categoryId)) {
      setForm((current) => ({ ...current, categoryId: filteredCategories[0].id }));
    }
  }, [categories, form.type]);

  useEffect(() => {
    if (!form.accountId && accounts.length > 0) {
      setForm((current) => ({ ...current, accountId: accounts[0].id }));
    }
  }, [accounts, form.accountId]);

  const filteredTransactions = useMemo(() => transactions.filter((item) => [item.description, item.categoryName, item.accountName, item.merchant ?? ''].join(' ').toLowerCase().includes(query.toLowerCase())), [transactions, query]);
  const filteredCategories = useMemo(() => categories.filter((category) => category.type === form.type), [categories, form.type]);

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
    <div className="space-y-6">
      <SectionTitle eyebrow="Unified Ledger" title="Transactions, anomalies, and movement" action={<Badge tone="slate">{transactions.length} entries</Badge>} />
      {error && <GlassCard className="text-rose-200">{error}</GlassCard>}
      <div className="grid gap-6 xl:grid-cols-[1.3fr_0.7fr]">
        <GlassCard>
          <div className="mb-5 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <label className="relative block flex-1">
              <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
              <input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search merchant, category, account…" className="w-full rounded-3xl border border-slate-200 bg-white py-3 pl-11 pr-4" />
            </label>
            <Badge tone="violet">Ledger table</Badge>
          </div>

          {filteredTransactions.length === 0 ? (
            <EmptyState title="No transactions yet" description="Add income and expense activity to populate the unified ledger." />
          ) : (
            <div className="overflow-hidden rounded-[1.75rem] border border-slate-200">
              <div className="grid grid-cols-[110px_1.4fr_1fr_1fr_120px_100px] gap-3 bg-slate-50 px-4 py-3 text-[11px] font-black uppercase tracking-[0.3em] text-slate-400">
                <span>Status</span><span>Entity</span><span>Category</span><span>Account</span><span>Magnitude</span><span>Actions</span>
              </div>
              <div className="divide-y divide-white/5">
                {filteredTransactions.map((item) => {
                  const anomaly = anomalies[item.id];
                  const flagged = Boolean(anomaly && anomaly.severity !== 'none');
                  return (
                    <div key={item.id} className={`grid grid-cols-[110px_1.4fr_1fr_1fr_120px_100px] gap-3 px-4 py-4 text-sm ${flagged ? 'bg-rose-500/10 animate-pulse-soft' : 'bg-white'}`}>
                      <div className="flex items-center gap-2">
                        {flagged ? <AlertTriangle className="h-4 w-4 text-rose-300" /> : <Sparkles className="h-4 w-4 text-emerald-300" />}
                        <span className={`rounded-full px-3 py-1 text-[11px] font-black uppercase tracking-[0.3em] ${flagged ? 'bg-rose-500/20 text-rose-200' : 'bg-emerald-500/15 text-emerald-200'}`}>{flagged ? anomaly?.severity : transactionTypeLabels[item.type]}</span>
                      </div>
                      <div>
                        <p className="font-black text-slate-900">{item.description}</p>
                        <p className="mt-1 text-xs text-slate-400">{item.merchant || 'No merchant'} • {formatDate(item.transactionDate)}</p>
                      </div>
                      <div className="flex items-center"><Badge tone={item.type === 1 ? 'emerald' : 'rose'}>{item.categoryName}</Badge></div>
                      <div className="flex items-center text-slate-600">{item.accountName}</div>
                      <div className={`flex items-center font-black ${item.type === 1 ? 'text-emerald-300' : 'text-rose-300'}`}>{formatCurrency(item.amount)}</div>
                      <div className="flex items-center gap-3 text-slate-600">
                        <button onClick={() => setSelected(item)}><Pencil className="h-4 w-4" /></button>
                        <button onClick={() => void remove(item.id)} className="text-rose-300"><Trash2 className="h-4 w-4" /></button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </GlassCard>

        <GlassCard>
          <SectionTitle eyebrow="Transaction editor" title={selected ? 'Update movement' : 'Add movement'} action={<button onClick={() => setSelected(null)} className="rounded-full bg-white/10 px-3 py-1 text-xs font-black uppercase tracking-[0.3em] text-slate-600"><Plus className="inline h-3 w-3" /> New</button>} />
          <form onSubmit={submit} className="space-y-4">
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Type</span><select value={form.type} onChange={(e) => setForm((current) => ({ ...current, type: Number(e.target.value) }))} className="app-form-control app-form-control--select w-full"><option value={1}>Income</option><option value={2}>Expense</option></select></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Account</span><select value={form.accountId} onChange={(e) => setForm((current) => ({ ...current, accountId: e.target.value }))} className="app-form-control app-form-control--select w-full">{accounts.map((account) => <option key={account.id} value={account.id}>{account.name}</option>)}</select></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Category</span><select value={form.categoryId} onChange={(e) => setForm((current) => ({ ...current, categoryId: e.target.value }))} className="app-form-control app-form-control--select w-full">{filteredCategories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Amount</span><NumberInput min="0.01" step="0.01" value={form.amount} blankWhenZero placeholder="0" onValueChange={(amount) => setForm((current) => ({ ...current, amount }))} className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Description</span><input value={form.description} onChange={(e) => setForm((current) => ({ ...current, description: e.target.value }))} required className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Date</span><input type="datetime-local" value={form.transactionDate} onChange={(e) => setForm((current) => ({ ...current, transactionDate: e.target.value }))} className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Merchant</span><input value={form.merchant} onChange={(e) => setForm((current) => ({ ...current, merchant: e.target.value }))} className="app-form-control w-full" /></label>
            <label className="block"><span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Notes</span><textarea value={form.notes} onChange={(e) => setForm((current) => ({ ...current, notes: e.target.value }))} className="min-h-[100px] w-full rounded-3xl border border-slate-200 bg-white px-4 py-3" /></label>
            <button disabled={submitting} className="w-full rounded-3xl bg-white px-5 py-3 text-sm font-black uppercase tracking-[0.35em] text-slate-950 disabled:opacity-60">{submitting ? 'Saving…' : selected ? 'Update Transaction' : 'Create Transaction'}</button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}

