using FinPilot.Domain.Common;
using FinPilot.Domain.Enums;

namespace FinPilot.Domain.Entities;

public sealed class AgentResult : AuditableEntity
{
    public Guid UserId { get; set; }
    public AgentType AgentType { get; set; }
    public AgentTrigger Trigger { get; set; }
    public AgentExecutionStatus Status { get; set; }
    public AgentSeverity Severity { get; set; }
    public string SourceEntityName { get; set; } = string.Empty;
    public Guid? SourceEntityId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDismissed { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }

    public User? User { get; set; }
}
