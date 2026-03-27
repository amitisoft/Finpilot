using FinPilot.Domain.Common;

namespace FinPilot.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    public User? User { get; set; }
}
