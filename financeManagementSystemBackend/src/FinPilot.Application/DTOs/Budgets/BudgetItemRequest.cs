using System.ComponentModel.DataAnnotations;

namespace FinPilot.Application.DTOs.Budgets;

public sealed class BudgetItemRequest
{
    public Guid CategoryId { get; init; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal LimitAmount { get; init; }
}
