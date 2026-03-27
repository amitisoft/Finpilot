using FinPilot.Application.Common;
using FinPilot.Application.DTOs.Transactions;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinPilot.Api.Controllers;

[Authorize]
public sealed class TransactionsController(ITransactionService transactionService, ICurrentUserService currentUserService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TransactionResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var items = await transactionService.GetAllAsync(userId, cancellationToken);
        return Success(items, "Transactions fetched successfully");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await transactionService.GetByIdAsync(userId, id, cancellationToken);
        return item is null ? NotFound(ApiResponse<TransactionResponse>.Fail("Transaction not found")) : Success(item, "Transaction fetched successfully");
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Create(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await transactionService.CreateAsync(userId, request, cancellationToken);
        return Success(item, "Transaction created successfully");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Update(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        var item = await transactionService.UpdateAsync(userId, id, request, cancellationToken);
        return Success(item, "Transaction updated successfully");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = EnsureUser();
        await transactionService.DeleteAsync(userId, id, cancellationToken);
        return Success<object>(null, "Transaction deleted successfully");
    }

    private Guid EnsureUser() => currentUserService.UserId ?? throw new InvalidOperationException("Unauthorized");
}
