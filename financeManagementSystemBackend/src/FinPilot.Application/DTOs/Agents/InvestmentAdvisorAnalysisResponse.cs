namespace FinPilot.Application.DTOs.Agents;

public sealed class InvestmentAdvisorAnalysisResponse
{
    public string Disclaimer { get; init; } = "This is informational guidance only and not licensed investment advice.";
    public string RiskProfile { get; init; } = "moderate";
    public int ConfidenceScore { get; init; }
    public decimal MonthlySurplus { get; init; }
    public IReadOnlyCollection<InvestmentAllocationSuggestionResponse> AllocationSuggestions { get; init; } = Array.Empty<InvestmentAllocationSuggestionResponse>();
    public IReadOnlyCollection<string> PriorityActions { get; init; } = Array.Empty<string>();
    public string Reasoning { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed class InvestmentAllocationSuggestionResponse
{
    public string Bucket { get; init; } = string.Empty;
    public int Percentage { get; init; }
    public string Rationale { get; init; } = string.Empty;
}
