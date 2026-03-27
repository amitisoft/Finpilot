namespace FinPilot.Application.DTOs.Budgets;

public sealed class BudgetResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Year { get; init; }
    public decimal TotalLimit { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal RemainingAmount { get; init; }
    public int AlertThresholdPercent { get; init; }
    public decimal UsagePercent { get; init; }
    public IReadOnlyCollection<BudgetItemResponse> Items { get; init; } = Array.Empty<BudgetItemResponse>();
}
