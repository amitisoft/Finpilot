using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FinPilot.UnitTests.Dashboard;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCurrentMonthAggregates()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Accounts.Add(new Account { UserId = userId, Name = "Main", Type = AccountType.Bank, Currency = "INR", OpeningBalance = 1000m, CurrentBalance = 1300m });
        db.Transactions.AddRange(
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Income, Amount = 500m, Description = "Salary", TransactionDate = DateTimeOffset.UtcNow },
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Expense, Amount = 200m, Description = "Food", TransactionDate = DateTimeOffset.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new DashboardService(db, CreateCache());
        var summary = await service.GetSummaryAsync(userId);

        Assert.Equal(500m, summary.TotalIncome);
        Assert.Equal(200m, summary.TotalExpenses);
        Assert.Equal(300m, summary.NetAmount);
        Assert.Equal(1300m, summary.TotalBalance);
        Assert.Equal(2, summary.TransactionCount);
    }

    [Fact]
    public async Task GetSpendingTrendAsync_ShouldReturnRequestedNumberOfMonths()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Transactions.AddRange(
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Income, Amount = 500m, Description = "Salary", TransactionDate = now },
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Expense, Amount = 200m, Description = "Food", TransactionDate = now.AddMonths(-1) }
        );
        await db.SaveChangesAsync();

        var service = new DashboardService(db, CreateCache());
        var trend = await service.GetSpendingTrendAsync(userId, 2);

        Assert.Equal(2, trend.Count);
        Assert.Contains(trend, x => x.Income == 500m);
        Assert.Contains(trend, x => x.Expense == 200m);
    }


    [Fact]
    public async Task GetSpendingTrendAsync_ShouldReadCachedTrendWithoutSerializationErrors()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Transactions.Add(new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Expense, Amount = 200m, Description = "Food", TransactionDate = now });
        await db.SaveChangesAsync();

        var service = new DashboardService(db, CreateCache());

        var first = await service.GetSpendingTrendAsync(userId, 2);
        var second = await service.GetSpendingTrendAsync(userId, 2);

        Assert.Equal(first.Count, second.Count);
        Assert.Contains(second, x => x.Expense == 200m);
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
