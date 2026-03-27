using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Transactions;

public sealed class TransactionResponse
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string AccountName { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset TransactionDate { get; init; }
    public string? Merchant { get; init; }
    public string? Notes { get; init; }
}
