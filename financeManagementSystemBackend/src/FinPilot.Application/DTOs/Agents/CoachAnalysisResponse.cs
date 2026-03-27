namespace FinPilot.Application.DTOs.Agents;

public sealed class CoachAnalysisResponse
{
    public int HealthScore { get; init; }
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetAmount { get; init; }
    public decimal SavingsRatePercent { get; init; }
    public string? TopCategoryName { get; init; }
    public decimal TopCategoryAmount { get; init; }
    public decimal TopCategoryPercentage { get; init; }
    public int AtRiskBudgetCount { get; init; }
    public string? ActiveGoalName { get; init; }
    public decimal ActiveGoalProgressPercent { get; init; }
    public IReadOnlyCollection<CoachBehaviorPatternResponse> BehavioralPatterns { get; init; } = Array.Empty<CoachBehaviorPatternResponse>();
    public IReadOnlyCollection<CoachSuggestionResponse> Suggestions { get; init; } = Array.Empty<CoachSuggestionResponse>();
    public string Encouragement { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed class CoachBehaviorPatternResponse
{
    public string Pattern { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class CoachSuggestionResponse
{
    public string Title { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public decimal ExpectedMonthlyImpact { get; init; }
}
