import { useEffect, useMemo, useRef, useState } from 'react';
import { AlertTriangle, BarChart3, Bot, LayoutDashboard, LineChart, LogOut, PiggyBank, ReceiptText, ShieldCheck, Target, WalletCards } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Button } from './Button';
import { PageIntro } from './PageIntro';
import { RailNavItem } from './RailNavItem';

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/accounts', label: 'Accounts', icon: WalletCards },
  { to: '/transactions', label: 'Ledger', icon: ReceiptText },
  { to: '/budgets', label: 'Budgets', icon: PiggyBank },
  { to: '/goals', label: 'Goals', icon: Target },
  { to: '/reports', label: 'Reports', icon: LineChart },
  { to: '/insights', label: 'Insights', icon: BarChart3 },
  { to: '/coach', label: 'AI Coach', icon: Bot },
  { to: '/categories', label: 'Categories', icon: ShieldCheck },
];

function buildGreeting(firstName: string) {
  return `${firstName}, here is your financial picture today.`;
}

function buildInitials(fullName?: string | null) {
  if (!fullName) {
    return 'FP';
  }

  const parts = fullName
    .split(' ')
    .map((part) => part.trim())
    .filter(Boolean)
    .slice(0, 2);

  return parts.map((part) => part[0]?.toUpperCase() ?? '').join('') || 'FP';
}

function FinPilotMark() {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className="app-shell__logo-mark">
      <rect x="4" y="4" width="40" height="40" rx="12" className="app-shell__logo-bg" />
      <path d="M14 31L22 23L28 27L35 16" className="app-shell__logo-line" />
      <circle cx="14" cy="31" r="2.5" className="app-shell__logo-dot" />
      <circle cx="22" cy="23" r="2.5" className="app-shell__logo-dot" />
      <circle cx="28" cy="27" r="2.5" className="app-shell__logo-dot" />
      <circle cx="35" cy="16" r="2.5" className="app-shell__logo-dot" />
    </svg>
  );
}

export function AppShell() {
  const { user, logout } = useAuth();
  const firstName = user?.fullName?.split(' ')[0] ?? 'Pilot';
  const initials = useMemo(() => buildInitials(user?.fullName), [user?.fullName]);
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (!menuRef.current?.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setMenuOpen(false);
      }
    }

    document.addEventListener('mousedown', handlePointerDown);
    document.addEventListener('keydown', handleEscape);

    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
      document.removeEventListener('keydown', handleEscape);
    };
  }, []);

  return (
    <div className="app-shell">
      <div className="app-shell__frame">
        <aside className="app-shell__sidebar">
          <div className="app-shell__rail">
            <NavLink to="/" className="app-shell__brand-button" aria-label="FinPilot home">
              <FinPilotMark />
              <span className="app-shell__tooltip">FinPilot</span>
            </NavLink>

            <nav className="app-shell__nav" aria-label="Primary navigation">
              <div className="app-shell__nav-list">
                {navItems.map(({ to, label, icon }) => (
                  <RailNavItem key={to} to={to} label={label} icon={icon} />
                ))}
              </div>
            </nav>

            <div ref={menuRef} className={`app-shell__profile${menuOpen ? ' app-shell__profile--open' : ''}`}>
              <button
                type="button"
                className="app-shell__profile-button"
                aria-label="Open account menu"
                aria-expanded={menuOpen}
                onClick={() => setMenuOpen((current) => !current)}
              >
                <span className="app-shell__avatar">{initials}</span>
                <span className="app-shell__tooltip app-shell__tooltip--profile">{user?.fullName ?? 'FinPilot User'}</span>
              </button>

              {menuOpen ? (
                <div className="app-shell__profile-menu" role="menu" aria-label="Account menu">
                  <div className="app-shell__profile-summary">
                    <span className="app-shell__profile-avatar">{initials}</span>
                    <div>
                      <p className="app-shell__profile-name">{user?.fullName ?? 'FinPilot User'}</p>
                      <p className="app-shell__profile-email">{user?.email ?? '—'}</p>
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    fullWidth
                    iconLeading={<LogOut className="app-shell__menu-icon" />}
                    onClick={() => {
                      setMenuOpen(false);
                      void logout();
                    }}
                  >
                    Sign out
                  </Button>
                </div>
              ) : null}
            </div>
          </div>
        </aside>

        <main className="app-shell__main">
          <div className="app-shell__content">
            <header className="app-shell__header">
              <PageIntro
                eyebrow="Personal finance workspace"
                title={buildGreeting(firstName)}
                description="Track balances, budgets, forecasts, and next actions from one clean financial tracker."
                aside={
                  <div className="app-shell__status">
                    <p className="app-shell__status-eyebrow">Status</p>
                    <p className="app-shell__status-label">Operational</p>
                  </div>
                }
              />
            </header>
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
