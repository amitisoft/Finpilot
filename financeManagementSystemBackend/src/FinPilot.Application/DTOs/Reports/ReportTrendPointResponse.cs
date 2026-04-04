namespace FinPilot.Application.DTOs.Reports;

public sealed class ReportTrendPointResponse
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
    public decimal NetAmount { get; init; }
}