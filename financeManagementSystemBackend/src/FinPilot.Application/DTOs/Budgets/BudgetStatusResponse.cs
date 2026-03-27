namespace FinPilot.Application.DTOs.Budgets;

public sealed class BudgetStatusResponse
{
    public Guid BudgetId { get; init; }
    public string BudgetName { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Year { get; init; }
    public decimal TotalLimit { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal UsagePercent { get; init; }
    public bool IsOverBudget { get; init; }
    public bool ThresholdReached { get; init; }
}
