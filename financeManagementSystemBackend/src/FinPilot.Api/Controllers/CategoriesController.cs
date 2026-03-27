using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Categories;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class CategoriesController(ICategoryService categoryService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CategoryResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await categoryService.GetAllAsync(userId, cancellationToken);
        return Success(items, "Categories fetched successfully");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await categoryService.GetByIdAsync(userId, id, cancellationToken);
        return item is null ? NotFound(ApiResponse<CategoryResponse>.Fail("Category not found")) : Success(item, "Category fetched successfully");
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await categoryService.CreateAsync(userId, request, cancellationToken);
        return Success(item, "Category created successfully");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await categoryService.UpdateAsync(userId, id, request, cancellationToken);
        return Success(item, "Category updated successfully");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        await categoryService.DeleteAsync(userId, id, cancellationToken);
        return Success<object>(null, "Category deleted successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
