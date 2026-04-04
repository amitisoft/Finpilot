import { useEffect, useState } from 'react';
import type { InputHTMLAttributes, ReactNode } from 'react';
import { X } from 'lucide-react';
import { cn } from '../lib/utils';

export function GlassCard({ className, children }: { className?: string; children: ReactNode }) {
  return <div className={cn('app-card', className)}>{children}</div>;
}

export function SectionTitle({ eyebrow, title, action }: { eyebrow: string; title: string; action?: ReactNode }) {
  return (
    <div className="app-section-title">
      <div>
        <p className="app-section-title__eyebrow">{eyebrow}</p>
        <h3 className="app-section-title__title">{title}</h3>
      </div>
      {action}
    </div>
  );
}

export function MetricCard({ label, value, tone = 'default' }: { label: string; value: string; tone?: 'default' | 'positive' | 'negative' }) {
  return (
    <GlassCard className="app-metric-card">
      <p className="app-metric-card__label">{label}</p>
      <p className={`app-metric-card__value app-metric-card__value--${tone}`}>{value}</p>
    </GlassCard>
  );
}

export function Badge({ children, tone = 'slate' }: { children: ReactNode; tone?: 'slate' | 'emerald' | 'rose' | 'amber' | 'violet' }) {
  return <span className={`app-badge app-badge--${tone}`}>{children}</span>;
}

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <GlassCard className="app-card--dashed app-card--centered app-empty-state">
      <p className="app-empty-state__eyebrow">No data yet</p>
      <h4 className="app-empty-state__title">{title}</h4>
      <p className="app-empty-state__copy">{description}</p>
    </GlassCard>
  );
}

export function LoadingPanel({ label = 'Loading FinPilot workspace…' }: { label?: string }) {
  return <GlassCard className="app-loading-panel">{label}</GlassCard>;
}

export function DetailModal({
  open,
  title,
  eyebrow,
  description,
  onClose,
  children
}: {
  open: boolean;
  title: string;
  eyebrow?: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
}) {
  useEffect(() => {
    if (!open) return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [onClose, open]);

  if (!open) {
    return null;
  }

  return (
    <div className="app-modal" role="dialog" aria-modal="true" aria-label={title}>
      <button type="button" className="app-modal__backdrop" aria-label="Close detail view" onClick={onClose} />
      <div className="app-modal__surface">
        <div className="app-modal__header">
          <div>
            {eyebrow ? <p className="app-modal__eyebrow">{eyebrow}</p> : null}
            <h3 className="app-modal__title">{title}</h3>
            {description ? <p className="app-modal__description">{description}</p> : null}
          </div>
          <button type="button" className="app-modal__close" aria-label="Close detail view" onClick={onClose}>
            <X size={18} />
          </button>
        </div>
        <div className="app-modal__body">{children}</div>
      </div>
    </div>
  );
}

type NumberInputProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'value' | 'onChange'> & {
  value: number;
  onValueChange: (value: number) => void;
  blankWhenZero?: boolean;
};

function formatNumberDisplay(value: number, blankWhenZero: boolean) {
  if (blankWhenZero && value == 0) return '';
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

        if (rawValue === '' || rawValue == '-' || rawValue == '.' || rawValue == '-.') {
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
