using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Insights;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Agents;

public sealed class InvestmentAdvisorAgentServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldReturnAllocationPlanForPositiveCashflow()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Goals.Add(new Goal
        {
            UserId = userId,
            Name = "Retirement",
            CurrentAmount = 20000m,
            TargetAmount = 200000m,
            Status = GoalStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = new InvestmentAdvisorAgentService(new InsightContextBuilder(dbContext, new PositiveCashflowDashboardService()));
        var result = await service.AnalyzeAsync(userId, "moderate", 29);

        Assert.Contains("not licensed investment advice", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("moderate", result.RiskProfile);
        Assert.True(result.MonthlySurplus > 0);
        Assert.Equal(100, result.AllocationSuggestions.Sum(x => x.Percentage));
        Assert.NotEmpty(result.PriorityActions);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFavorLiquidityWhenCashflowIsNegative()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var service = new InvestmentAdvisorAgentService(new InsightContextBuilder(dbContext, new NegativeCashflowDashboardService()));

        var result = await service.AnalyzeAsync(userId);

        Assert.Equal(0m, result.MonthlySurplus);
        Assert.Equal("Emergency fund", result.AllocationSuggestions.First().Bucket);
        Assert.Contains(result.PriorityActions, x => x.Contains("expenses below income", StringComparison.OrdinalIgnoreCase));
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class PositiveCashflowDashboardService : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSummaryResponse { TotalIncome = 60000m, TotalExpenses = 35000m, NetAmount = 25000m, TotalBalance = 150000m, TransactionCount = 10 });
        public Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SpendingTrendPointResponse>>([
                new SpendingTrendPointResponse { Month = 1, Year = 2026, Income = 58000m, Expense = 34000m },
                new SpendingTrendPointResponse { Month = 2, Year = 2026, Income = 60000m, Expense = 35000m }
            ]);
        public Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CategoryBreakdownResponse>>([
                new CategoryBreakdownResponse { CategoryId = Guid.NewGuid(), CategoryName = "Housing", Amount = 12000m, Percentage = 34.29m }
            ]);
        public Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<GoalProgressResponse>>(Array.Empty<GoalProgressResponse>());
        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>(Array.Empty<BudgetStatusResponse>());
        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NegativeCashflowDashboardService : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSummaryResponse { TotalIncome = 30000m, TotalExpenses = 36000m, NetAmount = -6000m, TotalBalance = 12000m, TransactionCount = 8 });
        public Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SpendingTrendPointResponse>>([
                new SpendingTrendPointResponse { Month = 1, Year = 2026, Income = 30000m, Expense = 36000m }
            ]);
        public Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CategoryBreakdownResponse>>(Array.Empty<CategoryBreakdownResponse>());
        public Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<GoalProgressResponse>>(Array.Empty<GoalProgressResponse>());
        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>(Array.Empty<BudgetStatusResponse>());
        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
