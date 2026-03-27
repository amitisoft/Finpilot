using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Accounts;

public sealed class AccountResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public AccountType Type { get; init; }
    public string Currency { get; init; } = "INR";
    public decimal OpeningBalance { get; init; }
    public decimal CurrentBalance { get; init; }
}
