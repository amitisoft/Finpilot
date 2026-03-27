using FinPilot.Domain.Enums;

namespace FinPilot.Infrastructure.Agents;

internal sealed record AgentWorkItem(Guid ResultId, Guid UserId, AgentType AgentType, AgentTrigger Trigger, Guid? SourceEntityId);
