import { Navigate, Route, Routes } from 'react-router-dom';
import { AppShell } from '../components/AppShell';
import { LoadingPanel } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { AccountsPage } from '../pages/AccountsPage';
import { ActivityPage } from '../pages/ActivityPage';
import { AuthPage } from '../pages/AuthPage';
import { BudgetsPage } from '../pages/BudgetsPage';
import { CategoriesPage } from '../pages/CategoriesPage';
import { CoachPage } from '../pages/CoachPage';
import { DashboardPage } from '../pages/DashboardPage';
import { GoalsPage } from '../pages/GoalsPage';
import { InsightsPage } from '../pages/InsightsPage';
import { TransactionsPage } from '../pages/TransactionsPage';

function ProtectedLayout() {
  const { token, loading } = useAuth();

  if (loading) {
    return <div className="p-6"><LoadingPanel /></div>;
  }

  if (!token) {
    return <Navigate to="/auth" replace />;
  }

  return <AppShell />;
}

export default function App() {
  const { token } = useAuth();

  return (
    <Routes>
      <Route path="/auth" element={token ? <Navigate to="/" replace /> : <AuthPage />} />
      <Route path="/" element={<ProtectedLayout />}>
        <Route index element={<DashboardPage />} />
        <Route path="accounts" element={<AccountsPage />} />
        <Route path="transactions" element={<TransactionsPage />} />
        <Route path="budgets" element={<BudgetsPage />} />
        <Route path="goals" element={<GoalsPage />} />
        <Route path="insights" element={<InsightsPage />} />
        <Route path="coach" element={<CoachPage />} />
        <Route path="categories" element={<CategoriesPage />} />
        <Route path="activity" element={<ActivityPage />} />
      </Route>
      <Route path="*" element={<Navigate to={token ? '/' : '/auth'} replace />} />
    </Routes>
  );
}
