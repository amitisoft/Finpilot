namespace FinPilot.Application.DTOs.Agents;

public sealed class DashboardReportWidgetResponse
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Highlights { get; init; } = Array.Empty<string>();
    public string Forecast { get; init; } = string.Empty;
    public string Disclaimer { get; init; } = "FinPilot report widgets summarize your existing data and should be reviewed before making decisions.";
    public DateTimeOffset GeneratedAt { get; init; }
}
