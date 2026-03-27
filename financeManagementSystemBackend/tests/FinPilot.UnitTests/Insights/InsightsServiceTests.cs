using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Insights;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FinPilot.UnitTests.Insights;

public sealed class InsightsServiceTests
{
    [Fact]
    public async Task GetBudgetRiskInsightsAsync_ShouldReturnWarningWhenThresholdReached()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Food", Type = TransactionType.Expense };
        db.Categories.Add(category);
        db.Budgets.Add(new Budget
        {
            UserId = userId,
            Name = "Monthly Budget",
            Month = DateTimeOffset.UtcNow.Month,
            Year = DateTimeOffset.UtcNow.Year,
            TotalLimit = 1000m,
            AlertThresholdPercent = 50,
            BudgetItems = [new BudgetItem { CategoryId = category.Id, LimitAmount = 800m }]
        });
        db.Transactions.Add(new Transaction
        {
            UserId = userId,
            AccountId = accountId,
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 700m,
            Description = "Groceries",
            TransactionDate = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetBudgetRiskInsightsAsync(userId);

        Assert.Contains(result.Cards, x => x.Type == "warning");
    }

    [Fact]
    public async Task GetAnomalyInsightsAsync_ShouldDetectDuplicateMerchantAmountPattern()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Subscriptions", Type = TransactionType.Expense };
        db.Categories.Add(category);
        db.Transactions.AddRange(
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = category.Id, Type = TransactionType.Expense, Amount = 499m, Description = "Music", Merchant = "Spotify", TransactionDate = DateTimeOffset.UtcNow.AddDays(-10) },
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = category.Id, Type = TransactionType.Expense, Amount = 499m, Description = "Music", Merchant = "Spotify", TransactionDate = DateTimeOffset.UtcNow.AddDays(-2) }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetAnomalyInsightsAsync(userId);

        Assert.Contains(result.Cards, x => x.Title.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || x.Summary.Contains("Spotify", StringComparison.OrdinalIgnoreCase));
    }

    private static InsightsService CreateService(FinPilotDbContext db)
    {
        var dashboardService = new DashboardService(db, CreateCache());
        var builder = new InsightContextBuilder(db, dashboardService);
        return new InsightsService(builder);
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }
}
