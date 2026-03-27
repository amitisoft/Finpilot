namespace FinPilot.Application.DTOs.Insights;

public sealed class InsightBundleResponse
{
    public string Headline { get; init; } = string.Empty;
    public IReadOnlyCollection<InsightCardResponse> Cards { get; init; } = Array.Empty<InsightCardResponse>();
    public DateTimeOffset GeneratedAt { get; init; }
    public string Disclaimer { get; init; } = "FinPilot provides informational coaching, not investment, tax, or legal advice.";
}
