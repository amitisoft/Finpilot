using FinPilot.Application.DTOs.Goals;
using FinPilot.Application.Interfaces.Audit;
using FinPilot.Application.Interfaces.Dashboard;
using FinPilot.Application.Interfaces.Goals;
using FinPilot.Domain.Entities;
using FinPilot.Domain.Enums;
using FinPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinPilot.Infrastructure.Finance;

public sealed class GoalService(FinPilotDbContext dbContext, IDashboardService dashboardService, IAuditLogService? auditLogService = null) : IGoalService
{
    public async Task<IReadOnlyCollection<GoalResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Goals.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.TargetDate).ThenBy(x => x.Name).Select(Map()).ToListAsync(cancellationToken);
    }

    public async Task<GoalResponse?> GetByIdAsync(Guid userId, Guid goalId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Goals.AsNoTracking().Where(x => x.UserId == userId && x.Id == goalId).Select(Map()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GoalResponse> CreateAsync(Guid userId, CreateGoalRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.TargetAmount, request.CurrentAmount);
        var goal = new Goal
        {
            UserId = userId,
            Name = request.Name.Trim(),
            TargetAmount = request.TargetAmount,
            CurrentAmount = request.CurrentAmount,
            TargetDate = NormalizeToUtc(request.TargetDate),
            Status = request.CurrentAmount >= request.TargetAmount ? GoalStatus.Completed : GoalStatus.Active
        };

        dbContext.Goals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        var response = Map(goal);
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Goal", goal.Id, "created", null, response, cancellationToken);
        }

        return response;
    }

    public async Task<GoalResponse> UpdateAsync(Guid userId, Guid goalId, UpdateGoalRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.TargetAmount, request.CurrentAmount);
        var goal = await dbContext.Goals.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == goalId, cancellationToken) ?? throw new InvalidOperationException("Goal not found.");
        var before = Map(goal);
        goal.Name = request.Name.Trim();
        goal.TargetAmount = request.TargetAmount;
        goal.CurrentAmount = request.CurrentAmount;
        goal.TargetDate = NormalizeToUtc(request.TargetDate);
        goal.Status = request.CurrentAmount >= request.TargetAmount ? GoalStatus.Completed : request.Status;
        goal.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        var response = Map(goal);
        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Goal", goal.Id, "updated", before, response, cancellationToken);
        }

        return response;
    }

    public async Task DeleteAsync(Guid userId, Guid goalId, CancellationToken cancellationToken = default)
    {
        var goal = await dbContext.Goals.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == goalId, cancellationToken) ?? throw new InvalidOperationException("Goal not found.");
        var before = Map(goal);
        dbContext.Goals.Remove(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardService.InvalidateAsync(userId, cancellationToken);

        if (auditLogService is not null)
        {
            await auditLogService.WriteAsync(userId, "Goal", goalId, "deleted", before, null, cancellationToken);
        }
    }

    private static DateTimeOffset? NormalizeToUtc(DateTimeOffset? value) => value?.ToUniversalTime();
    private static void Validate(string name, decimal targetAmount, decimal currentAmount)
    { if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Goal name is required."); if (targetAmount <= 0) throw new InvalidOperationException("Target amount must be greater than zero."); if (currentAmount < 0) throw new InvalidOperationException("Current amount cannot be negative."); }
    private static System.Linq.Expressions.Expression<Func<Goal, GoalResponse>> Map() => x => new GoalResponse { Id = x.Id, Name = x.Name, TargetAmount = x.TargetAmount, CurrentAmount = x.CurrentAmount, ProgressPercent = x.TargetAmount <= 0 ? 0 : Math.Round((x.CurrentAmount / x.TargetAmount) * 100m, 2), TargetDate = x.TargetDate, Status = x.Status };
    private static GoalResponse Map(Goal x) => new() { Id = x.Id, Name = x.Name, TargetAmount = x.TargetAmount, CurrentAmount = x.CurrentAmount, ProgressPercent = x.TargetAmount <= 0 ? 0 : Math.Round((x.CurrentAmount / x.TargetAmount) * 100m, 2), TargetDate = x.TargetDate, Status = x.Status };
}
