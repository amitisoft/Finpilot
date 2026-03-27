namespace FinPilot.Application.DTOs.Audit;

public sealed class AuditLogResponse
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
