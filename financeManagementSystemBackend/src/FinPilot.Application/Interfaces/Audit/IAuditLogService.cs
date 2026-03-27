using FinPilot.Application.DTOs.Audit;

namespace FinPilot.Application.Interfaces.Audit;

public interface IAuditLogService
{
    Task WriteAsync(Guid? userId, string entityName, Guid entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditLogResponse>> GetRecentAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default);
}
