using FinPilot.Application.DTOs.Transactions;

namespace FinPilot.Application.Interfaces.Transactions;

public interface ITransactionService
{
    Task<IReadOnlyCollection<TransactionResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TransactionResponse?> GetByIdAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);
    Task<TransactionResponse> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default);
}
