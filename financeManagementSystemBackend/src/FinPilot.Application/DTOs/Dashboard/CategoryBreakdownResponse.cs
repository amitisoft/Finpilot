namespace FinPilot.Application.DTOs.Dashboard;

public sealed class CategoryBreakdownResponse
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
}
