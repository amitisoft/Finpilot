using FinPilot.Application.DTOs.Categories;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Categories;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Finance;

public sealed class CategoryService(FinPilotDbContext dbContext, IDashboardService dashboardService, IAuditLogService? auditLogService = null) : ICategoryService
{
    public async Task<IReadOnlyCollection<CategoryResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.AsNoTracking().Where(x => x.UserId == userId || x.UserId == null).OrderBy(x => x.Name).Select(Map()).ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse?> GetByIdAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.AsNoTracking().Where(x => x.Id == categoryId && (x.UserId == userId || x.UserId == null)).Select(Map()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryResponse> CreateAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        var normalizedName = request.Name.Trim();
        var loweredName = normalizedName.ToLower();
        var exists = await dbContext.Categories.AnyAsync(x => x.UserId == userId && x.Name.ToLower() == loweredName && x.Type == request.Type, cancellationToken);
        if (exists) throw new InvalidOperationException("A category with the same name and type already exists.");

        var category = new Domain.Entities.Category { UserId = userId, Name = normalizedName, Type = request.Type, Color = request.Color?.Trim(), Icon = request.Icon?.Trim(), IsDefault = false };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        var response = Map(category);
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Category", category.Id, "created", null, response, cancellationToken);
        }

        return response;
    }

    public async Task<CategoryResponse> UpdateAsync(Guid userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Category not found.");
        var before = Map(category);
        var normalizedName = request.Name.Trim();
        var loweredName = normalizedName.ToLower();
        var exists = await dbContext.Categories.AnyAsync(x => x.Id != categoryId && x.UserId == userId && x.Name.ToLower() == loweredName && x.Type == request.Type, cancellationToken);
        if (exists) throw new InvalidOperationException("A category with the same name and type already exists.");

        category.Name = normalizedName; category.Type = request.Type; category.Color = request.Color?.Trim(); category.Icon = request.Icon?.Trim(); category.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        var response = Map(category);
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Category", category.Id, "updated", before, response, cancellationToken);
        }

        return response;
    }

    public async Task DeleteAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Category not found.");
        var before = Map(category);
        var inUse = await dbContext.Transactions.AnyAsync(x => x.CategoryId == categoryId, cancellationToken) || await dbContext.BudgetItems.AnyAsync(x => x.CategoryId == categoryId, cancellationToken);
        if (inUse) throw new InvalidOperationException("Category cannot be deleted because it is in use.");
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Category", categoryId, "deleted", before, null, cancellationToken);
        }
    }

    private static void Validate(string name) { if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Category name is required."); }
    private static System.Linq.Expressions.Expression<Func<Domain.Entities.Category, CategoryResponse>> Map() => x => new CategoryResponse { Id = x.Id, Name = x.Name, Type = x.Type, Color = x.Color, Icon = x.Icon, IsDefault = x.IsDefault };
    private static CategoryResponse Map(Domain.Entities.Category x) => new() { Id = x.Id, Name = x.Name, Type = x.Type, Color = x.Color, Icon = x.Icon, IsDefault = x.IsDefault };
}
