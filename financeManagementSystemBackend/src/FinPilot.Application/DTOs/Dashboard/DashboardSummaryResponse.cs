namespace FinPilot.Application.DTOs.Dashboard;

public sealed class DashboardSummaryResponse
{
    public decimal TotalIncome { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetAmount { get; init; }
    public decimal TotalBalance { get; init; }
    public int TransactionCount { get; init; }
}
