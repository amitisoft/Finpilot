using System.ComponentModel.DataAnnotations;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Accounts;

public sealed class UpdateAccountRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    public AccountType Type { get; init; }

    [Required, StringLength(10, MinimumLength = 3)]
    public string Currency { get; init; } = "INR";

    [Range(typeof(decimal), "0", "999999999")]
    public decimal OpeningBalance { get; init; }
}
