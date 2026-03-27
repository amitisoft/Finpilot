using FinPilot.Domain.Common;
using FinPilot.Domain.Enums;

namespace FinPilot.Domain.Entities;

public sealed class Account : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }

    public User? User { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
