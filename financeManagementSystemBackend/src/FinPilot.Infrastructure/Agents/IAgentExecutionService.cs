using FinPilot.Application.DTOs.Agents;
using FinPilot.Domain.Enums;

namespace FinPilot.Infrastructure.Agents;

internal interface IAgentExecutionService
{
    Task<AgentResultResponse> ExecuteAnomalyAsync(Guid userId, Guid transactionId, AgentTrigger trigger, Guid? existingResultId, CancellationToken cancellationToken = default);
    Task<AgentResultResponse> ExecuteBudgetAsync(Guid userId, Guid budgetId, AgentTrigger trigger, Guid? existingResultId, CancellationToken cancellationToken = default);
    Task<AgentResultResponse> ExecuteCoachAsync(Guid userId, AgentTrigger trigger, Guid? existingResultId, string? userQuestion = null, CancellationToken cancellationToken = default);
    Task<AgentResultResponse> ExecuteInvestmentAsync(Guid userId, AgentTrigger trigger, Guid? existingResultId, string? riskProfile = null, int? age = null, CancellationToken cancellationToken = default);
    Task<AgentResultResponse> ExecuteReportAsync(Guid userId, AgentTrigger trigger, Guid? existingResultId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid resultId, string errorMessage, CancellationToken cancellationToken = default);
}
