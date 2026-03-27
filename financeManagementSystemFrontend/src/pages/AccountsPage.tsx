import { useEffect, useMemo, useState } from 'react';
import { Banknote, CreditCard, Pencil, Plus, Trash2, Wallet, Zap } from 'lucide-react';
import { Badge, EmptyState, GlassCard, LoadingPanel, NumberInput, SectionTitle } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { accountTypeLabels, flattenErrors, formatCurrency } from '../lib/utils';
import type { AccountResponse } from '../types/api';

const emptyForm = { name: '', type: 2, currency: 'INR', openingBalance: 0 };

function AccountIcon({ type }: { type: number }) {
  if (type === 2) return <CreditCard className="h-5 w-5" />;
  if (type === 1 || type === 4) return <Wallet className="h-5 w-5" />;
  if (type === 5) return <Zap className="h-5 w-5" />;
  return <Banknote className="h-5 w-5" />;
}

export function AccountsPage() {
  const { api } = useAuth();
  const [accounts, setAccounts] = useState<AccountResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<AccountResponse | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [submitting, setSubmitting] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await api.get<AccountResponse[]>('/api/accounts');
      setAccounts(items);
    } catch (loadError) {
      setError(flattenErrors(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  useEffect(() => {
    if (!selected) {
      setForm(emptyForm);
      return;
    }

    setForm({
      name: selected.name,
      type: selected.type,
      currency: selected.currency,
      openingBalance: selected.openingBalance
    });
  }, [selected]);

  const totalBalance = useMemo(() => accounts.reduce((sum, account) => sum + account.currentBalance, 0), [accounts]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      if (selected) {
        await api.put(`/api/accounts/${selected.id}`, form);
      } else {
        await api.post('/api/accounts', form);
      }
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
      await api.delete(`/api/accounts/${id}`);
      await load();
    } catch (deleteError) {
      setError(flattenErrors(deleteError));
    }
  };

  if (loading) return <LoadingPanel label="Loading capital accounts…" />;

  return (
    <div className="space-y-6">
      <SectionTitle eyebrow="Capital Accounts" title="Where your money lives" action={<Badge tone="emerald">{formatCurrency(totalBalance)}</Badge>} />

      {error && <GlassCard className="text-rose-200">{error}</GlassCard>}

      <div className="grid gap-6 xl:grid-cols-[1.3fr_0.7fr]">
        <div>
          {accounts.length === 0 ? (
            <EmptyState title="No accounts created" description="Add your first bank, wallet, cash, or investment account to activate FinPilot." />
          ) : (
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
              {accounts.map((account) => (
                <GlassCard key={account.id} className="min-h-[220px]">
                  <div className="flex items-start justify-between gap-3">
                    <div className="rounded-3xl bg-white/10 p-4 text-violet-200">
                      <AccountIcon type={account.type} />
                    </div>
                    <Badge tone="slate">{accountTypeLabels[account.type]}</Badge>
                  </div>
                  <p className="mt-8 text-xs font-black uppercase tracking-[0.35em] text-slate-400">{account.currency}</p>
                  <h3 className="mt-2 text-2xl font-black">{account.name}</h3>
                  <p className="mt-4 text-4xl font-black tracking-tighter">{formatCurrency(account.currentBalance, account.currency)}</p>
                  <div className="mt-6 flex items-center justify-between gap-3 text-xs font-black uppercase tracking-[0.3em]">
                    <button onClick={() => setSelected(account)} className="inline-flex items-center gap-2 text-slate-700"><Pencil className="h-4 w-4" />Details</button>
                    <button onClick={() => void remove(account.id)} className="inline-flex items-center gap-2 text-rose-200"><Trash2 className="h-4 w-4" />Transfer</button>
                  </div>
                </GlassCard>
              ))}
            </div>
          )}
        </div>

        <GlassCard>
          <SectionTitle eyebrow="Account editor" title={selected ? 'Update account' : 'Add account'} action={<button onClick={() => setSelected(null)} className="rounded-full bg-white/10 px-3 py-1 text-xs font-black uppercase tracking-[0.3em] text-slate-600"><Plus className="inline h-3 w-3" /> New</button>} />
          <form onSubmit={submit} className="space-y-4">
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Name</span>
              <input value={form.name} onChange={(e) => setForm((current) => ({ ...current, name: e.target.value }))} required className="app-form-control w-full" />
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Type</span>
              <select value={form.type} onChange={(e) => setForm((current) => ({ ...current, type: Number(e.target.value) }))} className="app-form-control app-form-control--select w-full">
                {Object.entries(accountTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Currency</span>
              <input value={form.currency} onChange={(e) => setForm((current) => ({ ...current, currency: e.target.value.toUpperCase() }))} required className="app-form-control w-full" />
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Opening balance</span>
              <NumberInput min="0" step="0.01" value={form.openingBalance} blankWhenZero placeholder="0" onValueChange={(openingBalance) => setForm((current) => ({ ...current, openingBalance }))} className="app-form-control w-full" />
            </label>
            <button disabled={submitting} className="w-full rounded-3xl bg-white px-5 py-3 text-sm font-black uppercase tracking-[0.35em] text-slate-950 disabled:opacity-60">{submitting ? 'Saving…' : selected ? 'Update Account' : 'Create Account'}</button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
