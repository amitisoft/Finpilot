using System.ComponentModel.DataAnnotations;
using FinPilot.Domain.Enums;

namespace FinPilot.Application.DTOs.Categories;

public sealed class UpdateCategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    public TransactionType Type { get; init; }

    [StringLength(20)]
    public string? Color { get; init; }

    [StringLength(50)]
    public string? Icon { get; init; }
}
