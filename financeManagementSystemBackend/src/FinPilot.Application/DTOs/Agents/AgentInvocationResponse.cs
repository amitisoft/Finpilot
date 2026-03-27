using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Agents;

public sealed class AgentInvocationResponse
{
    public AgentType Agent { get; init; }
    public bool Cached { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public string Disclaimer { get; init; } = "FinPilot provides informational guidance only and does not execute financial actions.";
    public AgentResultResponse Result { get; init; } = new();
}
