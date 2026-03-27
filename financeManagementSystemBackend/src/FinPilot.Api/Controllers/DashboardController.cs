using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class DashboardController(IDashboardService dashboardService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await dashboardService.GetSummaryAsync(userId, cancellationToken);
        return Success(item, "Dashboard summary fetched successfully");
    }

    [HttpGet("spending-trend")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SpendingTrendPointResponse>>>> GetSpendingTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        var userId = EnsureUser();
        var items = await dashboardService.GetSpendingTrendAsync(userId, months, cancellationToken);
        return Success(items, "Spending trend fetched successfully");
    }

    [HttpGet("category-breakdown")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CategoryBreakdownResponse>>>> GetCategoryBreakdown(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await dashboardService.GetCategoryBreakdownAsync(userId, cancellationToken);
        return Success(items, "Category breakdown fetched successfully");
    }

    [HttpGet("budget-health")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BudgetStatusResponse>>>> GetBudgetHealth(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await dashboardService.GetBudgetHealthAsync(userId, cancellationToken);
        return Success(items, "Budget health fetched successfully");
    }

    [HttpGet("goal-progress")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GoalProgressResponse>>>> GetGoalProgress(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await dashboardService.GetGoalProgressAsync(userId, cancellationToken);
        return Success(items, "Goal progress fetched successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
