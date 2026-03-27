using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Categories;

public sealed class CategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public TransactionType Type { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsDefault { get; init; }
}
