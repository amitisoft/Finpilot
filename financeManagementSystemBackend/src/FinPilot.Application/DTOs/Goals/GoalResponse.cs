using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Goals;

public sealed class GoalResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal TargetAmount { get; init; }
    public decimal CurrentAmount { get; init; }
    public decimal ProgressPercent { get; init; }
    public DateTimeOffset? TargetDate { get; init; }
    public GoalStatus Status { get; init; }
}
