namespace FinPilot.Application.DTOs.Forecast;

public sealed class MonthlyForecastResponse
{
    public decimal CurrentBalance { get; init; }
    public decimal ProjectedEndOfMonthBalance { get; init; }
    public decimal ProjectedMonthNetAmount { get; init; }
    public decimal ProjectedRemainingNetAmount { get; init; }
    public decimal AverageDailyNetAmount { get; init; }
    public int DaysTracked { get; init; }
    public int DaysRemaining { get; init; }
    public string Confidence { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Assumptions { get; init; } = Array.Empty<string>();
    public DateTimeOffset GeneratedAt { get; init; }
}