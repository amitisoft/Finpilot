namespace FinPilot.Application.DTOs.Agents;

public sealed class BudgetAdvisorAnalysisResponse
{
    public Guid BudgetId { get; init; }
    public string BudgetName { get; init; } = string.Empty;
    public string Status { get; init; } = "on_track";
    public int DaysRemainingInMonth { get; init; }
    public decimal TotalLimit { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal UsagePercent { get; init; }
    public IReadOnlyCollection<BudgetCategoryAlertResponse> OverrunCategories { get; init; } = Array.Empty<BudgetCategoryAlertResponse>();
    public IReadOnlyCollection<BudgetSafeToSpendResponse> SafeToSpend { get; init; } = Array.Empty<BudgetSafeToSpendResponse>();
    public IReadOnlyCollection<string> Recommendations { get; init; } = Array.Empty<string>();
    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed class BudgetCategoryAlertResponse
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal LimitAmount { get; init; }
    public decimal SpentAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal UsagePercent { get; init; }
    public decimal OverrunAmount { get; init; }
    public decimal ProjectedMonthEnd { get; init; }
}

public sealed class BudgetSafeToSpendResponse
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal RemainingAmount { get; init; }
}
