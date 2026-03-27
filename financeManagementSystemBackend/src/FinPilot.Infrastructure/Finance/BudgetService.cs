using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Budgets;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinPilot.Infrastructure.Finance;

public sealed class BudgetService(
    FinPilotDbContext dbContext,
    IDashboardService? dashboardService = null,
    IAgentOrchestratorService? agentOrchestratorService = null,
    ILogger<BudgetService>? logger = null,
    IAuditLogService? auditLogService = null) : IBudgetService
{
    public async Task<IReadOnlyCollection<BudgetResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var budgets = await QueryBudgets(userId).ToListAsync(cancellationToken);
        return await MapBudgetsAsync(budgets, cancellationToken);
    }

    public async Task<BudgetResponse?> GetByIdAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default)
    {
        var budget = await QueryBudgets(userId).FirstOrDefaultAsync(x => x.Id == budgetId, cancellationToken);
        if (budget is null) return null;
        return (await MapBudgetsAsync([budget], cancellationToken)).Single();
    }

    public async Task<BudgetResponse> CreateAsync(Guid userId, CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Month, request.Year, request.TotalLimit, request.AlertThresholdPercent, request.Items);
        await EnsureUniqueBudgetAsync(userId, request.Month, request.Year, null, cancellationToken);
        var categoryMap = await ResolveExpenseCategoriesAsync(userId, request.Items.Select(x => x.CategoryId).Distinct().ToArray(), cancellationToken);
        var budget = new Budget
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Month = request.Month,
            Year = request.Year,
            TotalLimit = request.TotalLimit,
            AlertThresholdPercent = request.AlertThresholdPercent,
            BudgetItems = request.Items.Select(x => new BudgetItem { CategoryId = x.CategoryId, LimitAmount = x.LimitAmount }).ToList()
        };

        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardAsync(userId, cancellationToken);
        await QueueBudgetCheckSafelyAsync(userId, budget.Id, cancellationToken);
        var response = (await MapBudgetsAsync([budget], cancellationToken, categoryMap)).Single();

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Budget", budget.Id, "created", null, response, cancellationToken);
        }

        return response;
    }

    public async Task<BudgetResponse> UpdateAsync(Guid userId, Guid budgetId, UpdateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Month, request.Year, request.TotalLimit, request.AlertThresholdPercent, request.Items);
        await EnsureUniqueBudgetAsync(userId, request.Month, request.Year, budgetId, cancellationToken);
        await ResolveExpenseCategoriesAsync(userId, request.Items.Select(x => x.CategoryId).Distinct().ToArray(), cancellationToken);

        var budget = await dbContext.Budgets.FirstOrDefaultAsync(x => x.Id == budgetId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Budget not found.");
        var before = await GetByIdAsync(userId, budgetId, cancellationToken) ?? throw new InvalidOperationException("Budget not found.");
        budget.Name = request.Name.Trim();
        budget.Month = request.Month;
        budget.Year = request.Year;
        budget.TotalLimit = request.TotalLimit;
        budget.AlertThresholdPercent = request.AlertThresholdPercent;
        budget.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var existingItems = await dbContext.BudgetItems.Where(x => x.BudgetId == budgetId).ToListAsync(cancellationToken);
        if (existingItems.Count > 0)
        {
            dbContext.BudgetItems.RemoveRange(existingItems);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var replacementItems = request.Items.Select(x => new BudgetItem
        {
            BudgetId = budget.Id,
            CategoryId = x.CategoryId,
            LimitAmount = x.LimitAmount
        }).ToList();

        dbContext.BudgetItems.AddRange(replacementItems);
        await dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardAsync(userId, cancellationToken);
        await QueueBudgetCheckSafelyAsync(userId, budgetId, cancellationToken);

        var response = await GetByIdAsync(userId, budgetId, cancellationToken) ?? throw new InvalidOperationException("Budget not found after update.");
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Budget", budgetId, "updated", before, response, cancellationToken);
        }

        return response;
    }

    public async Task DeleteAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken = default)
    {
        var budget = await dbContext.Budgets.Include(x => x.BudgetItems).FirstOrDefaultAsync(x => x.Id == budgetId && x.UserId == userId, cancellationToken) ?? throw new InvalidOperationException("Budget not found.");
        var before = await GetByIdAsync(userId, budgetId, cancellationToken) ?? throw new InvalidOperationException("Budget not found.");
        dbContext.BudgetItems.RemoveRange(budget.BudgetItems);
        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardAsync(userId, cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Budget", budgetId, "deleted", before, null, cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<BudgetStatusResponse>> GetStatusesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var budgets = await QueryBudgets(userId).ToListAsync(cancellationToken);
        var responses = await MapBudgetsAsync(budgets, cancellationToken);
        return responses.Select(x => new BudgetStatusResponse { BudgetId = x.Id, BudgetName = x.Name, Month = x.Month, Year = x.Year, TotalLimit = x.TotalLimit, TotalSpent = x.TotalSpent, RemainingAmount = x.RemainingAmount, UsagePercent = x.UsagePercent, IsOverBudget = x.TotalSpent > x.TotalLimit, ThresholdReached = x.UsagePercent >= x.AlertThresholdPercent }).ToList();
    }

    private IQueryable<Budget> QueryBudgets(Guid userId) => dbContext.Budgets.AsNoTracking().Include(x => x.BudgetItems).Where(x => x.UserId == userId).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ThenBy(x => x.Name);
    private async Task<IReadOnlyCollection<BudgetResponse>> MapBudgetsAsync(IReadOnlyCollection<Budget> budgets, CancellationToken cancellationToken, Dictionary<Guid, Category>? resolvedCategories = null)
    {
        if (budgets.Count == 0) return Array.Empty<BudgetResponse>();
        var categoryIds = budgets.SelectMany(x => x.BudgetItems).Select(x => x.CategoryId).Distinct().ToArray();
        var categoryMap = resolvedCategories ?? await dbContext.Categories.AsNoTracking().Where(x => categoryIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var transactionGroups = await LoadBudgetSpendAsync(budgets, cancellationToken);
        return budgets.Select(budget =>
        {
            var items = budget.BudgetItems.Select(item =>
            {
                var spent = transactionGroups.TryGetValue((budget.Year, budget.Month, item.CategoryId), out var value) ? value : 0m;
                var usagePercent = item.LimitAmount <= 0 ? 0 : decimal.Round((spent / item.LimitAmount) * 100m, 2);
                return new BudgetItemResponse { CategoryId = item.CategoryId, CategoryName = categoryMap.TryGetValue(item.CategoryId, out var category) ? category.Name : string.Empty, LimitAmount = item.LimitAmount, SpentAmount = spent, RemainingAmount = item.LimitAmount - spent, UsagePercent = usagePercent };
            }).ToList();
            var totalSpent = items.Sum(x => x.SpentAmount);
            var totalUsage = budget.TotalLimit <= 0 ? 0 : decimal.Round((totalSpent / budget.TotalLimit) * 100m, 2);
            return new BudgetResponse { Id = budget.Id, Name = budget.Name, Month = budget.Month, Year = budget.Year, TotalLimit = budget.TotalLimit, TotalSpent = totalSpent, RemainingAmount = budget.TotalLimit - totalSpent, AlertThresholdPercent = budget.AlertThresholdPercent, UsagePercent = totalUsage, Items = items };
        }).ToList();
    }
    private async Task<Dictionary<(int Year, int Month, Guid CategoryId), decimal>> LoadBudgetSpendAsync(IReadOnlyCollection<Budget> budgets, CancellationToken cancellationToken)
    { var years = budgets.Select(x => x.Year).Distinct().ToArray(); var months = budgets.Select(x => x.Month).Distinct().ToArray(); var categoryIds = budgets.SelectMany(x => x.BudgetItems).Select(x => x.CategoryId).Distinct().ToArray(); var userId = budgets.First().UserId; var transactions = await dbContext.Transactions.AsNoTracking().Where(x => x.UserId == userId && x.Type == TransactionType.Expense && years.Contains(x.TransactionDate.Year) && months.Contains(x.TransactionDate.Month) && categoryIds.Contains(x.CategoryId)).ToListAsync(cancellationToken); return transactions.GroupBy(x => (x.TransactionDate.Year, x.TransactionDate.Month, x.CategoryId)).ToDictionary(x => x.Key, x => x.Sum(t => t.Amount)); }
    private async Task<Dictionary<Guid, Category>> ResolveExpenseCategoriesAsync(Guid userId, Guid[] categoryIds, CancellationToken cancellationToken)
    { var categories = await dbContext.Categories.AsNoTracking().Where(x => categoryIds.Contains(x.Id) && (x.UserId == userId || x.UserId == null)).ToListAsync(cancellationToken); if (categories.Count != categoryIds.Length) throw new InvalidOperationException("One or more categories were not found."); if (categories.Any(x => x.Type != TransactionType.Expense)) throw new InvalidOperationException("Budgets only support expense categories."); return categories.ToDictionary(x => x.Id); }
    private async Task EnsureUniqueBudgetAsync(Guid userId, int month, int year, Guid? existingId, CancellationToken cancellationToken)
    { var exists = await dbContext.Budgets.AnyAsync(x => x.UserId == userId && x.Month == month && x.Year == year && (!existingId.HasValue || x.Id != existingId.Value), cancellationToken); if (exists) throw new InvalidOperationException("A budget already exists for the selected month and year."); }
    private static void Validate(string name, int month, int year, decimal totalLimit, int alertThresholdPercent, IReadOnlyCollection<BudgetItemRequest> items)
    { if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Budget name is required."); if (month is < 1 or > 12) throw new InvalidOperationException("Month must be between 1 and 12."); if (year < 2000 || year > 2100) throw new InvalidOperationException("Year is invalid."); if (totalLimit <= 0) throw new InvalidOperationException("Total limit must be greater than zero."); if (alertThresholdPercent is < 1 or > 100) throw new InvalidOperationException("Alert threshold must be between 1 and 100."); if (items.Count == 0) throw new InvalidOperationException("At least one budget item is required."); if (items.Any(x => x.LimitAmount <= 0)) throw new InvalidOperationException("Budget item limit amounts must be greater than zero."); if (items.GroupBy(x => x.CategoryId).Any(g => g.Count() > 1)) throw new InvalidOperationException("Duplicate categories are not allowed in a budget."); if (items.Sum(x => x.LimitAmount) > totalLimit) throw new InvalidOperationException("Budget item limits cannot exceed the total budget."); }
    private async Task InvalidateDashboardAsync(Guid userId, CancellationToken cancellationToken) { if (dashboardService is not null) await dashboardService.InvalidateAsync(userId, cancellationToken); }
    private async Task QueueBudgetCheckSafelyAsync(Guid userId, Guid budgetId, CancellationToken cancellationToken)
    {
        if (agentOrchestratorService is null)
        {
            return;
        }

        try
        {
            await agentOrchestratorService.QueueBudgetCheckAsync(userId, budgetId, AgentTrigger.Event, cancellationToken);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Unable to queue budget screening for budget {BudgetId}", budgetId);
        }
    }
}
