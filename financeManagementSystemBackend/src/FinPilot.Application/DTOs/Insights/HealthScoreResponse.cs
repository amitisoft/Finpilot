namespace FinPilot.Application.DTOs.Insights;

public sealed class HealthScoreResponse
{
    public int Score { get; init; }
    public string Label { get; init; } = string.Empty;
    public IReadOnlyCollection<HealthScoreBreakdownResponse> Breakdown { get; init; } = Array.Empty<HealthScoreBreakdownResponse>();
    public IReadOnlyCollection<string> Strengths { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Risks { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> Suggestions { get; init; } = Array.Empty<string>();
    public DateTimeOffset GeneratedAt { get; init; }
    public string Disclaimer { get; init; } = "FinPilot provides informational coaching, not investment, tax, or legal advice.";
}

public sealed class HealthScoreBreakdownResponse
{
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}