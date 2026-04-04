import type { LucideIcon } from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { cn } from '../lib/utils';

interface RailNavItemProps {
  to: string;
  label: string;
  icon: LucideIcon;
}

export function RailNavItem({ to, label, icon: Icon }: RailNavItemProps) {
  return (
    <NavLink
      to={to}
      aria-label={label}
      className={({ isActive }) => cn('app-shell__nav-link', isActive && 'app-shell__nav-link--active')}
    >
      <Icon className="app-shell__nav-icon" />
      <span className="app-shell__tooltip app-shell__tooltip--nav">{label}</span>
    </NavLink>
  );
}
