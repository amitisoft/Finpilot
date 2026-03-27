using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Insights;

namespace FinPilot.Infrastructure.Agents;

public sealed class FinancialCoachAgentService(InsightContextBuilder contextBuilder)
{
    public async Task<CoachAnalysisResponse> AnalyzeAsync(Guid userId, string? userQuestion = null, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, 3, cancellationToken);
        var patterns = new List<CoachBehaviorPatternResponse>();
        var suggestions = new List<CoachSuggestionResponse>();
        var healthScore = 65;
        var savingsRate = 0m;

        if (context.Summary.TotalIncome > 0)
        {
            savingsRate = decimal.Round((context.Summary.NetAmount / context.Summary.TotalIncome) * 100m, 2);
            if (savingsRate >= 20)
            {
                patterns.Add(new CoachBehaviorPatternResponse
                {
                    Pattern = "Strong monthly surplus",
                    Impact = "high",
                    Description = $"You are keeping roughly {savingsRate}% of income after expenses this month."
                });
                suggestions.Add(new CoachSuggestionResponse
                {
                    Title = "Protect your surplus",
                    Action = "Move part of this month's surplus directly into a goal or emergency fund before discretionary spending expands.",
                    ExpectedMonthlyImpact = decimal.Round(context.Summary.NetAmount * 0.35m, 2)
                });
                healthScore += 20;
            }
            else if (savingsRate > 0)
            {
                patterns.Add(new CoachBehaviorPatternResponse
                {
                    Pattern = "Positive but thin cushion",
                    Impact = "medium",
                    Description = $"You are still positive this month, but the leftover margin is only {savingsRate}% of income."
                });
                suggestions.Add(new CoachSuggestionResponse
                {
                    Title = "Create a wider buffer",
                    Action = "Trim one non-essential category this month so a larger share of income stays available for goals.",
                    ExpectedMonthlyImpact = Math.Max(decimal.Round(context.Summary.TotalExpenses * 0.05m, 2), 100m)
                });
                healthScore += 8;
            }
            else
            {
                patterns.Add(new CoachBehaviorPatternResponse
                {
                    Pattern = "Negative monthly cashflow",
                    Impact = "high",
                    Description = "Expenses are currently outrunning income, which can quickly pressure savings and balances."
                });
                suggestions.Add(new CoachSuggestionResponse
                {
                    Title = "Stop the monthly leak first",
                    Action = "Pause discretionary categories and review the top spending driver before setting new savings targets.",
                    ExpectedMonthlyImpact = Math.Max(decimal.Round(Math.Abs(context.Summary.NetAmount), 2), 100m)
                });
                healthScore -= 18;
            }
        }
        else
        {
            patterns.Add(new CoachBehaviorPatternResponse
            {
                Pattern = "Income data is incomplete",
                Impact = "medium",
                Description = "The system cannot measure your true savings rate until income transactions are tracked consistently."
            });
            suggestions.Add(new CoachSuggestionResponse
            {
                Title = "Track income consistently",
                Action = "Log salary and other income sources so guidance can compare spending against actual inflows.",
                ExpectedMonthlyImpact = 0m
            });
            healthScore -= 10;
        }

        var topCategory = context.CategoryBreakdown.OrderByDescending(x => x.Amount).FirstOrDefault();
        if (topCategory is not null)
        {
            patterns.Add(new CoachBehaviorPatternResponse
            {
                Pattern = "Concentrated discretionary pressure",
                Impact = topCategory.Percentage >= 35 ? "high" : "medium",
                Description = $"{topCategory.CategoryName} is taking {topCategory.Percentage}% of this month's expense spend."
            });
            suggestions.Add(new CoachSuggestionResponse
            {
                Title = $"Cap {topCategory.CategoryName}",
                Action = $"Set a weekly cap for {topCategory.CategoryName} and review purchases before the weekend or other trigger periods.",
                ExpectedMonthlyImpact = Math.Max(decimal.Round(topCategory.Amount * 0.12m, 2), 50m)
            });
            healthScore -= topCategory.Percentage >= 35 ? 8 : 3;
        }

