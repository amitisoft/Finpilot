using FinPilot.Application.DTOs.Transactions;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Application.Interfaces.Transactions;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinPilot.Infrastructure.Finance;

public sealed class TransactionService(
    FinPilotDbContext dbContext,
    IDashboardService dashboardService,
    IAgentOrchestratorService agentOrchestratorService,
    ILogger<TransactionService> logger,
    IAuditLogService? auditLogService = null) : ITransactionService
{
    public async Task<IReadOnlyCollection<TransactionResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.UserId == userId).OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.CreatedAt).Select(Map()).ToListAsync(cancellationToken);
    }

    public async Task<TransactionResponse?> GetByIdAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.UserId == userId && x.Id == transactionId).Select(Map()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TransactionResponse> CreateAsync(Guid userId, CreateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Amount, request.Description);
        var account = await GetOwnedAccountAsync(userId, request.AccountId, cancellationToken);
        var category = await GetCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var transaction = new Domain.Entities.Transaction
        {
            UserId = userId,
            AccountId = account.Id,
            CategoryId = category.Id,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            TransactionDate = NormalizeToUtc(request.TransactionDate),
            Merchant = request.Merchant?.Trim(),
            Notes = request.Notes?.Trim()
        };

        account.CurrentBalance += GetSignedAmount(transaction);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);
        await QueueAnomalyCheckSafelyAsync(userId, transaction.Id, cancellationToken);
        await QueueBudgetChecksSafelyAsync(userId, transaction.TransactionDate.Month, transaction.TransactionDate.Year, cancellationToken);
        transaction.Account = account;
        transaction.Category = category;
        var response = Map(transaction);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Transaction", transaction.Id, "created", null, response, cancellationToken);
        }

        return response;
    }

    public async Task<TransactionResponse> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Amount, request.Description);
        var transaction = await dbContext.Transactions.Include(x => x.Account).Include(x => x.Category).FirstOrDefaultAsync(x => x.UserId == userId && x.Id == transactionId, cancellationToken) ?? throw new InvalidOperationException("Transaction not found.");
        var before = Map(transaction);
        var originalAccount = transaction.Account ?? await GetOwnedAccountAsync(userId, transaction.AccountId, cancellationToken);
        var originalTransactionDate = transaction.TransactionDate;

        originalAccount.CurrentBalance -= GetSignedAmount(transaction);
        originalAccount.UpdatedAt = DateTimeOffset.UtcNow;
        var targetAccount = originalAccount.Id == request.AccountId ? originalAccount : await GetOwnedAccountAsync(userId, request.AccountId, cancellationToken);
        var targetCategory = await GetCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);
        transaction.AccountId = targetAccount.Id;
        transaction.CategoryId = targetCategory.Id;
        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.Description = request.Description.Trim();
        transaction.TransactionDate = NormalizeToUtc(request.TransactionDate);
        transaction.Merchant = request.Merchant?.Trim();
        transaction.Notes = request.Notes?.Trim();
        transaction.UpdatedAt = DateTimeOffset.UtcNow;
        targetAccount.CurrentBalance += GetSignedAmount(transaction);
        targetAccount.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);
        await QueueAnomalyCheckSafelyAsync(userId, transaction.Id, cancellationToken);
        await QueueBudgetChecksSafelyAsync(userId, transaction.TransactionDate.Month, transaction.TransactionDate.Year, cancellationToken);
        if (originalTransactionDate.Year != transaction.TransactionDate.Year || originalTransactionDate.Month != transaction.TransactionDate.Month)
        {
            await QueueBudgetChecksSafelyAsync(userId, originalTransactionDate.Month, originalTransactionDate.Year, cancellationToken);
        }
        transaction.Account = targetAccount;
        transaction.Category = targetCategory;
        var response = Map(transaction);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Transaction", transaction.Id, "updated", before, response, cancellationToken);
        }

        return response;
    }

    public async Task DeleteAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Transactions.Include(x => x.Account).Include(x => x.Category).FirstOrDefaultAsync(x => x.UserId == userId && x.Id == transactionId, cancellationToken) ?? throw new InvalidOperationException("Transaction not found.");
        var before = Map(transaction);
        var account = transaction.Account ?? await GetOwnedAccountAsync(userId, transaction.AccountId, cancellationToken);
        account.CurrentBalance -= GetSignedAmount(transaction);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        var transactionDate = transaction.TransactionDate;
        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);
        await QueueBudgetChecksSafelyAsync(userId, transactionDate.Month, transactionDate.Year, cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Transaction", transactionId, "deleted", before, null, cancellationToken);
        }
    }

    private async Task QueueAnomalyCheckSafelyAsync(Guid userId, Guid transactionId, CancellationToken cancellationToken)
    {
        try
        {
            await agentOrchestratorService.QueueTransactionAnomalyCheckAsync(userId, transactionId, AgentTrigger.Event, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to queue anomaly screening for transaction {TransactionId}", transactionId);
        }
    }

    private async Task QueueBudgetChecksSafelyAsync(Guid userId, int month, int year, CancellationToken cancellationToken)
    {
        try
        {
            await agentOrchestratorService.QueueBudgetChecksForPeriodAsync(userId, month, year, AgentTrigger.Event, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to queue budget screening for {Month}/{Year}", month, year);
        }
    }

    private async Task<Account> GetOwnedAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken) => await dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == accountId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Account not found.");

    private async Task<Category> GetCategoryAsync(Guid userId, Guid categoryId, TransactionType expectedType, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId && (x.UserId == userId || x.UserId == null), cancellationToken) ?? throw new InvalidOperationException("Category not found.");
        if (category.Type != expectedType)
        {
            throw new InvalidOperationException("Transaction type must match category type.");
        }

        return category;
    }

    private static DateTimeOffset NormalizeToUtc(DateTimeOffset value) => value.ToUniversalTime();
    private static decimal GetSignedAmount(Domain.Entities.Transaction transaction) => transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
    private static void Validate(decimal amount, string description) { if (amount <= 0) throw new InvalidOperationException("Amount must be greater than zero."); if (string.IsNullOrWhiteSpace(description)) throw new InvalidOperationException("Description is required."); }
    private static System.Linq.Expressions.Expression<Func<Domain.Entities.Transaction, TransactionResponse>> Map() => x => new TransactionResponse { Id = x.Id, AccountId = x.AccountId, AccountName = x.Account != null ? x.Account.Name : string.Empty, CategoryId = x.CategoryId, CategoryName = x.Category != null ? x.Category.Name : string.Empty, Type = x.Type, Amount = x.Amount, Description = x.Description, TransactionDate = x.TransactionDate, Merchant = x.Merchant, Notes = x.Notes };
    private static TransactionResponse Map(Domain.Entities.Transaction x) => new() { Id = x.Id, AccountId = x.AccountId, AccountName = x.Account?.Name ?? string.Empty, CategoryId = x.CategoryId, CategoryName = x.Category?.Name ?? string.Empty, Type = x.Type, Amount = x.Amount, Description = x.Description, TransactionDate = x.TransactionDate, Merchant = x.Merchant, Notes = x.Notes };
}
