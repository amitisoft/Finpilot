using FinPilot.Application.DTOs.Agents;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Agents;

public sealed class AgentResultService(FinPilotDbContext dbContext) : IAgentResultService
{
    public async Task<IReadOnlyCollection<AgentResultResponse>> GetResultsAsync(Guid userId, AgentType? agent = null, Guid? sourceEntityId = null, bool includeDismissed = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AgentResults.AsNoTracking().Where(x => x.UserId == userId);

        if (agent.HasValue)
        {
            query = query.Where(x => x.AgentType == agent.Value);
        }

        if (sourceEntityId.HasValue)
        {
            query = query.Where(x => x.SourceEntityId == sourceEntityId.Value);
        }

        if (!includeDismissed)
        {
            query = query.Where(x => !x.IsDismissed);
        }

        var items = await query.OrderByDescending(x => x.GeneratedAt).Take(50).ToListAsync(cancellationToken);
        return items.Select(AgentResultMappings.ToResponse).ToList();
    }

    public async Task<AgentResultResponse?> GetLatestForTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.AgentResults.AsNoTracking()
            .Where(x => x.UserId == userId && x.SourceEntityName == "transaction" && x.SourceEntityId == transactionId)
            .OrderByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? null : AgentResultMappings.ToResponse(item);
    }

    public async Task DismissAsync(Guid userId, Guid resultId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.AgentResults.FirstOrDefaultAsync(x => x.Id == resultId && x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Agent result not found.");
        item.IsDismissed = true;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
