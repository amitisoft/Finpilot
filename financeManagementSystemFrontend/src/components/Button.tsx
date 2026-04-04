import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/utils';

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
type ButtonSize = 'sm' | 'md' | 'lg';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  fullWidth?: boolean;
  iconLeading?: ReactNode;
  iconTrailing?: ReactNode;
}

export function Button({
  className,
  variant = 'primary',
  size = 'md',
  fullWidth = false,
  iconLeading,
  iconTrailing,
  children,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      type={type}
      className={cn(
        'app-button',
        `app-button--${variant}`,
        `app-button--${size}`,
        fullWidth && 'app-button--full-width',
        className
      )}
    >
      {iconLeading ? <span className="app-button__icon app-button__icon--leading">{iconLeading}</span> : null}
      <span className="app-button__label">{children}</span>
      {iconTrailing ? <span className="app-button__icon app-button__icon--trailing">{iconTrailing}</span> : null}
    </button>
  );
}
