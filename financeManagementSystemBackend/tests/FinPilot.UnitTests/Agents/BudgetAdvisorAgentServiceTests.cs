using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinPilot.UnitTests.Agents;

public sealed class BudgetAdvisorAgentServiceTests
{
    [Fact]
    public async Task AnalyzeBudgetAsync_ShouldReturnOverBudgetStatusWhenSpendExceedsLimit()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Food", Type = TransactionType.Expense };
        db.Categories.Add(category);
        var budget = new Budget
        {
            UserId = userId,
            Name = "March Budget",
            Month = DateTimeOffset.UtcNow.Month,
            Year = DateTimeOffset.UtcNow.Year,
            TotalLimit = 1000m,
            AlertThresholdPercent = 70,
            BudgetItems = [new BudgetItem { CategoryId = category.Id, LimitAmount = 600m }]
        };
        db.Budgets.Add(budget);
        db.Transactions.Add(new Transaction { UserId = userId, CategoryId = category.Id, AccountId = Guid.NewGuid(), Type = TransactionType.Expense, Amount = 1100m, Description = "Groceries", TransactionDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new BudgetAdvisorAgentService(new BudgetService(db, new FakeDashboardService(), new FakeAgentOrchestratorService(), NullLogger<BudgetService>.Instance));
        var result = await service.AnalyzeBudgetAsync(userId, budget.Id);

        Assert.Equal("over_budget", result.Status);
        Assert.NotEmpty(result.OverrunCategories);
        Assert.Contains(result.Recommendations, x => x.Contains("Freeze discretionary spending", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeBudgetAsync_ShouldReturnOnTrackStatusWhenSpendIsHealthy()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Transport", Type = TransactionType.Expense };
        db.Categories.Add(category);
        var budget = new Budget
        {
            UserId = userId,
            Name = "Transport Budget",
            Month = DateTimeOffset.UtcNow.Month,
            Year = DateTimeOffset.UtcNow.Year,
            TotalLimit = 2000m,
            AlertThresholdPercent = 80,
            BudgetItems = [new BudgetItem { CategoryId = category.Id, LimitAmount = 1000m }]
        };
        db.Budgets.Add(budget);
        db.Transactions.Add(new Transaction { UserId = userId, CategoryId = category.Id, AccountId = Guid.NewGuid(), Type = TransactionType.Expense, Amount = 200m, Description = "Metro", TransactionDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new BudgetAdvisorAgentService(new BudgetService(db, new FakeDashboardService(), new FakeAgentOrchestratorService(), NullLogger<BudgetService>.Instance));
        var result = await service.AnalyzeBudgetAsync(userId, budget.Id);

        Assert.Equal("on_track", result.Status);
        Assert.All(result.SafeToSpend, x => Assert.True(x.RemainingAmount >= 0));
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>(Array.Empty<BudgetStatusResponse>());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>());
        public Task<FinPilot.Application.DTOs.Dashboard.DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new FinPilot.Application.DTOs.Dashboard.DashboardSummaryResponse());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>());
        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAgentOrchestratorService : IAgentOrchestratorService
    {
        public Task QueueTransactionAnomalyCheckAsync(Guid userId, Guid transactionId, AgentTrigger trigger, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task QueueBudgetCheckAsync(Guid userId, Guid budgetId, AgentTrigger trigger, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task QueueBudgetChecksForPeriodAsync(Guid userId, int month, int year, AgentTrigger trigger, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<FinPilot.Application.DTOs.Agents.AgentInvocationResponse> InvokeAsync(Guid userId, FinPilot.Application.DTOs.Agents.InvokeAgentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FinPilot.Application.DTOs.Agents.AgentChatResponse> ChatAsync(Guid userId, FinPilot.Application.DTOs.Agents.AgentChatRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
