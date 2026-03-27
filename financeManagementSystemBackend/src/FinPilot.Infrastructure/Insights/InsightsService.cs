using FinPilot.Application.DTOs.Insights;
using FinPilot.Application.Interfaces.Insights;
using FinPilot.Domain.Enums;

namespace FinPilot.Infrastructure.Insights;

public sealed class InsightsService(InsightContextBuilder contextBuilder) : IInsightsService
{
    public async Task<InsightBundleResponse> GetMonthlyInsightsAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, months, cancellationToken);
        var cards = new List<InsightCardResponse>();

        if (context.Summary.TotalExpenses > context.Summary.TotalIncome)
        {
            cards.Add(CreateCard(
                "Expenses exceeded income",
                "warning",
                "high",
                $"This month expenses ({FormatMoney(context.Summary.TotalExpenses)}) are above income ({FormatMoney(context.Summary.TotalIncome)}).",
                ["Review non-essential spending this week", "Open the budget-risk insights and focus on top categories"]));
        }
        else if (context.Summary.TotalIncome > 0)
        {
            cards.Add(CreateCard(
                "Positive monthly cashflow",
                "positive",
                "medium",
                $"You are net positive by {FormatMoney(context.Summary.NetAmount)} this month.",
                ["Allocate part of the surplus toward goals", "Check if high-growth categories still need tightening"]));
        }

        var lastTwoMonths = context.TrendPoints.OrderBy(x => x.Year).ThenBy(x => x.Month).TakeLast(2).ToArray();
        if (lastTwoMonths.Length == 2)
        {
            var previousExpense = lastTwoMonths[0].Expense;
            var currentExpense = lastTwoMonths[1].Expense;
            if (previousExpense > 0)
            {
                var changePercent = Math.Round(((currentExpense - previousExpense) / previousExpense) * 100m, 2);
                if (Math.Abs(changePercent) >= 10)
                {
                    var type = changePercent > 0 ? "warning" : "positive";
                    var priority = changePercent > 20 ? "high" : "medium";
                    cards.Add(CreateCard(
                        "Monthly expense trend changed",
                        type,
                        priority,
                        $"Expenses changed by {changePercent}% compared with last month.",
                        changePercent > 0
                            ? ["Check the category breakdown for the main driver", "Review large transactions from the last 30 days"]
                            : ["Consider moving some of the savings toward goals", "Keep monitoring whether the drop is sustainable"]));
                }
            }
        }

        var topCategory = context.CategoryBreakdown.OrderByDescending(x => x.Amount).FirstOrDefault();
        if (topCategory is not null)
        {
            cards.Add(CreateCard(
                "Top spending category",
                "info",
                "medium",
                $"{topCategory.CategoryName} is currently the highest expense category at {FormatMoney(topCategory.Amount)} ({topCategory.Percentage}% of tracked expense spend).",
                ["Set a category-specific weekly cap", "Review recent transactions in this category for avoidable spend"]));
        }

        if (cards.Count == 0)
        {
            cards.Add(CreateCard(
                "Not enough monthly data yet",
                "info",
                "low",
                "Add a few more income and expense transactions to unlock richer month-level insights.",
                ["Create at least one income transaction", "Add budget items for major expense categories"]));
        }

        return new InsightBundleResponse
        {
            Headline = cards[0].Summary,
            Cards = cards,
            GeneratedAt = context.GeneratedAtUtc
        };
    }

    public async Task<InsightBundleResponse> GetBudgetRiskInsightsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, cancellationToken: cancellationToken);
        var cards = new List<InsightCardResponse>();
        var overBudget = context.BudgetHealth.Where(x => x.IsOverBudget).ToList();
        var thresholdReached = context.BudgetHealth.Where(x => !x.IsOverBudget && x.ThresholdReached).ToList();

        foreach (var item in overBudget)
        {
            cards.Add(CreateCard(
                $"{item.BudgetName} is over budget",
                "warning",
                "high",
                $"Spent {FormatMoney(item.TotalSpent)} against a limit of {FormatMoney(item.TotalLimit)}.",
                ["Pause discretionary spend in this budget", "Review the highest expense category contributing to the overrun"]));
        }

        foreach (var item in thresholdReached)
        {
            cards.Add(CreateCard(
                $"{item.BudgetName} is approaching its limit",
                "warning",
                "medium",
                $"Usage is at {item.UsagePercent}% with {FormatMoney(item.RemainingAmount)} remaining.",
                ["Reduce spend in the top category for this budget", "Delay low-priority purchases until next cycle"]));
        }

        if (cards.Count == 0)
        {
            cards.Add(CreateCard(
                "Budgets are under control",
                "positive",
                "low",
                "No active budget is currently over its threshold.",
                ["Keep checking category-level variance weekly", "Use monthly insights to tighten future limits"]));
        }

        return new InsightBundleResponse
        {
            Headline = cards[0].Summary,
            Cards = cards,
            GeneratedAt = context.GeneratedAtUtc
        };
    }

    public async Task<InsightBundleResponse> GetAnomalyInsightsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, cancellationToken: cancellationToken);
        var cards = new List<InsightCardResponse>();
        var recentExpenses = context.RecentTransactions.Where(x => x.Type == TransactionType.Expense).ToList();

        var duplicateGroups = recentExpenses
            .Where(x => !string.IsNullOrWhiteSpace(x.Merchant))
            .GroupBy(x => new { Merchant = x.Merchant!.Trim().ToLowerInvariant(), x.Amount })
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var group in duplicateGroups.Take(3))
        {
            cards.Add(CreateCard(
                "Possible duplicate or recurring charge",
                "warning",
                "medium",
                $"{group.Count()} transactions of {FormatMoney(group.Key.Amount)} were found for {group.Key.Merchant} in the last 90 days.",
                ["Check whether this is an intended subscription", "Review the transaction dates for accidental repeat payments"]));
        }

        var averageExpense = recentExpenses.Count == 0 ? 0 : recentExpenses.Average(x => x.Amount);
        var spikeThreshold = Math.Max(5000m, averageExpense * 2);
        foreach (var spike in recentExpenses.Where(x => x.Amount >= spikeThreshold).OrderByDescending(x => x.Amount).Take(3))
        {
            cards.Add(CreateCard(
                "Large expense spike detected",
                "warning",
                "high",
                $"A recent expense of {FormatMoney(spike.Amount)} in {spike.CategoryName} looks unusually large.",
                ["Confirm the merchant and category are correct", "If unexpected, flag it for user review immediately"]));
        }

        if (cards.Count == 0)
        {
            cards.Add(CreateCard(
                "No strong anomalies detected",
                "positive",
                "low",
                "Recent transactions do not show obvious duplicate or spike patterns.",
                ["Continue reviewing large new merchants weekly", "Keep transaction descriptions specific for better anomaly checks"]));
        }

        return new InsightBundleResponse
        {
            Headline = cards[0].Summary,
            Cards = cards,
            GeneratedAt = context.GeneratedAtUtc
        };
    }

    public async Task<InsightBundleResponse> GetGoalInsightsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, cancellationToken: cancellationToken);
        var cards = new List<InsightCardResponse>();
        var now = DateTimeOffset.UtcNow;

        foreach (var goal in context.Goals.Where(x => x.Status == GoalStatus.Completed).Take(2))
        {
            cards.Add(CreateCard(
                $"Goal completed: {goal.GoalName}",
                "positive",
                "medium",
                $"This goal has reached 100% of its target value.",
                ["Celebrate the milestone", "Create the next savings goal to keep momentum"]));
        }

        foreach (var goal in context.Goals.Where(x => x.Status == GoalStatus.Active && x.ProgressPercent >= 80).Take(2))
        {
            cards.Add(CreateCard(
                $"Goal is close: {goal.GoalName}",
                "positive",
                "medium",
                $"{goal.ProgressPercent}% of the target has been achieved.",
                ["Schedule one more contribution this month", "Use any positive monthly net amount to close the gap"]));
        }

        foreach (var goal in context.Goals.Where(x => x.Status == GoalStatus.Active && x.TargetDate.HasValue && x.TargetDate.Value <= now.AddDays(90) && x.ProgressPercent < 50).Take(2))
        {
            cards.Add(CreateCard(
                $"Goal may be behind pace: {goal.GoalName}",
                "warning",
                "high",
                $"The target date is within 90 days and progress is only {goal.ProgressPercent}%.",
                ["Increase the monthly contribution toward this goal", "Reduce discretionary categories temporarily to protect this target"]));
        }

        if (cards.Count == 0)
        {
            cards.Add(CreateCard(
                "Goals are stable",
                "info",
                "low",
                "No urgent goal risks were detected from current progress data.",
                ["Keep monthly contributions consistent", "Revisit goal dates if priorities changed"]));
        }

        return new InsightBundleResponse
        {
            Headline = cards[0].Summary,
            Cards = cards,
            GeneratedAt = context.GeneratedAtUtc
        };
    }

    private static InsightCardResponse CreateCard(string title, string type, string priority, string summary, IReadOnlyCollection<string> recommendations)
    {
        return new InsightCardResponse
        {
            Title = title,
            Type = type,
            Priority = priority,
            Summary = summary,
            Recommendations = recommendations
        };
    }

    private static string FormatMoney(decimal amount) => amount.ToString("0.##");
}
