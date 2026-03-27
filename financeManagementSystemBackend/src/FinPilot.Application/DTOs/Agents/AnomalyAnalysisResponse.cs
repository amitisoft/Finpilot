namespace FinPilot.Application.DTOs.Agents;

public sealed class AnomalyAnalysisResponse
{
    public Guid TransactionId { get; init; }
    public string Severity { get; init; } = "none";
    public string AnomalyType { get; init; } = "none";
    public int RiskScore { get; init; }
    public string Explanation { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = "none";
    public bool FlagForReview { get; init; }
    public IReadOnlyCollection<string> Signals { get; init; } = Array.Empty<string>();
    public DateTimeOffset GeneratedAt { get; init; }
}
