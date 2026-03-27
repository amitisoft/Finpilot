import { cn } from '../lib/utils';
import { useEffect, useState } from 'react';
import type { InputHTMLAttributes, ReactNode } from 'react';

export function GlassCard({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={cn('animate-fade-in-up rounded-[2rem] border border-slate-200 bg-white p-5 shadow-sm', className)}>{children}</div>;
}

export function SectionTitle({ eyebrow, title, action }: { eyebrow: string; title: string; action?: ReactNode }) {
  return (
    <div className="mb-5 flex items-end justify-between gap-4">
      <div>
        <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">{eyebrow}</p>
        <h3 className="mt-2 text-2xl font-black tracking-tight text-slate-900">{title}</h3>
      </div>
      {action}
    </div>
  );
}

export function MetricCard({ label, value, tone = 'default' }: { label: string; value: string; tone?: 'default' | 'positive' | 'negative' }) {
  const toneClass = tone === 'positive' ? 'text-emerald-600' : tone === 'negative' ? 'text-rose-600' : 'text-slate-900';
  return (
    <GlassCard className="min-h-[150px]">
      <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">{label}</p>
      <p className={cn('mt-6 text-4xl font-black tracking-tighter', toneClass)}>{value}</p>
    </GlassCard>
  );
}

export function Badge({ children, tone = 'slate' }: { children: ReactNode; tone?: 'slate' | 'emerald' | 'rose' | 'amber' | 'violet' }) {
  const toneClass = {
    slate: 'bg-slate-100 text-slate-700 border border-slate-200',
    emerald: 'bg-emerald-50 text-emerald-700 border border-emerald-200',
    rose: 'bg-rose-50 text-rose-700 border border-rose-200',
    amber: 'bg-amber-50 text-amber-700 border border-amber-200',
    violet: 'bg-violet-50 text-violet-700 border border-violet-200'
  }[tone];

  return <span className={cn('inline-flex rounded-full px-3 py-1 text-[11px] font-black uppercase tracking-[0.3em]', toneClass)}>{children}</span>;
}

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <GlassCard className="border-dashed text-center">
      <p className="text-sm font-black uppercase tracking-[0.35em] text-slate-500">No data yet</p>
      <h4 className="mt-3 text-xl font-black text-slate-900">{title}</h4>
      <p className="mx-auto mt-2 max-w-xl text-sm text-slate-500">{description}</p>
    </GlassCard>
  );
}

export function LoadingPanel({ label = 'Loading FinPilot workspace…' }: { label?: string }) {
  return <GlassCard className="text-center text-sm font-medium text-slate-600">{label}</GlassCard>;
}

type NumberInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'value' | 'onChange'> & {
  value: number;
  onValueChange: (value: number) => void;
  blankWhenZero?: boolean;
};

function formatNumberDisplay(value: number, blankWhenZero: boolean) {
  if (blankWhenZero && value === 0) return '';
  return Number.isFinite(value) ? String(value) : '';
}

export function NumberInput({ value, onValueChange, blankWhenZero = false, className, onBlur, onFocus, ...props }: NumberInputProps) {
  const [displayValue, setDisplayValue] = useState(() => formatNumberDisplay(value, blankWhenZero));
  const [isFocused, setIsFocused] = useState(false);

  useEffect(() => {
    if (!isFocused) {
      setDisplayValue(formatNumberDisplay(value, blankWhenZero));
    }
  }, [blankWhenZero, isFocused, value]);

  return (
    <input
      {...props}
      type="number"
      inputMode="decimal"
      value={displayValue}
      className={className}
      onFocus={(event) => {
        setIsFocused(true);
        event.target.select();
        onFocus?.(event);
      }}
      onBlur={(event) => {
        setIsFocused(false);
        const rawValue = event.target.value.trim();

        if (rawValue === '' || rawValue === '-' || rawValue === '.' || rawValue === '-.') {
          setDisplayValue(formatNumberDisplay(value, blankWhenZero));
          onBlur?.(event);
          return;
        }

        const parsedValue = Number(rawValue);
        if (Number.isFinite(parsedValue)) {
          onValueChange(parsedValue);
          setDisplayValue(formatNumberDisplay(parsedValue, blankWhenZero));
        } else {
          setDisplayValue(formatNumberDisplay(value, blankWhenZero));
        }

        onBlur?.(event);
      }}
      onChange={(event) => {
        const rawValue = event.target.value;
        setDisplayValue(rawValue);

        if (rawValue === '' || rawValue === '-' || rawValue === '.' || rawValue === '-.') {
          return;
        }

        const parsedValue = Number(rawValue);
        if (Number.isFinite(parsedValue)) {
          onValueChange(parsedValue);
        }
      }}
    />
  );
}
