using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Budgets;

public sealed class BudgetServiceTests
{
    [Fact]
    public async Task GetStatusesAsync_ShouldCalculateThresholdAndSpend()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Food", Type = TransactionType.Expense };
        db.Categories.Add(category);
        db.Budgets.Add(new Budget
        {
            UserId = userId,
            Name = "March Budget",
            Month = DateTimeOffset.UtcNow.Month,
            Year = DateTimeOffset.UtcNow.Year,
            TotalLimit = 1000m,
            AlertThresholdPercent = 50,
            BudgetItems = [new BudgetItem { CategoryId = category.Id, LimitAmount = 600m }]
        });
        db.Transactions.Add(new Transaction { UserId = userId, CategoryId = category.Id, AccountId = accountId, Type = TransactionType.Expense, Amount = 550m, Description = "Groceries", TransactionDate = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var service = new BudgetService(db, new FakeDashboardService());
        var statuses = await service.GetStatusesAsync(userId);
        var status = Assert.Single(statuses);

        Assert.Equal(550m, status.TotalSpent);
        Assert.True(status.ThresholdReached);
        Assert.False(status.IsOverBudget);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceBudgetItems()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var firstCategory = new Category { UserId = userId, Name = "Food", Type = TransactionType.Expense };
        var secondCategory = new Category { UserId = userId, Name = "Transport", Type = TransactionType.Expense };
        db.Categories.AddRange(firstCategory, secondCategory);

        var budget = new Budget
        {
            UserId = userId,
            Name = "March Budget",
            Month = DateTimeOffset.UtcNow.Month,
            Year = DateTimeOffset.UtcNow.Year,
            TotalLimit = 1000m,
            AlertThresholdPercent = 50,
            BudgetItems = [new BudgetItem { CategoryId = firstCategory.Id, LimitAmount = 600m }]
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        var service = new BudgetService(db, new FakeDashboardService());
        var updated = await service.UpdateAsync(userId, budget.Id, new UpdateBudgetRequest
        {
            Name = "March Budget Updated",
            Month = budget.Month,
            Year = budget.Year,
            TotalLimit = 1200m,
            AlertThresholdPercent = 70,
            Items = [new BudgetItemRequest { CategoryId = secondCategory.Id, LimitAmount = 700m }]
        });

        var storedBudget = await db.Budgets.Include(x => x.BudgetItems).SingleAsync(x => x.Id == budget.Id);
        var item = Assert.Single(storedBudget.BudgetItems);
        Assert.Equal(secondCategory.Id, item.CategoryId);
        Assert.Equal(700m, item.LimitAmount);
        Assert.Equal("March Budget Updated", updated.Name);
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Budgets.BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Budgets.BudgetStatusResponse>>(Array.Empty<FinPilot.Application.DTOs.Budgets.BudgetStatusResponse>());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>());
        public Task<FinPilot.Application.DTOs.Dashboard.DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new FinPilot.Application.DTOs.Dashboard.DashboardSummaryResponse());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>());
        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
