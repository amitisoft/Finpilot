using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Agents;

public sealed class AgentResultResponse
{
    public Guid Id { get; init; }
    public AgentType Agent { get; init; }
    public AgentTrigger Trigger { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Severity { get; init; } = "none";
    public string SourceEntityName { get; init; } = string.Empty;
    public Guid? SourceEntityId { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public bool IsDismissed { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public AnomalyAnalysisResponse? Anomaly { get; init; }
    public BudgetAdvisorAnalysisResponse? Budget { get; init; }
    public CoachAnalysisResponse? Coach { get; init; }
    public InvestmentAdvisorAnalysisResponse? Investment { get; init; }
    public ReportGeneratorAnalysisResponse? Report { get; init; }
}
