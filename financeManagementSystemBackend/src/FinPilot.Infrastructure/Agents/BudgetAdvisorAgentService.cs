using FinPilot.Application.DTOs.Agents;
using FinPilot.Application.Interfaces.Budgets;

namespace FinPilot.Infrastructure.Agents;

public sealed class BudgetAdvisorAgentService(IBudgetService budgetService)
{
    public async Task<BudgetAdvisorAnalysisResponse> AnalyzeBudgetAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default)
    {
        var budget = await budgetService.GetByIdAsync(userId, budgetId, cancellationToken)
            ?? throw new InvalidOperationException("Budget not found for budget analysis.");

        var budgetMonthStart = new DateTimeOffset(budget.Year, budget.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var daysInMonth = DateTime.DaysInMonth(budget.Year, budget.Month);
        var monthEnd = budgetMonthStart.AddMonths(1).AddDays(-1);
        var now = DateTimeOffset.UtcNow;
        var referenceDate = now.Year == budget.Year && now.Month == budget.Month ? now : monthEnd;
        var elapsedDays = Math.Clamp(referenceDate.Day, 1, daysInMonth);
        var daysRemaining = Math.Max(daysInMonth - elapsedDays, 0);
        var projectedMultiplier = elapsedDays <= 0 ? 1m : daysInMonth / (decimal)elapsedDays;

        var overrunCategories = budget.Items
            .Select(item => new BudgetCategoryAlertResponse
            {
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                LimitAmount = item.LimitAmount,
                SpentAmount = item.SpentAmount,
                RemainingAmount = item.RemainingAmount,
                UsagePercent = item.UsagePercent,
                OverrunAmount = item.SpentAmount > item.LimitAmount ? item.SpentAmount - item.LimitAmount : 0,
                ProjectedMonthEnd = decimal.Round(item.SpentAmount * projectedMultiplier, 2)
            })
            .Where(item => item.UsagePercent >= budget.AlertThresholdPercent || item.OverrunAmount > 0)
            .OrderByDescending(item => item.OverrunAmount)
            .ThenByDescending(item => item.UsagePercent)
            .ToList();

        var safeToSpend = budget.Items
            .Select(item => new BudgetSafeToSpendResponse
            {
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                RemainingAmount = decimal.Round(Math.Max(item.RemainingAmount, 0), 2)
            })
            .OrderBy(item => item.RemainingAmount)
            .ToList();

        var status = budget.TotalSpent > budget.TotalLimit
            ? "over_budget"
            : budget.UsagePercent >= budget.AlertThresholdPercent
                ? "at_risk"
                : "on_track";

        var recommendations = new List<string>();
        if (status == "over_budget")
        {
            recommendations.Add("Freeze discretionary spending in the categories already over budget.");
            recommendations.Add("Move any surplus from non-essential categories only after reviewing must-pay bills.");
        }
        else if (status == "at_risk")
        {
            recommendations.Add("Slow spending in the lowest remaining categories until the month closes.");
            recommendations.Add("Review this budget again after the next major expense posts.");
        }
        else
        {
            recommendations.Add("Keep spending near the current pace to stay within this budget.");
            recommendations.Add("Use any unused room to strengthen next month's savings plan instead of increasing discretionary spend.");
        }

        if (overrunCategories.Count > 0)
        {
            recommendations.Add($"Prioritize {overrunCategories[0].CategoryName} first because it is putting the most pressure on the budget.");
        }

        return new BudgetAdvisorAnalysisResponse
        {
            BudgetId = budget.Id,
            BudgetName = budget.Name,
            Status = status,
            DaysRemainingInMonth = daysRemaining,
            TotalLimit = budget.TotalLimit,
            TotalSpent = budget.TotalSpent,
            RemainingAmount = budget.RemainingAmount,
            UsagePercent = budget.UsagePercent,
            OverrunCategories = overrunCategories,
            SafeToSpend = safeToSpend,
            Recommendations = recommendations,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
