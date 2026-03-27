using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Agents;

public sealed class AnomalyAgentServiceTests
{
    [Fact]
    public async Task AnalyzeTransactionAsync_ShouldReturnHighSeverityForLargeNewMerchantSpike()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Shopping", Type = TransactionType.Expense };
        dbContext.Categories.Add(category);

        var baseline = Enumerable.Range(1, 5).Select(index => new Transaction
        {
            UserId = userId,
            AccountId = Guid.NewGuid(),
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 200 + index,
            Description = $"Purchase {index}",
            Merchant = "Known Store",
            TransactionDate = DateTimeOffset.UtcNow.AddDays(-10 - index)
        });

        var suspicious = new Transaction
        {
            UserId = userId,
            AccountId = Guid.NewGuid(),
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 6000m,
            Description = "Large purchase",
            Merchant = "Unknown Store",
            TransactionDate = DateTimeOffset.UtcNow
        };

        dbContext.Transactions.AddRange(baseline);
        dbContext.Transactions.Add(suspicious);
        await dbContext.SaveChangesAsync();

        var service = new AnomalyAgentService(dbContext);
        var result = await service.AnalyzeTransactionAsync(userId, suspicious.Id);

        Assert.Equal("high", result.Severity);
        Assert.True(result.FlagForReview);
        Assert.Equal("verify", result.RecommendedAction);
        Assert.Contains(result.Signals, x => x.Contains("usual range", StringComparison.OrdinalIgnoreCase) || x.Contains("new merchant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AnalyzeTransactionAsync_ShouldReturnNoneForNormalRecurringSpend()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var category = new Category { UserId = userId, Name = "Bills", Type = TransactionType.Expense };
        dbContext.Categories.Add(category);

        var history = Enumerable.Range(1, 4).Select(index => new Transaction
        {
            UserId = userId,
            AccountId = Guid.NewGuid(),
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 799m,
            Description = "Utility bill",
            Merchant = "Power Co",
            TransactionDate = DateTimeOffset.UtcNow.AddDays(-30 * index)
        });

        var current = new Transaction
        {
            UserId = userId,
            AccountId = Guid.NewGuid(),
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 799m,
            Description = "Utility bill",
            Merchant = "Power Co",
            TransactionDate = DateTimeOffset.UtcNow
        };

        dbContext.Transactions.AddRange(history);
        dbContext.Transactions.Add(current);
        await dbContext.SaveChangesAsync();

        var service = new AnomalyAgentService(dbContext);
        var result = await service.AnalyzeTransactionAsync(userId, current.Id);

        Assert.Equal("none", result.Severity);
        Assert.False(result.FlagForReview);
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }
}
