export interface ApiError {
  field: string;
  messages: string[];
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors: ApiError[] | null;
}

export interface RegistrationResponse {
  userId: string;
  fullName: string;
  email: string;
  requiresLogin: boolean;
}

export interface AuthResponse {
  userId: string;
  fullName: string;
  email: string;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface CurrentUserResponse {
  userId: string;
  fullName: string;
  email: string;
}

export interface AccountResponse {
  id: string;
  name: string;
  type: number;
  currency: string;
  openingBalance: number;
  currentBalance: number;
}

export interface CategoryResponse {
  id: string;
  name: string;
  type: number;
  color?: string | null;
  icon?: string | null;
  isDefault: boolean;
}

export interface TransactionResponse {
  id: string;
  accountId: string;
  accountName: string;
  categoryId: string;
  categoryName: string;
  type: number;
  amount: number;
  description: string;
  transactionDate: string;
  merchant?: string | null;
  notes?: string | null;
}

export interface BudgetItemResponse {
  categoryId: string;
  categoryName: string;
  limitAmount: number;
  spentAmount: number;
  remainingAmount: number;
  usagePercent: number;
}

export interface BudgetResponse {
  id: string;
  name: string;
  month: number;
  year: number;
  totalLimit: number;
  totalSpent: number;
  remainingAmount: number;
  alertThresholdPercent: number;
  usagePercent: number;
  items: BudgetItemResponse[];
}

export interface BudgetStatusResponse {
  budgetId: string;
  budgetName: string;
  month: number;
  year: number;
  totalLimit: number;
  totalSpent: number;
  remainingAmount: number;
  usagePercent: number;
  isOverBudget: boolean;
  thresholdReached: boolean;
}

export interface GoalResponse {
  id: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  progressPercent: number;
  targetDate?: string | null;
  status: number;
}

export interface DashboardSummaryResponse {
  totalIncome: number;
  totalExpenses: number;
  netAmount: number;
  totalBalance: number;
  transactionCount: number;
}

export interface SpendingTrendPointResponse {
  label: string;
  year: number;
  month: number;
  income: number;
  expense: number;
}

export interface CategoryBreakdownResponse {
  categoryId: string;
  categoryName: string;
  amount: number;
  percentage: number;
}

export interface GoalProgressResponse {
  goalId: string;
  goalName: string;
  currentAmount: number;
  targetAmount: number;
  progressPercent: number;
}

export interface MonthlyForecastResponse {
  currentBalance: number;
  projectedEndOfMonthBalance: number;
  projectedMonthNetAmount: number;
  projectedRemainingNetAmount: number;
  averageDailyNetAmount: number;
  daysTracked: number;
  daysRemaining: number;
  confidence: string;
  assumptions: string[];
  generatedAt: string;
}

export interface DailyForecastPointResponse {
  date: string;
  label: string;
  balance: number;
  dailyNetAmount: number;
  isProjected: boolean;
}

export interface ReportTrendPointResponse {
  year: number;
  month: number;
  label: string;
  income: number;
  expense: number;
  netAmount: number;
}

export interface NetWorthPointResponse {
  year: number;
  month: number;
  label: string;
  netWorth: number;
}

export interface HealthScoreBreakdownResponse {
  category: string;
  status: string;
  summary: string;
}

export interface HealthScoreResponse {
  score: number;
  label: string;
  breakdown: HealthScoreBreakdownResponse[];
  strengths: string[];
  risks: string[];
  suggestions: string[];
  generatedAt: string;
  disclaimer: string;
}

export interface InsightsOverviewSectionResponse {
  key: string;
  title: string;
  headline: string;
  priority: string;
}

export interface InsightsOverviewResponse {
  headline: string;
  healthScore: number;
  healthLabel: string;
  sections: InsightsOverviewSectionResponse[];
  generatedAt: string;
  disclaimer: string;
}

export interface InsightCardResponse {
  title: string;
  type: string;
  priority: string;
  summary: string;
  recommendations: string[];
}

export interface InsightBundleResponse {
  headline: string;
  cards: InsightCardResponse[];
  generatedAt: string;
  disclaimer: string;
}

export interface DashboardCoachWidgetResponse {
  healthScore: number;
  headline: string;
  encouragement: string;
  topPatterns: string[];
  primaryAction: string;
  estimatedMonthlyImpact: number;
  disclaimer: string;
  generatedAt: string;
}

export interface DashboardReportWidgetResponse {
  title: string;
  summary: string;
  highlights: string[];
  forecast: string;
  disclaimer: string;
  generatedAt: string;
}

export interface AgentChatResponse {
  reply: string;
  agentUsed: number;
  followUpSuggestions: string[];
  generatedAt: string;
}

export interface CoachBehaviorPatternResponse {
  pattern: string;
  impact: string;
  description: string;
}

export interface CoachSuggestionResponse {
  title: string;
  action: string;
  expectedMonthlyImpact: number;
}

export interface CoachAnalysisResponse {
  healthScore: number;
  behavioralPatterns: CoachBehaviorPatternResponse[];
  suggestions: CoachSuggestionResponse[];
  encouragement: string;
  generatedAt: string;
}

export interface AnomalyAnalysisResponse {
  transactionId: string;
  severity: string;
  anomalyType: string;
  riskScore: number;
  explanation: string;
  recommendedAction: string;
  flagForReview: boolean;
  signals: string[];
  generatedAt: string;
}

export interface BudgetAdvisorAnalysisResponse {
  status: string;
  overrunCategories: Array<{ category: string; overrunAmount: number; projectedMonthEnd: number }>;
  recommendations: string[];
  safeToSpend: Record<string, number>;
  generatedAt: string;
}

export interface InvestmentAdvisorAnalysisResponse {
  disclaimer: string;
  allocationSuggestions: Array<{ label: string; percentage: number; rationale: string }>;
  reasoning: string;
  priorityActions: string[];
  generatedAt: string;
}

export interface ReportGeneratorAnalysisResponse {
  title: string;
  summary: string;
  highlights: string[];
  forecast: string;
  markdownReport: string;
  generatedAt: string;
}

export interface AgentResultResponse {
  id: string;
  agent: number;
  trigger: number;
  status: string;
  severity: string;
  sourceEntityName: string;
  sourceEntityId?: string | null;
  summary: string;
  errorMessage?: string | null;
  isDismissed: boolean;
  generatedAt: string;
  expiresAt?: string | null;
  anomaly?: AnomalyAnalysisResponse | null;
  budget?: BudgetAdvisorAnalysisResponse | null;
  coach?: CoachAnalysisResponse | null;
  investment?: InvestmentAdvisorAnalysisResponse | null;
  report?: ReportGeneratorAnalysisResponse | null;
}

export interface AuditLogResponse {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  oldValues?: string | null;
  newValues?: string | null;
  createdAt: string;
}
