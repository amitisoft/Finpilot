import { useEffect, useMemo, useState } from 'react';
import { Banknote, CreditCard, Pencil, Plus, Trash2, Wallet, Zap } from 'lucide-react';
import { Button } from '../components/Button';
import { FormField } from '../components/FormField';
import { PageIntro } from '../components/PageIntro';
import { Badge, EmptyState, GlassCard, LoadingPanel } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { accountTypeLabels, flattenErrors, formatCurrency } from '../lib/utils';
import type { AccountResponse } from '../types/api';

const emptyForm = { name: '', type: 2, currency: 'INR', openingBalance: 0 };

function AccountIcon({ type }: { type: number }) {
  if (type === 2) return <CreditCard size={20} />;
  if (type === 1 || type === 4) return <Wallet size={20} />;
  if (type === 5) return <Zap size={20} />;
  return <Banknote size={20} />;
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
      setAccounts(await api.get<AccountResponse[]>('/api/accounts'));
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
    <div className="finance-page">
      <PageIntro
        eyebrow="Capital accounts"
        title="Where your money lives"
        description="Keep banks, wallets, cash, and investment balances structured in one place so the rest of the workspace has a reliable base."
        aside={<Badge tone="emerald">{formatCurrency(totalBalance)}</Badge>}
      />

      {error ? <GlassCard className="finance-page__feedback">{error}</GlassCard> : null}

      <div className="finance-page__grid">
        <div className="finance-page__stack">
          {accounts.length === 0 ? (
            <div className="finance-page__empty">
              <EmptyState title="No accounts created" description="Add your first bank, wallet, cash, or investment account to activate FinPilot." />
            </div>
          ) : (
            <div className="finance-page__cards finance-page__cards--compact">
              {accounts.map((account) => (
                <GlassCard key={account.id} className="finance-page__card">
                  <div className="finance-page__card-header">
                    <span className="finance-page__card-icon"><AccountIcon type={account.type} /></span>
                    <Badge tone="slate">{accountTypeLabels[account.type]}</Badge>
                  </div>
                  <p className="finance-page__card-eyebrow">{account.currency}</p>
                  <h3 className="finance-page__card-title">{account.name}</h3>
                  <p className="finance-page__card-value">{formatCurrency(account.currentBalance, account.currency)}</p>
                  <p className="finance-page__card-note">Opening balance {formatCurrency(account.openingBalance, account.currency)}</p>
                  <div className="finance-page__card-actions">
                    <Button variant="secondary" size="sm" onClick={() => setSelected(account)} iconLeading={<Pencil size={14} />}>Edit</Button>
                    <Button variant="ghost" size="sm" onClick={() => void remove(account.id)} iconLeading={<Trash2 size={14} />} className="finance-page__danger-button">Delete</Button>
                  </div>
                </GlassCard>
              ))}
            </div>
          )}
        </div>

        <GlassCard className="finance-page__editor">
          <PageIntro
            eyebrow="Account editor"
            title={selected ? 'Update account' : 'Add account'}
            description="Use consistent account labels and currencies so reports and forecasts stay clean."
            aside={<Button variant="secondary" size="sm" onClick={() => setSelected(null)} iconLeading={<Plus size={14} />}>New</Button>}
          />

          <form onSubmit={submit} className="finance-page__form">
            <FormField label="Name">
              <input value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required className="app-form-control" />
            </FormField>

            <FormField label="Type">
              <select value={form.type} onChange={(event) => setForm((current) => ({ ...current, type: Number(event.target.value) }))} className="app-form-control app-form-control--select">
                {Object.entries(accountTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </FormField>

            <FormField label="Currency">
              <input value={form.currency} onChange={(event) => setForm((current) => ({ ...current, currency: event.target.value.toUpperCase() }))} required className="app-form-control" />
            </FormField>

            <FormField label="Opening balance">
              <input value={form.openingBalance} onChange={(event) => setForm((current) => ({ ...current, openingBalance: Number(event.target.value) || 0 }))} type="number" min="0" step="0.01" className="app-form-control" />
            </FormField>

            <Button type="submit" fullWidth size="lg" disabled={submitting}>
              {submitting ? 'Saving…' : selected ? 'Update account' : 'Create account'}
            </Button>
          </form>
        </GlassCard>
      </div>
    </div>
  );
}
