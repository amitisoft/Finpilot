import { useState } from 'react';
import { Sparkles, TrendingUp } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { ApiClient } from '../lib/api';
import { API_DISPLAY_URL, flattenErrors } from '../lib/utils';
import type { AuthResponse } from '../types/api';

const publicApi = new ApiClient(() => null);

export function AuthPage() {
  const { login } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      const response = await publicApi.post<AuthResponse>(mode === 'login' ? '/api/Auth/login' : '/api/Auth/register', {
        ...(mode === 'register' ? { fullName } : {}),
        email,
        password
      });

      await login(response);
    } catch (submissionError) {
      setError(flattenErrors(submissionError));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center px-4 py-8 text-slate-900">
      <div className="grid w-full max-w-6xl gap-6 lg:grid-cols-[1.05fr_0.95fr]">
        <section className="rounded-[2rem] border border-slate-200 bg-white p-8 shadow-glass backdrop-blur-xl">
          <div className="rounded-[2rem] bg-white p-8">
            <p className="text-xs font-black uppercase tracking-[0.35em] text-violet-300">FinPilot Premium</p>
            <h1 className="mt-4 text-5xl font-black tracking-tight">Your command center for money, goals, and AI guidance.</h1>
            <p className="mt-4 max-w-2xl text-base text-slate-600">A premium personal finance workspace with glassmorphism surfaces, live account intelligence, anomaly alerts, and an AI coach built on your actual financial data.</p>

            <div className="mt-8 grid gap-4 md:grid-cols-2">
              <div className="rounded-[1.75rem] bg-hero-gradient p-5 text-white shadow-glow">
                <Sparkles className="h-6 w-6 text-white" />
                <p className="mt-6 text-xs font-black uppercase tracking-[0.35em] text-white/70">AI Co-Pilot</p>
                <p className="mt-2 text-2xl font-black">Coaching, anomaly review, monthly reports, and budget advice.</p>
              </div>
              <div className="rounded-[1.75rem] border border-slate-200 bg-slate-50 p-5">
                <TrendingUp className="h-6 w-6 text-emerald-300" />
                <p className="mt-6 text-xs font-black uppercase tracking-[0.35em] text-slate-400">Live backend</p>
                <p className="mt-2 text-2xl font-black">Directly wired to {API_DISPLAY_URL}</p>
              </div>
            </div>
          </div>
        </section>

        <section className="rounded-[2rem] border border-slate-200 bg-slate-50 p-8 shadow-glass backdrop-blur-xl">
          <div className="mb-8 flex items-center justify-between">
            <div>
              <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Welcome</p>
              <h2 className="mt-2 text-3xl font-black tracking-tight">{mode === 'login' ? 'Sign in to FinPilot' : 'Create your workspace'}</h2>
            </div>
            <div className="rounded-full border border-slate-200 bg-white p-1 text-sm">
              <button onClick={() => setMode('login')} className={`rounded-full px-4 py-2 font-semibold ${mode === 'login' ? 'bg-white text-slate-950' : 'text-slate-600'}`}>Login</button>
              <button onClick={() => setMode('register')} className={`rounded-full px-4 py-2 font-semibold ${mode === 'register' ? 'bg-white text-slate-950' : 'text-slate-600'}`}>Register</button>
            </div>
          </div>

          <form onSubmit={onSubmit} className="space-y-4">
            {mode === 'register' && (
              <label className="block">
                <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Full name</span>
                <input value={fullName} onChange={(e) => setFullName(e.target.value)} required className="w-full rounded-3xl border border-slate-200 bg-white px-5 py-4 outline-none ring-0 transition focus:border-violet-400" placeholder="Abhishek Shukla" />
              </label>
            )}
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Email</span>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required className="w-full rounded-3xl border border-slate-200 bg-white px-5 py-4 outline-none transition focus:border-violet-400" placeholder="you@example.com" />
            </label>
            <label className="block">
              <span className="mb-2 block text-xs font-black uppercase tracking-[0.35em] text-slate-400">Password</span>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required className="w-full rounded-3xl border border-slate-200 bg-white px-5 py-4 outline-none transition focus:border-violet-400" placeholder="Password@123" />
            </label>

            {error && <div className="rounded-3xl border border-rose-500/20 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">{error}</div>}

            <button type="submit" disabled={submitting} className="w-full rounded-3xl bg-gradient-to-r from-indigo-500 via-fuchsia-500 to-pink-500 px-6 py-4 text-sm font-black uppercase tracking-[0.35em] text-white shadow-glow transition hover:opacity-95 disabled:cursor-not-allowed disabled:opacity-60">
              {submitting ? 'Please wait…' : mode === 'login' ? 'Enter Command Center' : 'Create FinPilot Account'}
            </button>
          </form>
        </section>
      </div>
    </div>
  );
}