        var atRiskBudgets = context.BudgetHealth.Where(x => x.IsOverBudget || x.ThresholdReached).ToList();
        if (atRiskBudgets.Count > 0)
        {
            patterns.Add(new CoachBehaviorPatternResponse
            {
                Pattern = "Budget pressure is building",
                Impact = atRiskBudgets.Any(x => x.IsOverBudget) ? "high" : "medium",
                Description = $"{atRiskBudgets.Count} budget area(s) are already over plan or close to the warning threshold."
            });
            suggestions.Add(new CoachSuggestionResponse
            {
                Title = "Protect essential categories",
                Action = "Before making cuts, lock in upcoming essentials and then reduce the categories already above threshold.",
                ExpectedMonthlyImpact = Math.Max(decimal.Round(atRiskBudgets.Sum(x => x.RemainingAmount < 0 ? Math.Abs(x.RemainingAmount) : x.TotalLimit * 0.05m), 2), 75m)
            });
            healthScore -= atRiskBudgets.Any(x => x.IsOverBudget) ? 10 : 5;
        }

        var activeGoal = context.Goals.Where(x => x.Status == GoalStatus.Active).OrderByDescending(x => x.ProgressPercent).FirstOrDefault();
        if (activeGoal is not null)
        {
            patterns.Add(new CoachBehaviorPatternResponse
            {
                Pattern = "Goal progress exists",
                Impact = activeGoal.ProgressPercent >= 50 ? "medium" : "low",
                Description = $"{activeGoal.GoalName} is currently at {activeGoal.ProgressPercent}% progress."
            });
            suggestions.Add(new CoachSuggestionResponse
            {
                Title = $"Fund {activeGoal.GoalName} automatically",
                Action = "Use your next positive cashflow window to make a scheduled contribution instead of waiting for leftovers at month end.",
                ExpectedMonthlyImpact = Math.Max(decimal.Round((activeGoal.TargetAmount - activeGoal.CurrentAmount) / 6m, 2), 100m)
            });
            healthScore += activeGoal.ProgressPercent >= 50 ? 5 : 2;
        }

        if (patterns.Count < 3)
        {
            patterns.Add(new CoachBehaviorPatternResponse
            {
                Pattern = "Still building your history",
                Impact = "low",
                Description = "A few more weeks of consistent tracking will unlock stronger behaviour patterns."
            });
        }

        if (suggestions.Count < 3)
        {
            suggestions.Add(new CoachSuggestionResponse
            {
                Title = "Review spending weekly",
                Action = "Spend five minutes each week checking large transactions so habits are corrected before month end.",
                ExpectedMonthlyImpact = 0m
            });
        }

        healthScore = Math.Clamp(healthScore, 25, 95);
        var encouragement = healthScore switch
        {
            >= 80 => "You already have strong habits—small, consistent adjustments can accelerate your goals.",
            >= 65 => "You're on a workable path; tightening one or two categories can noticeably improve your monthly cushion.",
            >= 50 => "You're not far from a healthier rhythm; focus on a single spending habit and build momentum from there.",
            _ => "The good news is that even one focused change this month can quickly stabilize your finances."
        };

        if (!string.IsNullOrWhiteSpace(userQuestion) && userQuestion.Contains("food", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Insert(0, new CoachSuggestionResponse
            {
                Title = "Audit food spending specifically",
                Action = "Compare weekday essentials versus weekend or convenience purchases to see where food spending rises fastest.",
                ExpectedMonthlyImpact = topCategory?.CategoryName.Contains("food", StringComparison.OrdinalIgnoreCase) == true ? Math.Max(decimal.Round(topCategory.Amount * 0.15m, 2), 100m) : 100m
            });
        }

        return new CoachAnalysisResponse
        {
            HealthScore = healthScore,
            TotalIncome = context.Summary.TotalIncome,
            TotalExpenses = context.Summary.TotalExpenses,
            NetAmount = context.Summary.NetAmount,
            SavingsRatePercent = savingsRate,
            TopCategoryName = topCategory?.CategoryName,
            TopCategoryAmount = topCategory?.Amount ?? 0m,
            TopCategoryPercentage = topCategory?.Percentage ?? 0m,
            AtRiskBudgetCount = atRiskBudgets.Count,
            ActiveGoalName = activeGoal?.GoalName,
            ActiveGoalProgressPercent = activeGoal?.ProgressPercent ?? 0m,
            BehavioralPatterns = patterns.Take(3).ToList(),
            Suggestions = suggestions.Take(3).ToList(),
            Encouragement = encouragement,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }
}
