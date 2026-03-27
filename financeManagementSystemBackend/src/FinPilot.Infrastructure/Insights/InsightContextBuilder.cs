using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Insights;

public sealed class InsightContextBuilder(FinPilotDbContext dbContext, IDashboardService dashboardService)
{
    public async Task<InsightContext> BuildAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
    {
        months = Math.Clamp(months, 1, 12);

        var summary = await dashboardService.GetSummaryAsync(userId, cancellationToken);
        var trend = await dashboardService.GetSpendingTrendAsync(userId, months, cancellationToken);
        var breakdown = await dashboardService.GetCategoryBreakdownAsync(userId, cancellationToken);
        var budgetHealth = await dashboardService.GetBudgetHealthAsync(userId, cancellationToken);

        var goals = await dbContext.Goals
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.TargetDate)
            .ThenBy(x => x.Name)
            .Select(x => new InsightGoalContext
            {
                GoalId = x.Id,
                GoalName = x.Name,
                CurrentAmount = x.CurrentAmount,
                TargetAmount = x.TargetAmount,
                ProgressPercent = x.TargetAmount <= 0 ? 0 : Math.Round((x.CurrentAmount / x.TargetAmount) * 100m, 2),
                TargetDate = x.TargetDate,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);

        var recentTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.UserId == userId && x.TransactionDate >= DateTimeOffset.UtcNow.AddDays(-90))
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new InsightRecentTransaction
            {
                TransactionId = x.Id,
                CategoryName = x.Category != null ? x.Category.Name : string.Empty,
                Type = x.Type,
                Amount = x.Amount,
                Description = x.Description,
                Merchant = x.Merchant,
                TransactionDate = x.TransactionDate
            })
            .ToListAsync(cancellationToken);

        return new InsightContext
        {
            UserId = userId,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Summary = summary,
            TrendPoints = trend,
            CategoryBreakdown = breakdown,
            BudgetHealth = budgetHealth,
            Goals = goals,
            RecentTransactions = recentTransactions
        };
    }
}

