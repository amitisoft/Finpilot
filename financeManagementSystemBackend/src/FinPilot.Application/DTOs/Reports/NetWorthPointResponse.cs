namespace FinPilot.Application.DTOs.Reports;

public sealed class NetWorthPointResponse
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal NetWorth { get; init; }
}