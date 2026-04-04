using System.Text;
using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Insights;

namespace FinPilot.Infrastructure.Agents;

public sealed class ReportGeneratorAgentService(InsightContextBuilder contextBuilder)
{
    public async Task<ReportGeneratorAnalysisResponse> AnalyzeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, 6, cancellationToken);
        return Analyze(context);
    }

    public ReportGeneratorAnalysisResponse Analyze(InsightContext context)
    {
        var highlights = new List<string>
        {
            $"Income this month: {context.Summary.TotalIncome:0.##}",
            $"Expenses this month: {context.Summary.TotalExpenses:0.##}",
            $"Net cashflow: {context.Summary.NetAmount:0.##}"
        };

        var topCategory = context.CategoryBreakdown.OrderByDescending(x => x.Amount).FirstOrDefault();
        if (topCategory is not null)
        {
            highlights.Add($"Top expense category: {topCategory.CategoryName} at {topCategory.Amount:0.##}");
        }

        var activeGoal = context.Goals.Where(x => x.Status == GoalStatus.Active).OrderByDescending(x => x.ProgressPercent).FirstOrDefault();
        if (activeGoal is not null)
        {
            highlights.Add($"Lead goal: {activeGoal.GoalName} is {activeGoal.ProgressPercent:0.##}% funded");
        }

        var averageExpense = context.TrendPoints.Any() ? decimal.Round(context.TrendPoints.Average(x => x.Expense), 2) : context.Summary.TotalExpenses;
        var averageIncome = context.TrendPoints.Any() ? decimal.Round(context.TrendPoints.Average(x => x.Income), 2) : context.Summary.TotalIncome;
        var forecast = averageIncome > 0
            ? $"At the recent pace, next month's net cashflow could be about {averageIncome - averageExpense:0.##}."
            : "Track at least one full income cycle to unlock a more reliable monthly forecast.";

        var builder = new StringBuilder();
        builder.AppendLine("# Monthly Financial Report");
        builder.AppendLine();
        builder.AppendLine("## Cashflow");
        builder.AppendLine($"- Total income: {context.Summary.TotalIncome:0.##}");
        builder.AppendLine($"- Total expenses: {context.Summary.TotalExpenses:0.##}");
        builder.AppendLine($"- Net amount: {context.Summary.NetAmount:0.##}");
        builder.AppendLine();
        builder.AppendLine("## Spending Focus");
        builder.AppendLine(topCategory is null
            ? "- Not enough category data yet to identify a dominant spending area."
            : $"- {topCategory.CategoryName} is currently the top expense bucket at {topCategory.Amount:0.##} ({topCategory.Percentage:0.##}% of expenses).");
        builder.AppendLine();
        builder.AppendLine("## Budget Health");
        if (context.BudgetHealth.Any())
        {
            foreach (var budget in context.BudgetHealth.Take(3))
            {
                builder.AppendLine($"- {budget.BudgetName}: spent {budget.TotalSpent:0.##} / {budget.TotalLimit:0.##} ({budget.UsagePercent:0.##}%).");
            }
        }
        else
        {
            builder.AppendLine("- No active budgets found for the current period.");
        }

        builder.AppendLine();
        builder.AppendLine("## Goals");
        if (context.Goals.Any())
        {
            foreach (var goal in context.Goals.Take(3))
            {
                builder.AppendLine($"- {goal.GoalName}: {goal.ProgressPercent:0.##}% progress toward {goal.TargetAmount:0.##}.");
            }
        }
        else
        {
            builder.AppendLine("- No active goals found.");
        }

        builder.AppendLine();
        builder.AppendLine("## Forecast");
        builder.AppendLine($"- {forecast}");

        var title = $"FinPilot Report - {DateTimeOffset.UtcNow:MMMM yyyy}";
        var summary = context.Summary.NetAmount >= 0
            ? $"You finished the current month with a positive net cashflow of {context.Summary.NetAmount:0.##}."
            : $"You are currently running a negative net cashflow of {context.Summary.NetAmount:0.##}; review the top expense drivers soon.";

        return new ReportGeneratorAnalysisResponse
        {
            Title = title,
            Summary = summary,
            Highlights = highlights.Take(5).ToList(),
            MarkdownReport = builder.ToString().Trim(),
            Forecast = forecast,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
