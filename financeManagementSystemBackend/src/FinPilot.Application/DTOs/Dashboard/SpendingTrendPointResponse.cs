namespace FinPilot.Application.DTOs.Dashboard;

public sealed class SpendingTrendPointResponse
{
    public string Label { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal Income { get; init; }
    public decimal Expense { get; init; }
}
