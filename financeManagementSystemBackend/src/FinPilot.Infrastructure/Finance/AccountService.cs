using FinPilot.Application.DTOs.Accounts;
using FinPilot.Application.Interfaces.Accounts;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Finance;

public sealed class AccountService(FinPilotDbContext dbContext, IDashboardService dashboardService, IAuditLogService? auditLogService = null) : IAccountService
{
    public async Task<IReadOnlyCollection<AccountResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Name).Select(Map()).ToListAsync(cancellationToken);
    }

    public async Task<AccountResponse?> GetByIdAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Accounts.AsNoTracking().Where(x => x.Id == accountId && x.UserId == userId).Select(Map()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccountResponse> CreateAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Currency);
        var account = new Domain.Entities.Account { UserId = userId, Name = request.Name.Trim(), Type = request.Type, Currency = request.Currency.Trim().ToUpperInvariant(), OpeningBalance = request.OpeningBalance, CurrentBalance = request.OpeningBalance };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        var response = Map(account);
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Account", account.Id, "created", null, response, cancellationToken);
        }

        return response;
    }

    public async Task<AccountResponse> UpdateAsync(Guid userId, Guid accountId, UpdateAccountRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Currency);
        var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == accountId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Account not found.");
        var before = Map(account);
        var transactionDelta = await dbContext.Transactions.AsNoTracking().Where(x => x.UserId == userId && x.AccountId == accountId).ToListAsync(cancellationToken);
        account.Name = request.Name.Trim();
        account.Type = request.Type;
        account.Currency = request.Currency.Trim().ToUpperInvariant();
        account.OpeningBalance = request.OpeningBalance;
        account.CurrentBalance = request.OpeningBalance + transactionDelta.Sum(GetSignedAmount);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        var response = Map(account);
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Account", account.Id, "updated", before, response, cancellationToken);
        }

        return response;
    }

    public async Task DeleteAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == accountId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Account not found.");
        var before = Map(account);
        var hasTransactions = await dbContext.Transactions.AnyAsync(x => x.AccountId == accountId, cancellationToken);
        if (hasTransactions) throw new InvalidOperationException("Account cannot be deleted because it has transactions.");
        dbContext.Accounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Account", accountId, "deleted", before, null, cancellationToken);
        }
    }

    private static decimal GetSignedAmount(Domain.Entities.Transaction transaction) => transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
    private static void Validate(string name, string currency)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Account name is required.");
        if (string.IsNullOrWhiteSpace(currency)) throw new InvalidOperationException("Currency is required.");
    }
    private static System.Linq.Expressions.Expression<Func<Domain.Entities.Account, AccountResponse>> Map() => x => new AccountResponse { Id = x.Id, Name = x.Name, Type = x.Type, Currency = x.Currency, OpeningBalance = x.OpeningBalance, CurrentBalance = x.CurrentBalance };
    private static AccountResponse Map(Domain.Entities.Account x) => new() { Id = x.Id, Name = x.Name, Type = x.Type, Currency = x.Currency, OpeningBalance = x.OpeningBalance, CurrentBalance = x.CurrentBalance };
}
