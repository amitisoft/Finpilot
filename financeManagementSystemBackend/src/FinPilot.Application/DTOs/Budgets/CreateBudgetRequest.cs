using System.ComponentModel.DataAnnotations;

namespace FinPilot.Application.DTOs.Budgets;

public sealed class CreateBudgetRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 12)]
    public int Month { get; init; }

    [Range(2000, 2100)]
    public int Year { get; init; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal TotalLimit { get; init; }

    [Range(1, 100)]
    public int AlertThresholdPercent { get; init; } = 80;

    [MinLength(1)]
    public IReadOnlyCollection<BudgetItemRequest> Items { get; init; } = Array.Empty<BudgetItemRequest>();
}
