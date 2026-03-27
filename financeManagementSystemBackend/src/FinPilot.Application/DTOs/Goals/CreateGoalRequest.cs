using System.ComponentModel.DataAnnotations;

namespace FinPilot.Application.DTOs.Goals;

public sealed class CreateGoalRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal TargetAmount { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal CurrentAmount { get; init; }

    public DateTimeOffset? TargetDate { get; init; }
}
