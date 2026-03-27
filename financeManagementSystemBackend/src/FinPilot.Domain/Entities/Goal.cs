using FinPilot.Domain.Common;
using FinPilot.Domain.Enums;

namespace FinPilot.Domain.Entities;

public sealed class Goal : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTimeOffset? TargetDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public User? User { get; set; }
}
