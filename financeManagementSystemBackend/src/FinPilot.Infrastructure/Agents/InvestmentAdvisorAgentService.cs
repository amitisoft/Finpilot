using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Insights;

namespace FinPilot.Infrastructure.Agents;

public sealed class InvestmentAdvisorAgentService(InsightContextBuilder contextBuilder)
{
    public async Task<InvestmentAdvisorAnalysisResponse> AnalyzeAsync(Guid userId, string? riskProfile = null, int? age = null, CancellationToken cancellationToken = default)
    {
        var context = await contextBuilder.BuildAsync(userId, 6, cancellationToken);
        var normalizedRisk = NormalizeRiskProfile(riskProfile, age);
        var monthlySurplus = decimal.Round(Math.Max(context.Summary.NetAmount, 0m), 2);
        var monthlyExpenses = context.Summary.TotalExpenses;
        var emergencyCoverageMonths = monthlyExpenses <= 0 ? 0 : decimal.Round(context.Summary.TotalBalance / monthlyExpenses, 2);
        var goalPressure = context.Goals.Any(x => x.Status == GoalStatus.Active && x.ProgressPercent < 50);

        List<InvestmentAllocationSuggestionResponse> allocations;
        var priorityActions = new List<string>();
        var confidenceScore = 70;

        if (context.Summary.TotalIncome <= 0)
        {
            allocations =
            [
                new() { Bucket = "Emergency fund", Percentage = 70, Rationale = "Cashflow data is incomplete, so liquidity comes first until income is tracked consistently." },
                new() { Bucket = "Goal-ready cash", Percentage = 30, Rationale = "Keep funds flexible while you build a reliable income and spending history." }
            ];
            priorityActions.Add("Track salary and other recurring income before making long-term allocation decisions.");
            confidenceScore = 40;
        }
        else if (context.Summary.NetAmount <= 0)
        {
            allocations =
            [
                new() { Bucket = "Emergency fund", Percentage = 70, Rationale = "When monthly cashflow is negative, liquidity is more important than market exposure." },
                new() { Bucket = "Debt reduction / cash stabilization", Percentage = 30, Rationale = "Use remaining surplus windows to stabilize balances before taking investment risk." }
            ];
            priorityActions.Add("Bring monthly expenses below income before increasing investment exposure.");
            priorityActions.Add("Route any windfalls to emergency reserves first.");
            confidenceScore = 45;
        }
        else
        {
            allocations = BuildGrowthAllocations(normalizedRisk, emergencyCoverageMonths, goalPressure);
            priorityActions.Add($"Automate a monthly transfer of roughly {Math.Max(monthlySurplus * 0.4m, 100m):0.##} into your chosen allocation buckets.");
            if (emergencyCoverageMonths < 3)
            {
                priorityActions.Add("Build at least 3 months of essential expense coverage before increasing long-term growth exposure.");
                confidenceScore -= 10;
            }

            if (goalPressure)
            {
                priorityActions.Add("Keep near-term goal money in lower-volatility buckets until those goals are safely funded.");
                confidenceScore -= 5;
            }
        }

        if (!priorityActions.Any())
        {
            priorityActions.Add("Review your allocation quarterly instead of reacting to short-term swings.");
        }

        var reasoning = context.Summary.NetAmount > 0
            ? $"You are generating a monthly surplus of {context.Summary.NetAmount:0.##}, so the plan prioritizes a balance of safety and growth based on a {normalizedRisk} profile."
            : "Because current cashflow is tight or incomplete, the safest next move is to protect liquidity before increasing investment risk.";

        return new InvestmentAdvisorAnalysisResponse
        {
            Disclaimer = "This is informational guidance only and not licensed investment advice. Consult a certified financial adviser before making investment decisions.",
            RiskProfile = normalizedRisk,
            ConfidenceScore = Math.Clamp(confidenceScore, 30, 85),
            MonthlySurplus = monthlySurplus,
            AllocationSuggestions = allocations,
            PriorityActions = priorityActions.Distinct().Take(4).ToList(),
            Reasoning = reasoning,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    private static string NormalizeRiskProfile(string? riskProfile, int? age)
    {
        var normalized = riskProfile?.Trim().ToLowerInvariant();
        if (normalized is "conservative" or "moderate" or "aggressive")
        {
            return normalized;
        }

        return age switch
        {
            <= 30 => "moderate",
            >= 50 => "conservative",
            _ => "moderate"
        };
    }

    private static List<InvestmentAllocationSuggestionResponse> BuildGrowthAllocations(string riskProfile, decimal emergencyCoverageMonths, bool goalPressure)
    {
        var emergency = riskProfile switch
        {
            "conservative" => 40,
            "aggressive" => 20,
            _ => 30
        };
        var equity = riskProfile switch
        {
            "conservative" => 25,
            "aggressive" => 60,
            _ => 45
        };
        var fixedIncome = 100 - emergency - equity;

        if (emergencyCoverageMonths < 3)
        {
            emergency += 10;
            equity -= 10;
        }

        if (goalPressure)
        {
            fixedIncome += 5;
            equity -= 5;
        }

        return
        [
            new() { Bucket = "Emergency fund", Percentage = emergency, Rationale = "Keeps a liquidity buffer in place for unexpected expenses and near-term stability." },
            new() { Bucket = "Broad index equity", Percentage = equity, Rationale = "Targets long-term growth through diversified market exposure instead of concentrated picks." },
            new() { Bucket = "Fixed income / debt funds", Percentage = fixedIncome, Rationale = "Balances growth with lower-volatility capital for medium-term needs and goal protection." }
        ];
    }
}
