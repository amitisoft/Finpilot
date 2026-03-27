using System.Text.Json;
using FinPilot.Application.DTOs.Audit;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Domain.Entities;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Services;

public sealed class AuditLogService(FinPilotDbContext dbContext) : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(Guid? userId, string entityName, Guid entityId, string action, object? oldValues = null, object? newValues = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions)
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLogResponse>> GetRecentAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
