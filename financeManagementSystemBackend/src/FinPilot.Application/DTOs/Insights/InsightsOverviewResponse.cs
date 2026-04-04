namespace FinPilot.Application.DTOs.Insights;

public sealed class InsightsOverviewResponse
{
    public string Headline { get; init; } = string.Empty;
    public int HealthScore { get; init; }
    public string HealthLabel { get; init; } = string.Empty;
    public IReadOnlyCollection<InsightsOverviewSectionResponse> Sections { get; init; } = Array.Empty<InsightsOverviewSectionResponse>();
    public DateTimeOffset GeneratedAt { get; init; }
    public string Disclaimer { get; init; } = "FinPilot provides informational coaching, not investment, tax, or legal advice.";
}

public sealed class InsightsOverviewSectionResponse
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Headline { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
}