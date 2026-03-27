using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;

namespace FinPilot.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);
}
