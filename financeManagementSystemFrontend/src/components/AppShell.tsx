import { AlertTriangle, Bot, BarChart3, LayoutDashboard, LogOut, PiggyBank, ReceiptText, ShieldCheck, Target, WalletCards } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { cn } from '../lib/utils';

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/accounts', label: 'Accounts', icon: WalletCards },
  { to: '/transactions', label: 'Ledger', icon: ReceiptText },
  { to: '/budgets', label: 'Budgets', icon: PiggyBank },
  { to: '/goals', label: 'Goals', icon: Target },
  { to: '/insights', label: 'Insights', icon: BarChart3 },
  { to: '/coach', label: 'AI Coach', icon: Bot },
  { to: '/categories', label: 'Categories', icon: ShieldCheck },
  { to: '/activity', label: 'Activity', icon: AlertTriangle }
];

export function AppShell() {
  const { user, logout } = useAuth();

  return (
    <div className="h-screen overflow-hidden bg-slate-100 p-4 text-slate-900 md:p-6 lg:p-8">
      <div className="mx-auto flex h-full max-w-[1600px] gap-6">
        <aside className="hidden h-full w-72 shrink-0 overflow-hidden rounded-[2rem] border border-slate-200 bg-white p-5 shadow-glass lg:flex lg:flex-col">
          <div className="mb-6 rounded-3xl bg-hero-gradient p-5 text-white shadow-glow">
            <p className="text-xs font-black uppercase tracking-[0.35em] text-white/75">FinPilot Premium</p>
            <h1 className="mt-3 text-3xl font-black tracking-tight">Money, coached intelligently.</h1>
            <p className="mt-2 text-sm text-white/85">A calmer finance workspace built for daily decisions, not clutter.</p>
          </div>

          <nav className="min-h-0 flex-1 space-y-2 overflow-y-auto pr-1">
            {navItems.map(({ to, label, icon: Icon }) => (
              <NavLink
                key={to}
                to={to}
                className={({ isActive }) => cn(
                  'flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-semibold transition',
                  isActive ? 'bg-slate-900 text-white shadow-sm' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                )}
              >
                <Icon className="h-4 w-4" />
                {label}
              </NavLink>
            ))}
          </nav>

          <div className="mt-6 rounded-3xl border border-slate-200 bg-slate-50 p-4">
            <p className="text-xs font-black uppercase tracking-[0.3em] text-slate-400">Logged in as</p>
            <p className="mt-2 text-lg font-black text-slate-900">{user?.fullName ?? 'FinPilot User'}</p>
            <p className="text-sm text-slate-500">{user?.email ?? '—'}</p>
            <button
              onClick={() => void logout()}
              className="mt-4 flex w-full items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-semibold text-slate-700 transition hover:bg-slate-100"
            >
              <LogOut className="h-4 w-4" />
              Sign out
            </button>
          </div>
        </aside>

        <main className="flex min-w-0 flex-1 flex-col overflow-hidden rounded-[2rem] border border-slate-200 bg-white p-4 shadow-glass md:p-6">
          <header className="shrink-0 flex flex-col gap-3 rounded-[2rem] border border-slate-200 bg-slate-50 p-5 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Premium Finance Workspace</p>
              <h2 className="mt-2 text-3xl font-black tracking-tight text-slate-900">Welcome back, {user?.fullName?.split(' ')[0] ?? 'Pilot'}.</h2>
            </div>
            <div className="rounded-2xl bg-emerald-50 px-4 py-3 text-right text-emerald-700">
              <p className="text-xs font-black uppercase tracking-[0.35em]">Status</p>
              <p className="text-lg font-black">Optimal</p>
            </div>
          </header>
          <div className="min-h-0 flex-1 overflow-x-hidden overflow-y-auto pb-6 pr-1 pt-6">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}

