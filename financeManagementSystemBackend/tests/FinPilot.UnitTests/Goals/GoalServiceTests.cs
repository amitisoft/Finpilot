using FinPilot.Application.DTOs.Goals;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Finance;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.UnitTests.Goals;

public sealed class GoalServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenCurrentAmountMeetsTarget_ShouldMarkGoalCompleted()
    {
        await using var db = CreateDbContext();
        var service = new GoalService(db, new FakeDashboardService());
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(userId, new CreateGoalRequest
        {
            Name = "Emergency Fund",
            TargetAmount = 10000m,
            CurrentAmount = 10000m
        });

        Assert.Equal(GoalStatus.Completed, result.Status);
        Assert.Equal(100m, result.ProgressPercent);
    }

    [Fact]
    public async Task CreateAsync_ShouldNormalizeTargetDateToUtc()
    {
        await using var db = CreateDbContext();
        var service = new GoalService(db, new FakeDashboardService());
        var userId = Guid.NewGuid();
        var localOffsetDate = new DateTimeOffset(2026, 12, 31, 18, 0, 0, TimeSpan.FromHours(5.5));

        var result = await service.CreateAsync(userId, new CreateGoalRequest
        {
            Name = "Emergency Fund",
            TargetAmount = 10000m,
            CurrentAmount = 5000m,
            TargetDate = localOffsetDate
        });

        Assert.NotNull(result.TargetDate);
        Assert.Equal(TimeSpan.Zero, result.TargetDate!.Value.Offset);
        Assert.Equal(localOffsetDate.UtcDateTime, result.TargetDate.Value.UtcDateTime);
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
}
