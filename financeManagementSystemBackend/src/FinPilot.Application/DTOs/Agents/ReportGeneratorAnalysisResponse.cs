namespace FinPilot.Application.DTOs.Agents;

public sealed class ReportGeneratorAnalysisResponse
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Highlights { get; init; } = Array.Empty<string>();
    public string MarkdownReport { get; init; } = string.Empty;
    public string Forecast { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
}
