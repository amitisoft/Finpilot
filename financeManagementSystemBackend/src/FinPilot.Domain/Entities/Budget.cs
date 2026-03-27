using FinPilot.Domain.Common;

namespace FinPilot.Domain.Entities;

public sealed class Budget : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalLimit { get; set; }
    public int AlertThresholdPercent { get; set; } = 80;

    public User? User { get; set; }
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
}
