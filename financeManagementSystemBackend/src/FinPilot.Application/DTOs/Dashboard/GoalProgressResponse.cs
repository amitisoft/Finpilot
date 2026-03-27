namespace FinPilot.Application.DTOs.Dashboard;

public sealed class GoalProgressResponse
{
    public Guid GoalId { get; init; }
    public string GoalName { get; init; } = string.Empty;
    public decimal CurrentAmount { get; init; }
    public decimal TargetAmount { get; init; }
    public decimal ProgressPercent { get; init; }
}
