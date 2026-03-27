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

public sealed class ReportGeneratorAgentServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldProduceMarkdownReportAndHighlights()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        dbContext.Goals.Add(new Goal
        {
            UserId = userId,
            Name = "House Fund",
            CurrentAmount = 30000m,
            TargetAmount = 100000m,
            Status = GoalStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = new ReportGeneratorAgentService(new InsightContextBuilder(dbContext, new FakeDashboardService()));
        var result = await service.AnalyzeAsync(userId);

        Assert.Contains("# Monthly Financial Report", result.MarkdownReport);
        Assert.Contains("## Cashflow", result.MarkdownReport);
        Assert.NotEmpty(result.Highlights);
        Assert.Contains("Forecast", result.MarkdownReport, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSummaryResponse { TotalIncome = 55000m, TotalExpenses = 30000m, NetAmount = 25000m, TotalBalance = 90000m, TransactionCount = 14 });
        public Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SpendingTrendPointResponse>>([
                new SpendingTrendPointResponse { Month = 1, Year = 2026, Income = 52000m, Expense = 29000m },
                new SpendingTrendPointResponse { Month = 2, Year = 2026, Income = 55000m, Expense = 30000m }
            ]);
        public Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CategoryBreakdownResponse>>([
                new CategoryBreakdownResponse { CategoryId = Guid.NewGuid(), CategoryName = "Housing", Amount = 10000m, Percentage = 33.33m },
                new CategoryBreakdownResponse { CategoryId = Guid.NewGuid(), CategoryName = "Food", Amount = 6000m, Percentage = 20m }
            ]);
        public Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<GoalProgressResponse>>(Array.Empty<GoalProgressResponse>());
        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>([
                new BudgetStatusResponse { BudgetId = Guid.NewGuid(), BudgetName = "Monthly Budget", TotalLimit = 32000m, TotalSpent = 30000m, RemainingAmount = 2000m, UsagePercent = 93.75m, ThresholdReached = true, IsOverBudget = false }
            ]);
        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
