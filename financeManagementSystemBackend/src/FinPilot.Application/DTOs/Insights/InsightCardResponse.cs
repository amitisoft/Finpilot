namespace FinPilot.Application.DTOs.Insights;

public sealed class InsightCardResponse
{
    public string Title { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Recommendations { get; init; } = Array.Empty<string>();
}
