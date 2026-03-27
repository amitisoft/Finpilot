using FinPilot.Application.DTOs.Categories;

namespace FinPilot.Application.Interfaces.Categories;

public interface ICategoryService
{
    Task<IReadOnlyCollection<CategoryResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> GetByIdAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateAsync(Guid userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);
}
