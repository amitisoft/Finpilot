using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Agents;

public sealed class AgentChatResponse
{
    public string Reply { get; init; } = string.Empty;
    public AgentType AgentUsed { get; init; }
    public IReadOnlyCollection<string> FollowUpSuggestions { get; init; } = Array.Empty<string>();
    public DateTimeOffset GeneratedAt { get; init; }
}
