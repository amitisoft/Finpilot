using System.Text;
using FinPilot.Application.DTOs.Budgets;
using FinPilot.Application.DTOs.Dashboard;
using FinPilot.Application.Interfaces;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Agents;
using FinPilot.Infrastructure.Forecast;
using FinPilot.Infrastructure.Insights;
using FinPilot.Infrastructure.Persistence;
using FinPilot.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Reports;

public sealed class FinancialReportPdfServiceTests
{
    [Fact]
    public async Task GenerateFinancialReportPdfAsync_ShouldReturnProfessionalPdfDocument()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var salaryCategoryId = Guid.NewGuid();
        var foodCategoryId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            FullName = "Abhishek Shukla",
            Email = "abhishek.shukla@amiti.in",
            PasswordHash = "hash"
        });

        dbContext.Accounts.Add(new Account
        {
            Id = accountId,
            UserId = userId,
            Name = "Primary Bank",
            Type = AccountType.Bank,
            Currency = "INR",
            OpeningBalance = 10000m,
            CurrentBalance = 57000m
        });

        dbContext.Categories.AddRange(
            new Category { Id = salaryCategoryId, UserId = userId, Name = "Salary", Type = TransactionType.Income },
            new Category { Id = foodCategoryId, UserId = userId, Name = "Food", Type = TransactionType.Expense });

        dbContext.Transactions.AddRange(
            new Transaction
            {
                UserId = userId,
                AccountId = accountId,
                CategoryId = salaryCategoryId,
                Type = TransactionType.Income,
                Amount = 50000m,
                Description = "Salary",
                TransactionDate = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero)
            },
            new Transaction
            {
                UserId = userId,
                AccountId = accountId,
                CategoryId = foodCategoryId,
                Type = TransactionType.Expense,
                Amount = 3000m,
                Description = "Groceries",
                TransactionDate = new DateTimeOffset(2026, 3, 12, 0, 0, 0, TimeSpan.Zero)
            });

        dbContext.Goals.Add(new Goal
        {
            UserId = userId,
            Name = "Emergency Fund",
            CurrentAmount = 20000m,
            TargetAmount = 100000m,
            TargetDate = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero),
            Status = GoalStatus.Active
        });

        await dbContext.SaveChangesAsync();

        var dashboardService = new FakeDashboardService(foodCategoryId);
        var dateTimeProvider = new FakeDateTimeProvider(new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero));
        var contextBuilder = new InsightContextBuilder(dbContext, dashboardService);
        var reportService = new ReportsService(dbContext, dateTimeProvider);
        var forecastService = new ForecastService(dbContext, dateTimeProvider);
        var coachService = new FinancialCoachAgentService(contextBuilder);
        var healthService = new HealthScoreService(coachService);
        var reportAgentService = new ReportGeneratorAgentService(contextBuilder);
        var service = new FinancialReportPdfService(dbContext, reportService, forecastService, healthService, reportAgentService, contextBuilder);

        var (content, fileName) = await service.GenerateFinancialReportPdfAsync(userId);

        Assert.NotNull(content);
        Assert.True(content.Length > 1024);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(content, 0, 4));
        Assert.StartsWith("finpilot-financial-report-2026-", fileName, StringComparison.Ordinal);
    }

    private static FinPilotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinPilotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinPilotDbContext(options);
    }

    private sealed class FakeDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeDashboardService(Guid expenseCategoryId) : IDashboardService
    {
        public Task<DashboardSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new DashboardSummaryResponse
            {
                TotalIncome = 50000m,
                TotalExpenses = 3000m,
                NetAmount = 47000m,
                TotalBalance = 57000m,
                TransactionCount = 2
            });

        public Task<IReadOnlyCollection<SpendingTrendPointResponse>> GetSpendingTrendAsync(Guid userId, int months = 6, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SpendingTrendPointResponse>>([
                new SpendingTrendPointResponse { Month = 2, Year = 2026, Income = 48000m, Expense = 2800m },
                new SpendingTrendPointResponse { Month = 3, Year = 2026, Income = 50000m, Expense = 3000m }
            ]);

        public Task<IReadOnlyCollection<CategoryBreakdownResponse>> GetCategoryBreakdownAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<CategoryBreakdownResponse>>([
                new CategoryBreakdownResponse { CategoryId = expenseCategoryId, CategoryName = "Food", Amount = 3000m, Percentage = 100m }
            ]);

        public Task<IReadOnlyCollection<GoalProgressResponse>> GetGoalProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<GoalProgressResponse>>(Array.Empty<GoalProgressResponse>());

        public Task<IReadOnlyCollection<BudgetStatusResponse>> GetBudgetHealthAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<BudgetStatusResponse>>([
                new BudgetStatusResponse
                {
                    BudgetId = Guid.NewGuid(),
                    BudgetName = "March Budget",
                    TotalLimit = 8000m,
                    TotalSpent = 3000m,
                    RemainingAmount = 5000m,
                    UsagePercent = 37.5m,
                    ThresholdReached = false,
                    IsOverBudget = false
                }
            ]);

        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
