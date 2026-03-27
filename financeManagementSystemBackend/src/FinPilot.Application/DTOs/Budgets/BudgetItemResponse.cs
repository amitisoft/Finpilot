namespace FinPilot.Application.DTOs.Budgets;

public sealed class BudgetItemResponse
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal LimitAmount { get; init; }
    public decimal SpentAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal UsagePercent { get; init; }
}
