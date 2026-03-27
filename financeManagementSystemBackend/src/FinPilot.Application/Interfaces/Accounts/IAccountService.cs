using FinPilot.Application.DTOs.Accounts;

namespace FinPilot.Application.Interfaces.Accounts;

public interface IAccountService
{
    Task<IReadOnlyCollection<AccountResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AccountResponse?> GetByIdAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default);
    Task<AccountResponse> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task<AccountResponse> UpdateAsync(Guid userId, Guid accountId, UpdateAccountRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default);
}
