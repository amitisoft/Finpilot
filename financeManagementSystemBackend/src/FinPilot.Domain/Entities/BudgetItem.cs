using FinPilot.Domain.Common;

namespace FinPilot.Domain.Entities;

public sealed class BudgetItem : AuditableEntity
{
    public Guid BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }

    public Budget? Budget { get; set; }
    public Category? Category { get; set; }
}
