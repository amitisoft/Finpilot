namespace FinPilot.Application.DTOs.Agents;

public sealed class DashboardCoachWidgetResponse
{
    public int HealthScore { get; init; }
    public string Headline { get; init; } = string.Empty;
    public string Encouragement { get; init; } = string.Empty;
    public IReadOnlyCollection<string> TopPatterns { get; init; } = Array.Empty<string>();
    public string PrimaryAction { get; init; } = string.Empty;
    public decimal EstimatedMonthlyImpact { get; init; }
    public string Disclaimer { get; init; } = "FinPilot provides informational coaching only and does not execute financial actions.";
    public DateTimeOffset GeneratedAt { get; init; }
}
