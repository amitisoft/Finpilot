using FinPilot.Application.DTOs.Budgets;

namespace FinPilot.Application.Interfaces.Budgets;

public interface IBudgetService
{
    Task<IReadOnlyCollection<BudgetResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BudgetResponse?> GetByIdAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default);
    Task<BudgetResponse> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken = default);
    Task<BudgetResponse> UpdateAsync(Guid userId, Guid budgetId, UpdateBudgetRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BudgetStatusResponse>> GetStatusesAsync(Guid userId, CancellationToken cancellationToken = default);
}
