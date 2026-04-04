using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Forecast;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Forecast;

public sealed class ForecastServiceTests
{
    [Fact]
    public async Task GetMonthlyForecastAsync_ShouldProjectRemainingNetFromRecentHistory()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Accounts.Add(new Account
        {
            UserId = userId,
            Name = "Main",
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalance = 10000m,
            CurrentBalance = 13000m
        });
        db.Transactions.AddRange(
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Income, Amount = 5000m, Description = "Salary", TransactionDate = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero) },
            new Transaction { UserId = userId, AccountId = Guid.NewGuid(), CategoryId = Guid.NewGuid(), Type = TransactionType.Expense, Amount = 2000m, Description = "Bills", TransactionDate = new DateTimeOffset(2026, 3, 12, 0, 0, 0, TimeSpan.Zero) }
        );
        await db.SaveChangesAsync();

        var service = new ForecastService(db, new FakeDateTimeProvider(new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero)));
        var result = await service.GetMonthlyForecastAsync(userId);

        Assert.Equal(13000m, result.CurrentBalance);
        Assert.Equal(11, result.DaysRemaining);
        Assert.True(result.ProjectedEndOfMonthBalance > result.CurrentBalance);
        Assert.Contains(result.Assumptions, x => x.Contains("average daily net cashflow", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDailyForecastAsync_ShouldMarkFuturePointsAsProjected()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Accounts.Add(new Account
        {
            UserId = userId,
            Name = "Main",
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalance = 1000m,
            CurrentBalance = 1500m
        });
        db.Transactions.Add(new Transaction
        {
            UserId = userId,
            AccountId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Type = TransactionType.Income,
            Amount = 500m,
            Description = "Freelance",
            TransactionDate = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero)
        });
        await db.SaveChangesAsync();

        var now = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero);
        var service = new ForecastService(db, new FakeDateTimeProvider(now));
        var result = await service.GetDailyForecastAsync(userId);

        Assert.Equal(31, result.Count);
        Assert.All(result.Where(x => x.Date > now.Date), x => Assert.True(x.IsProjected));
        Assert.All(result.Where(x => x.Date <= now.Date), x => Assert.False(x.IsProjected));
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDateTimeProvider(DateTimeOffset utcNow) : FinPilot.Application.Interfaces.IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}