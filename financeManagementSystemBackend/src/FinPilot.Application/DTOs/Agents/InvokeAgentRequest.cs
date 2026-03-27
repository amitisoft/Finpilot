using System.ComponentModel.DataAnnotations;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Agents;

public sealed class InvokeAgentRequest
{
    public AgentType Agent { get; init; }
    public AgentTrigger Trigger { get; init; } = AgentTrigger.OnDemand;
    public Guid? TransactionId { get; init; }
    public Guid? BudgetId { get; init; }

    [StringLength(30)]
    public string? RiskProfile { get; init; }

    [Range(18, 100)]
    public int? Age { get; init; }
}
