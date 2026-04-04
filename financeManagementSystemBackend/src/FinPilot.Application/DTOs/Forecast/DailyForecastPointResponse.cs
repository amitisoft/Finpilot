namespace FinPilot.Application.DTOs.Forecast;

public sealed class DailyForecastPointResponse
{
    public DateTime Date { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal DailyNetAmount { get; init; }
    public bool IsProjected { get; init; }
}