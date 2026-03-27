using FinPilot.Application.DTOs.Transactions;
using FinPilot.Application.Interfaces.Agents;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinPilot.UnitTests.Transactions;

public sealed class TransactionServiceTests
{
    [Fact]
    public async Task CreateAsync_Expense_ShouldDecreaseAccountBalance()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var account = new Account { UserId = userId, Name = "Primary", Type = AccountType.Bank, Currency = "INR", OpeningBalance = 1000m, CurrentBalance = 1000m };
        var category = new Category { UserId = userId, Name = "Food", Type = TransactionType.Expense };

        dbContext.Accounts.Add(account);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var orchestrator = new FakeAgentOrchestratorService();
        var service = new TransactionService(dbContext, new FakeDashboardService(), orchestrator, NullLogger<TransactionService>.Instance);
        var result = await service.CreateAsync(userId, new CreateTransactionRequest { AccountId = account.Id, CategoryId = category.Id, Type = TransactionType.Expense, Amount = 250m, Description = "Dinner", TransactionDate = DateTimeOffset.UtcNow });

        Assert.Equal(750m, dbContext.Accounts.Single().CurrentBalance);
        Assert.Equal("Dinner", result.Description);
        Assert.Single(orchestrator.QueuedTransactionIds);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeTransactionDateToUtc()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var account = new Account { UserId = userId, Name = "Primary", Type = AccountType.Bank, Currency = "INR", OpeningBalance = 1000m, CurrentBalance = 1000m };
        var category = new Category { UserId = userId, Name = "Food", Type = TransactionType.Expense };

        dbContext.Accounts.Add(account);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext, new FakeDashboardService(), new FakeAgentOrchestratorService(), NullLogger<TransactionService>.Instance);
        var localOffsetDate = new DateTimeOffset(2026, 3, 22, 18, 0, 0, TimeSpan.FromHours(5.5));

        var result = await service.CreateAsync(userId, new CreateTransactionRequest
        {
            AccountId = account.Id,
            CategoryId = category.Id,
            Type = TransactionType.Expense,
            Amount = 250m,
            Description = "Dinner",
            TransactionDate = localOffsetDate
        });

        Assert.Equal(TimeSpan.Zero, result.TransactionDate.Offset);
        Assert.Equal(localOffsetDate.UtcDateTime, result.TransactionDate.UtcDateTime);
    }

    [Fact]
    public async Task UpdateAsync_ChangingAccount_ShouldMoveBalanceBetweenAccounts()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var source = new Account { UserId = userId, Name = "Bank A", Type = AccountType.Bank, Currency = "INR", OpeningBalance = 1000m, CurrentBalance = 1000m };
        var target = new Account { UserId = userId, Name = "Bank B", Type = AccountType.Bank, Currency = "INR", OpeningBalance = 500m, CurrentBalance = 500m };
        var category = new Category { UserId = userId, Name = "Salary", Type = TransactionType.Income };

        dbContext.Accounts.AddRange(source, target);
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        var service = new TransactionService(dbContext, new FakeDashboardService(), new FakeAgentOrchestratorService(), NullLogger<TransactionService>.Instance);
        var created = await service.CreateAsync(userId, new CreateTransactionRequest { AccountId = source.Id, CategoryId = category.Id, Type = TransactionType.Income, Amount = 200m, Description = "Salary credit", TransactionDate = DateTimeOffset.UtcNow });

        await service.UpdateAsync(userId, created.Id, new UpdateTransactionRequest { AccountId = target.Id, CategoryId = category.Id, Type = TransactionType.Income, Amount = 200m, Description = "Salary credit", TransactionDate = DateTimeOffset.UtcNow });

        Assert.Equal(1000m, dbContext.Accounts.Single(x => x.Id == source.Id).CurrentBalance);
        Assert.Equal(700m, dbContext.Accounts.Single(x => x.Id == target.Id).CurrentBalance);
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Budgets.BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Budgets.BudgetStatusResponse>>(Array.Empty<FinPilot.Application.DTOs.Budgets.BudgetStatusResponse>());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.CategoryBreakdownResponse>());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.GoalProgressResponse>());
        public Task<FinPilot.Application.DTOs.Dashboard.DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new FinPilot.Application.DTOs.Dashboard.DashboardSummaryResponse());
        public Task<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>>(Array.Empty<FinPilot.Application.DTOs.Dashboard.SpendingTrendPointResponse>());
        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAgentOrchestratorService : IAgentOrchestratorService
    {
        public List<Guid> QueuedTransactionIds { get; } = new();
        public List<(int Month, int Year)> QueuedBudgetPeriods { get; } = new();

        public Task QueueTransactionAnomalyCheckAsync(Guid userId, Guid transactionId, AgentTrigger trigger, CancellationToken cancellationToken = default)
        {
            QueuedTransactionIds.Add(transactionId);
            return Task.CompletedTask;
        }

        public Task QueueBudgetCheckAsync(Guid userId, Guid budgetId, AgentTrigger trigger, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task QueueBudgetChecksForPeriodAsync(Guid userId, int month, int year, AgentTrigger trigger, CancellationToken cancellationToken = default)
        {
            QueuedBudgetPeriods.Add((month, year));
            return Task.CompletedTask;
        }

        public Task<FinPilot.Application.DTOs.Agents.AgentInvocationResponse> InvokeAsync(Guid userId, FinPilot.Application.DTOs.Agents.InvokeAgentRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<FinPilot.Application.DTOs.Agents.AgentChatResponse> ChatAsync(Guid userId, FinPilot.Application.DTOs.Agents.AgentChatRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
