using FinPilot.Domain.Common;
using FinPilot.Domain.Enums;

namespace FinPilot.Domain.Entities;

public sealed class Transaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public Guid CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset TransactionDate { get; set; }
    public string? Merchant { get; set; }
    public string? Notes { get; set; }

    public User? User { get; set; }
    public Account? Account { get; set; }
    public Category? Category { get; set; }
}
