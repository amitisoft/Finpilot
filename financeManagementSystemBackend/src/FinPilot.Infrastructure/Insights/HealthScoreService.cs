using FinPilot.Application.DTOs.Insights;
using FinPilot.Application.Interfaces.Insights;
using FinPilot.Infrastructure.Agents;

namespace FinPilot.Infrastructure.Insights;

public sealed class HealthScoreService(FinancialCoachAgentService financialCoachAgentService) : IHealthScoreService
{
    public async Task<HealthScoreResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var coach = await financialCoachAgentService.AnalyzeAsync(userId, cancellationToken: cancellationToken);
        var breakdown = new List<HealthScoreBreakdownResponse>
        {
            BuildCashflow(coach),
            BuildSavings(coach),
            BuildSpending(coach),
            BuildBudget(coach),
            BuildGoals(coach)
        };

        var strengths = breakdown
            .Where(x => x.Status is "strong" or "stable")
            .Select(x => x.Summary)
            .Take(3)
            .ToList();

        var risks = breakdown
            .Where(x => x.Status is "watch" or "weak" or "incomplete")
            .Select(x => x.Summary)
            .Take(3)
            .ToList();

        return new HealthScoreResponse
        {
            Score = coach.HealthScore,
            Label = coach.HealthScore switch
            {
                >= 80 => "Excellent",
                >= 65 => "Stable",
                >= 50 => "Watchlist",
                _ => "Recovery mode"
            },
            Breakdown = breakdown,
            Strengths = strengths,
            Risks = risks,
            Suggestions = coach.Suggestions.Select(x => x.Action).Take(3).ToList(),
            GeneratedAt = coach.GeneratedAt
        };
    }

    private static HealthScoreBreakdownResponse BuildCashflow(FinPilot.Application.DTOs.Agents.CoachAnalysisResponse coach)
    {
        if (coach.TotalIncome <= 0)
        {
            return new HealthScoreBreakdownResponse
            {
                Category = "Cashflow visibility",
                Status = "incomplete",
                Summary = "Income has not been tracked consistently enough to benchmark true monthly cashflow."
            };
        }

        return new HealthScoreBreakdownResponse
        {
            Category = "Cashflow visibility",
            Status = coach.NetAmount >= 0 ? "strong" : "weak",
            Summary = coach.NetAmount >= 0
                ? $"Monthly inflows are currently ahead of outflows by {coach.NetAmount:0.##}."
                : $"Monthly outflows are ahead of inflows by {Math.Abs(coach.NetAmount):0.##}, which needs attention."
        };
    }

    private static HealthScoreBreakdownResponse BuildSavings(FinPilot.Application.DTOs.Agents.CoachAnalysisResponse coach)
    {
        if (coach.TotalIncome <= 0)
        {
            return new HealthScoreBreakdownResponse
            {
                Category = "Savings efficiency",
                Status = "incomplete",
                Summary = "Savings rate will become meaningful once recurring income is recorded."
            };
        }

        var status = coach.SavingsRatePercent switch
        {
            >= 20 => "strong",
            >= 10 => "stable",
            > 0 => "watch",
            _ => "weak"
        };

        return new HealthScoreBreakdownResponse
        {
            Category = "Savings efficiency",
            Status = status,
            Summary = $"Current savings rate is {coach.SavingsRatePercent:0.##}% of tracked income."
        };
    }

    private static HealthScoreBreakdownResponse BuildSpending(FinPilot.Application.DTOs.Agents.CoachAnalysisResponse coach)
    {
        if (string.IsNullOrWhiteSpace(coach.TopCategoryName))
        {
            return new HealthScoreBreakdownResponse
            {
                Category = "Spending concentration",
                Status = "incomplete",
                Summary = "Category data is still too thin to identify concentration risk."
            };
        }

        var status = coach.TopCategoryPercentage switch
        {
            <= 20 => "strong",
            <= 35 => "watch",
            _ => "weak"
        };

        return new HealthScoreBreakdownResponse
        {
            Category = "Spending concentration",
            Status = status,
            Summary = $"{coach.TopCategoryName} currently represents {coach.TopCategoryPercentage:0.##}% of tracked expenses."
        };
    }

    private static HealthScoreBreakdownResponse BuildBudget(FinPilot.Application.DTOs.Agents.CoachAnalysisResponse coach)
    {
        var status = coach.AtRiskBudgetCount switch
        {
            0 => "stable",
            1 => "watch",
            _ => "weak"
        };

        return new HealthScoreBreakdownResponse
        {
            Category = "Budget discipline",
            Status = status,
            Summary = coach.AtRiskBudgetCount == 0
                ? "No active budget appears to be over threshold right now."
                : $"{coach.AtRiskBudgetCount} active budget area(s) are already close to or above plan."
        };
    }

    private static HealthScoreBreakdownResponse BuildGoals(FinPilot.Application.DTOs.Agents.CoachAnalysisResponse coach)
    {
        if (string.IsNullOrWhiteSpace(coach.ActiveGoalName))
        {
            return new HealthScoreBreakdownResponse
            {
                Category = "Goal momentum",
                Status = "building",
                Summary = "No active goal is being tracked yet, so long-term progress is still forming."
            };
        }

        var status = coach.ActiveGoalProgressPercent switch
        {
            >= 60 => "strong",
            >= 30 => "stable",
            _ => "watch"
        };

        return new HealthScoreBreakdownResponse
        {
            Category = "Goal momentum",
            Status = status,
            Summary = $"{coach.ActiveGoalName} is {coach.ActiveGoalProgressPercent:0.##}% funded."
        };
    }
}