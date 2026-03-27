using System.Text.Json;
using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Finance;

public sealed class DashboardService(FinPilotDbContext dbContext, IDistributedCache cache) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetOrCreateAsync($"dashboard:summary:{userId}", async () =>
        {
            var now = DateTimeOffset.UtcNow;
            var transactions = await dbContext.Transactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate.Year == now.Year && x.TransactionDate.Month == now.Month).ToListAsync(cancellationToken);
            var accounts = await dbContext.Accounts.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(cancellationToken);

            var totalIncome = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
            var totalExpenses = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

            return new DashboardSummaryResponse
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                NetAmount = totalIncome - totalExpenses,
                TotalBalance = accounts.Sum(x => x.CurrentBalance),
                TransactionCount = transactions.Count
            };
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 12);
        return await GetOrCreateAsync($"dashboard:trend:{userId}:{months}", async () =>
        {
            var start = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-(months - 1));
            var transactions = await dbContext.Transactions.AsNoTracking().Where(x => x.UserId == userId && x.TransactionDate >= start).ToListAsync(cancellationToken);

            return Enumerable.Range(0, months).Select(offset =>
            {
                var monthDate = start.AddMonths(offset);
                var monthTransactions = transactions.Where(x => x.TransactionDate.Year == monthDate.Year && x.TransactionDate.Month == monthDate.Month);
                return new SpendingTrendPointResponse
                {
                    Year = monthDate.Year,
                    Month = monthDate.Month,
                    Label = monthDate.ToString("MMM yyyy"),
                    Income = monthTransactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
                    Expense = monthTransactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount)
                };
            }).ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetOrCreateAsync($"dashboard:breakdown:{userId}", async () =>
        {
            var now = DateTimeOffset.UtcNow;
            var transactions = await dbContext.Transactions.AsNoTracking().Include(x => x.Category).Where(x => x.UserId == userId && x.Type == TransactionType.Expense && x.TransactionDate.Year == now.Year && x.TransactionDate.Month == now.Month).ToListAsync(cancellationToken);
            var total = transactions.Sum(x => x.Amount);

            return transactions.GroupBy(x => new { x.CategoryId, Name = x.Category != null ? x.Category.Name : string.Empty })
                .Select(g => new CategoryBreakdownResponse
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(x => x.Amount),
                    Percentage = total <= 0 ? 0 : Math.Round((g.Sum(x => x.Amount) / total) * 100m, 2)
                })
                .OrderByDescending(x => x.Amount)
                .ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetOrCreateAsync($"dashboard:budget-health:{userId}", async () =>
        {
            var service = new BudgetService(dbContext);
            var items = await service.GetStatusesAsync(userId, cancellationToken);
            return items.ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetOrCreateAsync($"dashboard:goal-progress:{userId}", async () =>
        {
            var items = await dbContext.Goals.AsNoTracking().Where(x => x.UserId == userId)
                .OrderByDescending(x => x.TargetDate)
                .Select(x => new GoalProgressResponse
                {
                    GoalId = x.Id,
                    GoalName = x.Name,
                    CurrentAmount = x.CurrentAmount,
                    TargetAmount = x.TargetAmount,
                    ProgressPercent = x.TargetAmount <= 0 ? 0 : Math.Round((x.CurrentAmount / x.TargetAmount) * 100m, 2)
                }).ToListAsync(cancellationToken);
            return items;
        }, cancellationToken);
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var keys = new[]
        {
            $"dashboard:summary:{userId}",
            $"dashboard:breakdown:{userId}",
            $"dashboard:budget-health:{userId}",
            $"dashboard:goal-progress:{userId}"
        };

        var trendKeys = Enumerable.Range(1, 12).Select(months => $"dashboard:trend:{userId}:{months}");
        return Task.WhenAll(keys.Concat(trendKeys).Select(x => cache.RemoveAsync(x, cancellationToken)));
    }

    private async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var value = JsonSerializer.Deserialize<T>(cached);
            if (value is not null)
            {
                return value;
            }
        }

        var created = await factory();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(created), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, cancellationToken);

        return created;
    }
}
