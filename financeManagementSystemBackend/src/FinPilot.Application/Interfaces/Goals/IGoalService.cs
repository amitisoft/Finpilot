using FinPilot.Application.DTOs.Goals;

namespace FinPilot.Application.Interfaces.Goals;

public interface IGoalService
{
    Task<IReadOnlyCollection<GoalResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<GoalResponse?> GetByIdAsync(Guid userId, Guid goalId, CancellationToken cancellationToken = default);
    Task<GoalResponse> CreateAsync(Guid userId, CreateGoalRequest request, CancellationToken cancellationToken = default);
    Task<GoalResponse> UpdateAsync(Guid userId, Guid goalId, UpdateGoalRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid goalId, CancellationToken cancellationToken = default);
}
