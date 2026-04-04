import { useState } from 'react';
import { Sparkles, TrendingUp } from 'lucide-react';
import { Button } from '../components/Button';
import { FormField } from '../components/FormField';
import { useAuth } from '../contexts/AuthContext';
import { ApiClient } from '../lib/api';
import { API_DISPLAY_URL, flattenErrors } from '../lib/utils';
import type { AuthResponse, RegistrationResponse } from '../types/api';

const publicApi = new ApiClient(() => null);

export function AuthPage() {
  const { login } = useAuth();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleModeChange = (nextMode: 'login' | 'register') => {
    setMode(nextMode);
    setError(null);
    if (nextMode === 'register') {
      setSuccess(null);
    }
  };

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setSuccess(null);

    try {
      if (mode === 'register') {
        const response = await publicApi.post<RegistrationResponse>('/api/Auth/register', {
          fullName: fullName.trim(),
          email: email.trim(),
          password
        });

        setFullName('');
        setPassword('');
        setMode('login');
        setSuccess(response.requiresLogin ? 'Account created successfully. Please sign in to continue.' : 'Account created successfully.');
        return;
      }

      const response = await publicApi.post<AuthResponse>('/api/Auth/login', {
        email: email.trim(),
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
    <div className="auth-page">
      <div className="auth-page__layout">
        <section className="auth-page__sidebar">
          <div className="auth-page__sidebar-inner">
            <p className="auth-page__eyebrow">FinPilot Premium</p>
            <h1 className="auth-page__headline">Money clarity, forecasting, and AI guidance in one workspace.</h1>
            <p className="auth-page__copy">Built for a calm finance workflow: accounts, budgets, reports, and a coaching layer that translates raw activity into next actions.</p>

            <div className="auth-page__feature-grid">
              <article className="auth-page__feature auth-page__feature--primary">
                <Sparkles className="auth-page__feature-icon" />
                <p className="auth-page__feature-label">AI guidance</p>
                <h2 className="auth-page__feature-title">Coaching, anomaly review, and monthly report generation.</h2>
                <p className="auth-page__feature-copy">A single assistant layer for insight, explanation, and prioritized actions.</p>
              </article>
              <article className="auth-page__feature">
                <TrendingUp className="auth-page__feature-icon" />
                <p className="auth-page__feature-label">Live backend</p>
                <h2 className="auth-page__feature-title">Connected directly to {API_DISPLAY_URL}.</h2>
                <p className="auth-page__feature-copy">The same app surfaces accounts, forecasts, goals, budgets, and reports from backend data.</p>
              </article>
            </div>
          </div>
        </section>

        <section className="auth-page__panel">
          <div className="auth-page__header">
            <div>
              <p className="auth-page__eyebrow">Welcome</p>
              <h2 className="auth-page__title">{mode === 'login' ? 'Sign in to FinPilot' : 'Create your workspace'}</h2>
            </div>
            <div className="auth-page__toggle" role="tablist" aria-label="Authentication mode">
              <button type="button" onClick={() => handleModeChange('login')} className={`auth-page__toggle-button${mode === 'login' ? ' auth-page__toggle-button--active' : ''}`}>Login</button>
              <button type="button" onClick={() => handleModeChange('register')} className={`auth-page__toggle-button${mode === 'register' ? ' auth-page__toggle-button--active' : ''}`}>Register</button>
            </div>
          </div>

          <form onSubmit={onSubmit} className="auth-page__form">
            {mode === 'register' && (
              <FormField label="Full name">
                <input value={fullName} onChange={(event) => setFullName(event.target.value)} required className="app-form-control" placeholder="Abhishek Shukla" />
              </FormField>
            )}

            <FormField label="Email">
              <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} required className="app-form-control" placeholder="you@example.com" />
            </FormField>

            <FormField label="Password">
              <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} required className="app-form-control" placeholder="Password@123" />
            </FormField>

            {success && <div className="auth-page__message auth-page__message--success">{success}</div>}
            {error && <div className="auth-page__message auth-page__message--error">{error}</div>}

            <Button type="submit" disabled={submitting} fullWidth size="lg" className="auth-page__submit">
              {submitting ? 'Please wait…' : mode === 'login' ? 'Sign in to workspace' : 'Create FinPilot account'}
            </Button>
          </form>
        </section>
      </div>
    </div>
  );
}
