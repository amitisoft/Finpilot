export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '';

export const accountTypeLabels: Record<number, string> = {
  1: 'Cash',
  2: 'Bank',
  3: 'Credit Card',
  4: 'Wallet',
  5: 'Investment'
};

export const transactionTypeLabels: Record<number, string> = {
  1: 'Income',
  2: 'Expense'
};

export const goalStatusLabels: Record<number, string> = {
  1: 'Active',
  2: 'Completed',
  3: 'Archived'
};

export const agentTypeLabels: Record<number, string> = {
  1: 'Anomaly',
  2: 'Budget',
  3: 'Coach',
  4: 'Investment',
  5: 'Report'
};

export const API_DISPLAY_URL = API_BASE_URL || 'same-origin /api';

export function cn(...classes: Array<string | false | null | undefined>) {
  return classes.filter(Boolean).join(' ');
}

export function formatCurrency(value: number, currency = 'INR') {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0
  }).format(value ?? 0);
}

export function formatCompactCurrency(value: number, currency = 'INR') {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency,
    notation: 'compact',
    maximumFractionDigits: 1
  }).format(value ?? 0);
}

export function formatDate(value?: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  }).format(new Date(value));
}

export function formatDateTime(value?: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

export function monthName(month: number) {
  return new Date(2026, Math.max(month - 1, 0), 1).toLocaleString('en-IN', { month: 'short' });
}

export function flattenErrors(errorLike: unknown): string {
  if (!errorLike) return 'Something went wrong.';
  if (typeof errorLike === 'string') return errorLike;
  if (errorLike instanceof Error) return errorLike.message;
  return 'Something went wrong.';
}
