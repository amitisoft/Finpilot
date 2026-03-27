using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Goals;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Goals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class GoalsController(IGoalService goalService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GoalResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await goalService.GetAllAsync(userId, cancellationToken);
        return Success(items, "Goals fetched successfully");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GoalResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await goalService.GetByIdAsync(userId, id, cancellationToken);
        return item is null ? NotFound(ApiResponse<GoalResponse>.Fail("Goal not found")) : Success(item, "Goal fetched successfully");
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GoalResponse>>> Create(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await goalService.CreateAsync(userId, request, cancellationToken);
        return Success(item, "Goal created successfully");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GoalResponse>>> Update(Guid id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await goalService.UpdateAsync(userId, id, request, cancellationToken);
        return Success(item, "Goal updated successfully");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        await goalService.DeleteAsync(userId, id, cancellationToken);
        return Success<object>(null, "Goal deleted successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
