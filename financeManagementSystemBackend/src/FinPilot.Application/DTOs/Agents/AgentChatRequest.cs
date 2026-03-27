using System.ComponentModel.DataAnnotations;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Agents;

public sealed class AgentChatRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string Message { get; init; } = string.Empty;

    public Guid? TransactionId { get; init; }
    public Guid? BudgetId { get; init; }

    [StringLength(30)]
    public string? RiskProfile { get; init; }

    [Range(18, 100)]
    public int? Age { get; init; }

    public IReadOnlyCollection<string> ConversationHistory { get; init; } = Array.Empty<string>();
}
