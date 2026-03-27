using FinPilot.Domain.Common;
using FinPilot.Domain.Enums;

namespace FinPilot.Domain.Entities;

public sealed class Category : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }

    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
}
