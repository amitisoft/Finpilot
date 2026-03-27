using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.Interfaces.Agents;

public interface IAgentResultService
{
    Task<IReadOnlyCollection<AgentResultResponse>> GetResultsAsync(Guid userId, AgentType? agent = null, Guid? sourceEntityId = null, bool includeDismissed = false, CancellationToken cancellationToken = default);
    Task<AgentResultResponse?> GetLatestForTransactionAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);
    Task DismissAsync(Guid userId, Guid resultId, CancellationToken cancellationToken = default);
}
