using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Domain.Enums;

namespace FinPilot.Infrastructure.Insights;

public sealed class InsightContext
{
    public Guid UserId { get; init; }
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public DashboardSummaryResponse Summary { get; init; } = new();
    public IReadOnlyCollection<SpendingTrendPointResponse> TrendPoints { get; init; } = Array.Empty<SpendingTrendPointResponse>();
    public IReadOnlyCollection<CategoryBreakdownResponse> CategoryBreakdown { get; init; } = Array.Empty<CategoryBreakdownResponse>();
    public IReadOnlyCollection<BudgetStatusResponse> BudgetHealth { get; init; } = Array.Empty<BudgetStatusResponse>();
    public IReadOnlyCollection<InsightGoalContext> Goals { get; init; } = Array.Empty<InsightGoalContext>();
    public IReadOnlyCollection<InsightRecentTransaction> RecentTransactions { get; init; } = Array.Empty<InsightRecentTransaction>();
}

public sealed class InsightGoalContext
{
    public Guid GoalId { get; init; }
    public string GoalName { get; init; } = string.Empty;
    public decimal CurrentAmount { get; init; }
    public decimal TargetAmount { get; init; }
    public decimal ProgressPercent { get; init; }
    public DateTimeOffset? TargetDate { get; init; }
    public GoalStatus Status { get; init; }
}

public sealed class InsightRecentTransaction
{
    public Guid TransactionId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Merchant { get; init; }
    public DateTimeOffset TransactionDate { get; init; }
}

