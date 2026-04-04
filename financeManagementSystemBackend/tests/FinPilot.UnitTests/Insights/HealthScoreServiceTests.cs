using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Insights;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Insights;

public sealed class HealthScoreServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnBreakdownAndSuggestionsForTrackedUser()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Goals.Add(new Goal
        {
            UserId = userId,
            Name = "Emergency Fund",
            CurrentAmount = 15000m,
            TargetAmount = 50000m,
            Status = GoalStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = new HealthScoreService(new FinancialCoachAgentService(new InsightContextBuilder(dbContext, new FakeDashboardService())));
        var result = await service.GetAsync(userId);

        Assert.InRange(result.Score, 25, 95);
        Assert.Equal(5, result.Breakdown.Count);
        Assert.Contains(result.Breakdown, x => x.Category == "Cashflow visibility" && x.Status == "strong");
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public async Task GetAsync_ShouldFlagIncompleteIncomeWhenHistoryIsThin()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var service = new HealthScoreService(new FinancialCoachAgentService(new InsightContextBuilder(dbContext, new LowDataDashboardService())));

        var result = await service.GetAsync(userId);

        Assert.Contains(result.Breakdown, x => x.Category == "Cashflow visibility" && x.Status == "incomplete");
        Assert.Contains(result.Breakdown, x => x.Category == "Savings efficiency" && x.Status == "incomplete");
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSummaryResponse
            {
                TotalIncome = 50000m,
                TotalExpenses = 32000m,
                NetAmount = 18000m,
                TotalBalance = 75000m,
                TransactionCount = 12
            });

        public Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SpendingTrendPointResponse>>([
                new SpendingTrendPointResponse { Month = 1, Year = 2026, Income = 48000m, Expense = 30000m },
                new SpendingTrendPointResponse { Month = 2, Year = 2026, Income = 50000m, Expense = 32000m }
            ]);

        public Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CategoryBreakdownResponse>>([
                new CategoryBreakdownResponse { CategoryId = Guid.NewGuid(), CategoryName = "Food", Amount = 14000m, Percentage = 43.75m },
                new CategoryBreakdownResponse { CategoryId = Guid.NewGuid(), CategoryName = "Transport", Amount = 5000m, Percentage = 15.63m }
            ]);

        public Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<GoalProgressResponse>>(Array.Empty<GoalProgressResponse>());

        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>([
                new BudgetStatusResponse
                {
                    BudgetId = Guid.NewGuid(),
                    BudgetName = "Monthly Budget",
                    TotalLimit = 35000m,
                    TotalSpent = 32000m,
                    RemainingAmount = 3000m,
                    UsagePercent = 91.43m,
                    ThresholdReached = true,
                    IsOverBudget = false
                }
            ]);

        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class LowDataDashboardService : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSummaryResponse());

        public Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SpendingTrendPointResponse>>(Array.Empty<SpendingTrendPointResponse>());

        public Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CategoryBreakdownResponse>>(Array.Empty<CategoryBreakdownResponse>());

        public Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<GoalProgressResponse>>(Array.Empty<GoalProgressResponse>());

        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>(Array.Empty<BudgetStatusResponse>());

        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}