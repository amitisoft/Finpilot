using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.Interfaces.Agents;

public interface IAgentOrchestratorService
{
    Task QueueTransactionAnomalyCheckAsync(Guid userId, Guid transactionId, AgentTrigger trigger, CancellationToken cancellationToken = default);
    Task QueueBudgetCheckAsync(Guid userId, Guid budgetId, AgentTrigger trigger, CancellationToken cancellationToken = default);
    Task QueueBudgetChecksForPeriodAsync(Guid userId, int month, int year, AgentTrigger trigger, CancellationToken cancellationToken = default);
    Task<AgentInvocationResponse> InvokeAsync(Guid userId, InvokeAgentRequest request, CancellationToken cancellationToken = default);
    Task<AgentChatResponse> ChatAsync(Guid userId, AgentChatRequest request, CancellationToken cancellationToken = default);
}
