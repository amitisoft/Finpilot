import type { ReactNode } from 'react';
import { cn } from '../lib/utils';

interface FormFieldProps {
  label: string;
  hint?: string;
  error?: string | null;
  className?: string;
  children: ReactNode;
}

export function FormField({ label, hint, error, className, children }: FormFieldProps) {
  return (
    <label className={cn('app-form-field', error && 'app-form-field--error', className)}>
      <span className="app-form-field__label">{label}</span>
      {children}
      {hint ? <span className="app-form-field__hint">{hint}</span> : null}
      {error ? <span className="app-form-field__error">{error}</span> : null}
    </label>
  );
}
