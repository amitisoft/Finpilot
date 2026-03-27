using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Budgets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class BudgetsController(IBudgetService budgetService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BudgetResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await budgetService.GetAllAsync(userId, cancellationToken);
        return Success(items, "Budgets fetched successfully");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await budgetService.GetByIdAsync(userId, id, cancellationToken);
        return item is null ? NotFound(ApiResponse<BudgetResponse>.Fail("Budget not found")) : Success(item, "Budget fetched successfully");
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> Create(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await budgetService.CreateAsync(userId, request, cancellationToken);
        return Success(item, "Budget created successfully");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> Update(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await budgetService.UpdateAsync(userId, id, request, cancellationToken);
        return Success(item, "Budget updated successfully");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        await budgetService.DeleteAsync(userId, id, cancellationToken);
        return Success<object>(null, "Budget deleted successfully");
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BudgetStatusResponse>>>> GetStatuses(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await budgetService.GetStatusesAsync(userId, cancellationToken);
        return Success(items, "Budget status fetched successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
