using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Accounts;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class AccountsController(IAccountService accountService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AccountResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await accountService.GetAllAsync(userId, cancellationToken);
        return Success(items, "Accounts fetched successfully");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await accountService.GetByIdAsync(userId, id, cancellationToken);
        return item is null ? NotFound(ApiResponse<AccountResponse>.Fail("Account not found")) : Success(item, "Account fetched successfully");
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await accountService.CreateAsync(userId, request, cancellationToken);
        return Success(item, "Account created successfully");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountResponse>>> Update(Guid id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await accountService.UpdateAsync(userId, id, request, cancellationToken);
        return Success(item, "Account updated successfully");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        await accountService.DeleteAsync(userId, id, cancellationToken);
        return Success<object>(null, "Account deleted successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
