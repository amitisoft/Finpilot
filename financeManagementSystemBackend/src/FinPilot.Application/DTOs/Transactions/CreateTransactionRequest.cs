using System.ComponentModel.DataAnnotations;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Transactions;

public sealed class CreateTransactionRequest
{
    public Guid AccountId { get; init; }
    public Guid CategoryId { get; init; }
    public TransactionType Type { get; init; }

    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Amount { get; init; }

    [Required, StringLength(250, MinimumLength = 2)]
    public string Description { get; init; } = string.Empty;

    public DateTimeOffset TransactionDate { get; init; }

    [StringLength(120)]
    public string? Merchant { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
